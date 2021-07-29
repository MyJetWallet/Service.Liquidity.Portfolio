using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc.Simulation.Models;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.Portfolio.Tests;

namespace Service.Liquidity.Portfolio.Simulation.Services
{
    public class AssetPortfolioSimulationManager
    {
        private readonly AssetPortfolioMath _assetPortfolioMath;
        private readonly AssetPortfolioManager _assetPortfolioManager;
        private readonly IndexPricesClientMock _indexPricesClientMock;
        
        private readonly List<PortfolioSimulation> _simulationList = new List<PortfolioSimulation>();

        public AssetPortfolioSimulationManager()
        {
            var noSqlDataReader = new MyNoSqlServerDataReaderMock();
            _indexPricesClientMock = new IndexPricesClientMock();
            _assetPortfolioMath = new AssetPortfolioMath();

            _indexPricesClientMock.PriceMap[AssetPortfolioManager.UsdAsset] = 1;

            _assetPortfolioManager = new AssetPortfolioManager(Program.LogFactory.CreateLogger<AssetPortfolioManager>(),
                noSqlDataReader, _indexPricesClientMock, _assetPortfolioMath) {_isInit = true};

        }

        public async Task<PortfolioSimulation> CreateNewSimulation()
        {
            var newSimulation = new PortfolioSimulation(GenerateNewSimulationId());
            _simulationList.Add(newSimulation);
            return newSimulation;
        }

        public async Task<List<PortfolioSimulation>> GetSimulationList()
        {
            return _simulationList;
        }

        public async Task<PortfolioSimulation> GetSimulation(long simulationId)
        {
            return _simulationList.FirstOrDefault(e => e.SimulationId == simulationId);
        }

        public async Task ReportSimulationTrade(ReportSimulationTradeRequest request)
        {
            SetIndexPrices(request.BaseAsset, request.BaseAssetIndexPrice);
            SetIndexPrices(request.QuoteAsset, request.QuoteAssetIndexPrice);
            
            var simulation = _simulationList.FirstOrDefault(e => e.SimulationId == request.SimulationId);
            if (simulation == null)
                throw new Exception($"Simulation with id {request.SimulationId} not found");
            
            var baseAssetBalance = _assetPortfolioManager.GetBalanceEntity(simulation?.AssetBalances,
                AssetPortfolioManager.Broker, request.WalletName, request.BaseAsset);
            
            var baseAssetDiff = new AssetBalanceDifference(AssetPortfolioManager.Broker, request.WalletName, request.BaseAsset,
                request.BaseVolume, request.BaseVolume * request.BaseAssetIndexPrice, request.BaseAssetIndexPrice);
            
            var quoteAssetBalance = _assetPortfolioManager.GetBalanceEntity(simulation?.AssetBalances,
                AssetPortfolioManager.Broker, request.WalletName, request.QuoteAsset);
            
            var quoteAssetDiff = new AssetBalanceDifference(AssetPortfolioManager.Broker, request.WalletName, request.QuoteAsset,
                request.QuoteVolume, request.QuoteVolume * request.QuoteAssetIndexPrice, request.QuoteAssetIndexPrice);
            
            _assetPortfolioMath.UpdateBalance(baseAssetBalance, baseAssetDiff);
            _assetPortfolioMath.UpdateBalance(quoteAssetBalance, quoteAssetDiff);

            _assetPortfolioManager.FixReleasedPnl(simulation.Portfolio, simulation.AssetBalances);
            _assetPortfolioManager.UpdatePortfolio(simulation.Portfolio, simulation.AssetBalances);
            var trade = new AssetPortfolioTrade()
            {
                DateTime = DateTime.UtcNow,
                BaseAsset = request.BaseAsset,
                QuoteAsset = request.QuoteAsset,
                BaseVolume = request.BaseVolume,
                QuoteVolume = request.QuoteVolume,
                WalletName = request.WalletName
            };
            
            simulation.Trades.Add(trade);
        }

        private void SetIndexPrices(string asset, decimal price)
        {
            _indexPricesClientMock.PriceMap[asset] = price;
        }

        private long GenerateNewSimulationId()
        {
            var lastId = _simulationList.Count == 0 
                ? 0
                : _simulationList.Max(e => e.SimulationId);
            return ++lastId;
        }
    }
}
