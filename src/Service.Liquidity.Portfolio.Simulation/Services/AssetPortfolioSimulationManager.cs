﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc.Simulation.Models;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.Portfolio.Tests;
using Service.Liquidity.Portfolio.Simulation.Models;

namespace Service.Liquidity.Portfolio.Simulation.Services
{
    public class AssetPortfolioSimulationManager
    {
        private readonly List<SimulationStorage> _simulationStorages = new();

        public async Task<PortfolioSimulation> CreateNewSimulation()
        {
            var noSqlDataReader = new MyNoSqlServerDataReaderMock();
            var indexPricesClientMock = new IndexPricesClientMock();
            var assetPortfolioMath = new AssetPortfolioMath();
            var assetPortfolioManager = new AssetPortfolioManager(Program.LogFactory.CreateLogger<AssetPortfolioManager>(),
                noSqlDataReader, indexPricesClientMock, assetPortfolioMath);
            var simulationEntity = new PortfolioSimulation(GenerateNewSimulationId());

            await assetPortfolioManager.ReloadBalance(null);

            var newSimulation = new SimulationStorage(assetPortfolioMath, 
                assetPortfolioManager, 
                indexPricesClientMock,
                simulationEntity);
            _simulationStorages.Add(newSimulation);
            
            return simulationEntity;
        }

        public async Task<List<PortfolioSimulation>> GetSimulationList()
        {
            return _simulationStorages.Select(e => e.SimulationEntity).ToList();
        }

        public async Task<PortfolioSimulation> GetSimulation(long simulationId)
        {
            return _simulationStorages.Select(e => e.SimulationEntity).FirstOrDefault(e => e.SimulationId == simulationId);
        }

        public async Task ReportSimulationTrade(ReportSimulationTradeRequest request)
        {
            SetIndexPrices(request.SimulationId);
            
            var simulation = _simulationStorages.FirstOrDefault(e => e.SimulationEntity.SimulationId == request.SimulationId);
            if (simulation == null)
                throw new Exception($"Simulation with id {request.SimulationId} not found");
            
            var baseAssetBalance = simulation.AssetPortfolioManager.GetBalanceEntity(simulation?.SimulationEntity.AssetBalances,
                AssetPortfolioManager.Broker, request.WalletName, request.BaseAsset);
            var quoteAssetBalance = simulation.AssetPortfolioManager.GetBalanceEntity(simulation?.SimulationEntity.AssetBalances,
                AssetPortfolioManager.Broker, request.WalletName, request.QuoteAsset);

            decimal baseAssetPrice, quoteAssetPrice;
            try
            {
                baseAssetPrice = simulation.IndexPricesClientMock.PriceMap[request.BaseAsset];
                quoteAssetPrice = simulation.IndexPricesClientMock.PriceMap[request.QuoteAsset];
            }
            catch (Exception)
            {
                throw new Exception("Prices not found.");
            }
            
            var baseAssetDiff = new AssetBalanceDifference(AssetPortfolioManager.Broker, request.WalletName, request.BaseAsset,
                request.BaseVolume, request.BaseVolume * baseAssetPrice, baseAssetPrice);
            var quoteAssetDiff = new AssetBalanceDifference(AssetPortfolioManager.Broker, request.WalletName, request.QuoteAsset,
                request.QuoteVolume, request.QuoteVolume * quoteAssetPrice, quoteAssetPrice);
            
            simulation.AssetPortfolioMath.UpdateBalance(baseAssetBalance, baseAssetDiff);
            simulation.AssetPortfolioMath.UpdateBalance(quoteAssetBalance, quoteAssetDiff);

            simulation.AssetPortfolioManager.FixReleasedPnl(simulation.SimulationEntity.Portfolio, simulation.SimulationEntity.AssetBalances);
            simulation.AssetPortfolioManager.UpdatePortfolio(simulation.SimulationEntity.Portfolio, simulation.SimulationEntity.AssetBalances);
            var trade = new AssetPortfolioTrade()
            {
                DateTime = DateTime.UtcNow,
                BaseAsset = request.BaseAsset,
                QuoteAsset = request.QuoteAsset,
                BaseVolume = request.BaseVolume,
                QuoteVolume = request.QuoteVolume,
                WalletName = request.WalletName,
                BaseAssetPriceInUsd = baseAssetPrice,
                QuoteAssetPriceInUsd = quoteAssetPrice
            };
            
            simulation.SimulationEntity.Trades.Add(trade);
        }

        private void SetIndexPrices(long simulationId)
        {
            var simulation = _simulationStorages.FirstOrDefault(e => e.SimulationEntity.SimulationId == simulationId);

            if (simulation != null)
                simulation.IndexPricesClientMock.PriceMap = _simulationStorages
                    .First(e => e.SimulationEntity.SimulationId == simulationId).SimulationEntity.PriceMap;
        }

        private long GenerateNewSimulationId()
        {
            var lastId = _simulationStorages.Count == 0 
                ? 0
                : _simulationStorages.Max(e => e.SimulationEntity.SimulationId);
            return ++lastId;
        }

        public async Task DeleteSimulation(long simulationId)
        {
            _simulationStorages.RemoveAll(e => e.SimulationEntity.SimulationId == simulationId);
        }

        public async Task SetSimulationPrices(SetSimulationPricesRequest request)
        {
            var simulation = _simulationStorages.FirstOrDefault(e => e.SimulationEntity.SimulationId == request.SimulationId);
            if (simulation != null) simulation.SimulationEntity.PriceMap = request.PriceMap;
        }
    }
}