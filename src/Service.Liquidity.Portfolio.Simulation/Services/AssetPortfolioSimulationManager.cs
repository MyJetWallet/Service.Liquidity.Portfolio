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
        
        private readonly List<PortfolioSimulation> _simulationList = new();

        public AssetPortfolioSimulationManager()
        {
            var noSqlDataReader = new MyNoSqlServerDataReaderMock();
            _indexPricesClientMock = new IndexPricesClientMock();
            _assetPortfolioMath = new AssetPortfolioMath();

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
            SetIndexPrices(request.SimulationId);
            
            var simulation = _simulationList.FirstOrDefault(e => e.SimulationId == request.SimulationId);
            if (simulation == null)
                throw new Exception($"Simulation with id {request.SimulationId} not found");
            
            var baseAssetBalance = _assetPortfolioManager.GetBalanceEntity(simulation?.AssetBalances,
                AssetPortfolioManager.Broker, request.WalletName, request.BaseAsset);
            
            var baseAssetDiff = new AssetBalanceDifference(AssetPortfolioManager.Broker, request.WalletName, request.BaseAsset,
                request.BaseVolume, request.BaseVolume * _indexPricesClientMock.PriceMap[request.BaseAsset], _indexPricesClientMock.PriceMap[request.BaseAsset]);
            
            var quoteAssetBalance = _assetPortfolioManager.GetBalanceEntity(simulation?.AssetBalances,
                AssetPortfolioManager.Broker, request.WalletName, request.QuoteAsset);
            
            var quoteAssetDiff = new AssetBalanceDifference(AssetPortfolioManager.Broker, request.WalletName, request.QuoteAsset,
                request.QuoteVolume, request.QuoteVolume * _indexPricesClientMock.PriceMap[request.QuoteAsset], _indexPricesClientMock.PriceMap[request.QuoteAsset]);
            
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
                WalletName = request.WalletName,
                BaseAssetPriceInUsd = _indexPricesClientMock.PriceMap[request.BaseAsset],
                QuoteAssetPriceInUsd = _indexPricesClientMock.PriceMap[request.QuoteAsset]
            };
            
            simulation.Trades.Add(trade);
        }

        private void SetIndexPrices(long simulationId)
        {
            _indexPricesClientMock.PriceMap = _simulationList.First(e => e.SimulationId == simulationId).PriceMap;
        }

        private long GenerateNewSimulationId()
        {
            var lastId = _simulationList.Count == 0 
                ? 0
                : _simulationList.Max(e => e.SimulationId);
            return ++lastId;
        }

        public async Task DeleteSimulation(long simulationId)
        {
            _simulationList.RemoveAll(e => e.SimulationId == simulationId);
        }

        public async Task SetSimulationPrices(SetSimulationPricesRequest request)
        {
            var simulation = _simulationList.FirstOrDefault(e => e.SimulationId == request.SimulationId);
            if (simulation != null) simulation.PriceMap = request.PriceMap;
        }
    }
}
