﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class PortfolioHandler : IPortfolioHandler
    {
        private readonly ILogger<PortfolioHandler> _logger;
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;
        private readonly TradeCacheStorage _tradeCacheStorage;
        private readonly IPublisher<AssetPortfolioTrade> _tradePublisher;
        private readonly IPublisher<ChangeBalanceHistory> _changeBalanceHistoryPublisher;
        private readonly IAssetPortfolioBalanceStorage _portfolioBalanceStorage;

        public PortfolioHandler(ILogger<PortfolioHandler> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            TradeCacheStorage tradeCacheStorage,
            IPublisher<AssetPortfolioTrade> tradePublisher,
            IAssetPortfolioBalanceStorage portfolioBalanceStorage,
            IPublisher<ChangeBalanceHistory> changeBalanceHistoryPublisher)
        {
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _tradeCacheStorage = tradeCacheStorage;
            _tradePublisher = tradePublisher;
            _portfolioBalanceStorage = portfolioBalanceStorage;
            _changeBalanceHistoryPublisher = changeBalanceHistoryPublisher;
        }
        
        public async ValueTask HandleTradesAsync(List<AssetPortfolioTrade> trades)
        {
            using var activity = MyTelemetry.StartActivity("HandleTradesAsync");
            trades.Count.AddToActivityAsTag("Trades count");
            
            _logger.LogInformation("Receive trades count: {count}", trades.Count);

            await SetUsdProjection(trades);
            
            foreach (var trade in trades)
            {
                try
                {
                    HandleTradeAsync(trade);
                }
                catch (Exception exception)
                {
                    _logger.LogError("Catch exception: {errorMessaeJson} in trade {tradeJson}", exception.Message, JsonConvert.SerializeObject(trade));
                }
            }
            
            var tradesWithSuccess = trades.Where(trade => trade.ErrorMessage == string.Empty).ToList();
            tradesWithSuccess.Count.AddToActivityAsTag("tradesWithSuccess");
            
            var tradesWithErrors = trades.Where(trade => trade.ErrorMessage != string.Empty).ToList();
            tradesWithErrors.Count.AddToActivityAsTag("tradesWithErrors");
            
            await SaveTrades(trades);
            
            _logger.LogInformation("Handled trades with errors: {countWithErrors}; with success: {countWithSuccess}", tradesWithErrors.Count, tradesWithSuccess.Count);
        }

        private void HandleTradeAsync(AssetPortfolioTrade assetPortfolioTrade)
        {
            using var activity = MyTelemetry.StartActivity("HandleTradeAsync");
            
            var cache = _tradeCacheStorage.GetFromCache(assetPortfolioTrade.TradeId);
            if (cache != null)
            {
                assetPortfolioTrade.ErrorMessage = cache.ErrorMessage;
                return;
            }

            try
            {
                UpdateBalanceByTrade(assetPortfolioTrade);
            }
            catch (Exception exception)
            {
                assetPortfolioTrade.ErrorMessage = exception.Message;
                exception.FailActivity();
                throw;
            }
            finally
            {
                _tradeCacheStorage.SaveInCache(assetPortfolioTrade);
            }
        }

        private async ValueTask SetUsdProjection(List<AssetPortfolioTrade> trades)
        {
            trades.ForEach(async trade =>
            {
                
                var projectionOnBaseAsset = await _anotherAssetProjectionService.GetProjectionAsync(new GetProjectionRequest()
                {
                    BrokerId = trade.AssociateBrokerId,
                    FromAsset = trade.BaseAsset,
                    FromVolume = trade.BaseVolume,
                    ToAsset = "USD"
                });
                trade.BaseVolumeInUsd = projectionOnBaseAsset.ProjectionVolume;
                trade.BaseAssetPriceInUsd = 0; //todo: get prices from https://monfex.atlassian.net/browse/SPOTLIQ-119

                var projectionOnQuoteAsset = await _anotherAssetProjectionService.GetProjectionAsync(new GetProjectionRequest()
                {
                    BrokerId = trade.AssociateBrokerId,
                    FromAsset = trade.QuoteAsset,
                    FromVolume = trade.QuoteVolume,
                    ToAsset = "USD"
                });
                trade.QuoteVolumeInUsd = projectionOnQuoteAsset.ProjectionVolume;
                trade.QuoteVolumeInUsd = 0; //todo: get prices from https://monfex.atlassian.net/browse/SPOTLIQ-119
            });
        }

        private async ValueTask SaveTrades(List<AssetPortfolioTrade> trades)
        {
            trades.ForEach(async elem =>
            {
                await _tradePublisher.PublishAsync(elem);
            });
        }

        private void UpdateBalanceByTrade(AssetPortfolioTrade assetPortfolioTrade)
        {
            var baseAsset = assetPortfolioTrade.BaseAsset;
            var quoteAsset = assetPortfolioTrade.QuoteAsset;

            var balanceList = new List<AssetBalanceDifference>();
            
            var baseAssetBalance = new AssetBalanceDifference(assetPortfolioTrade.AssociateBrokerId,
                assetPortfolioTrade.WalletName,
                baseAsset, 
                assetPortfolioTrade.BaseVolume, 
                assetPortfolioTrade.BaseVolumeInUsd,
                assetPortfolioTrade.BaseAssetPriceInUsd);
            
            var quoteAssetBalance = new AssetBalanceDifference(assetPortfolioTrade.AssociateBrokerId,
                assetPortfolioTrade.WalletName,
                quoteAsset, 
                assetPortfolioTrade.QuoteVolume, 
                assetPortfolioTrade.QuoteVolumeInUsd,
                assetPortfolioTrade.QuoteAssetPriceInUsd);
            
            balanceList.Add(baseAssetBalance);
            balanceList.Add(quoteAssetBalance);
            
            var pnlByAsset = _portfolioBalanceStorage.UpdateBalance(balanceList);
            assetPortfolioTrade.ReleasePnl = pnlByAsset.Select(elem => PnlByAsset.Create(elem.Key, elem.Value)).ToList();
        }
        
        public async Task SaveChangeBalanceHistoryAsync(ChangeBalanceHistory balanceHistory)
        {
            await _changeBalanceHistoryPublisher.PublishAsync(balanceHistory);
        }
    }
}
