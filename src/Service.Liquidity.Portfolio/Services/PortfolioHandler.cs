using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using ME.Contracts.OutgoingMessages;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.IndexPrices.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Services
{
    public class PortfolioHandler : IPortfolioHandler
    {
        private readonly ILogger<PortfolioHandler> _logger;
        private readonly TradeCacheStorage _tradeCacheStorage;
        private readonly IPublisher<AssetPortfolioTrade> _tradePublisher;
        private readonly IPublisher<ChangeBalanceHistory> _changeBalanceHistoryPublisher;
        private readonly IPublisher<ManualSettlement> _manualSettlementPublisher;
        private readonly AssetPortfolioManager _portfolioManager;
        private readonly IIndexPricesClient _indexPricesClient;
        private readonly PortfolioMetrics _portfolioMetrics;

        public PortfolioHandler(ILogger<PortfolioHandler> logger,
            TradeCacheStorage tradeCacheStorage,
            IPublisher<AssetPortfolioTrade> tradePublisher,
            AssetPortfolioManager portfolioManager,
            IPublisher<ChangeBalanceHistory> changeBalanceHistoryPublisher,
            IIndexPricesClient indexPricesClient,
            PortfolioMetrics portfolioMetrics,
            IPublisher<ManualSettlement> manualSettlementPublisher)
        {
            _logger = logger;
            _tradeCacheStorage = tradeCacheStorage;
            _tradePublisher = tradePublisher;
            _portfolioManager = portfolioManager;
            _changeBalanceHistoryPublisher = changeBalanceHistoryPublisher;
            _indexPricesClient = indexPricesClient;
            _portfolioMetrics = portfolioMetrics;
            _manualSettlementPublisher = manualSettlementPublisher;
        }
        
        public async ValueTask HandleTradesAsync(List<AssetPortfolioTrade> trades)
        {
            using var activity = MyTelemetry.StartActivity("HandleTradesAsync");
            trades.Count.AddToActivityAsTag("Trades count");
            _logger.LogInformation("Receive trades count: {count}", trades.Count);

            if (!trades.Any())
            {
                return;
            }

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
            trades.ForEach(trade =>
            {
                var (baseIndexPrice, baseUsdVolume) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(trade.BaseAsset, trade.BaseVolume);
                trade.BaseVolumeInUsd = baseUsdVolume;
                trade.BaseAssetPriceInUsd = baseIndexPrice.UsdPrice;
                
                var secondUsdPrice = Math.Abs(baseIndexPrice.UsdPrice * trade.BaseVolume / trade.QuoteVolume);
                var secondUsdVolume = trade.QuoteVolume * secondUsdPrice;
                
                trade.QuoteVolumeInUsd = secondUsdVolume;
                trade.QuoteAssetPriceInUsd = secondUsdPrice; 
            });
        }

        private async ValueTask SaveTrades(List<AssetPortfolioTrade> trades)
        {
            foreach (var trade in trades)
            {
                try
                {
                    _portfolioMetrics.SetTradeMetrics(trade);
                    await _tradePublisher.PublishAsync(trade);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
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


            if (!string.IsNullOrWhiteSpace(assetPortfolioTrade.FeeAsset) && assetPortfolioTrade.FeeVolume != 0)
            {
                var (feeIndexPrice, feeUsdVolume) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(assetPortfolioTrade.FeeAsset, assetPortfolioTrade.FeeVolume);
                var feeAssetDiff = new AssetBalanceDifference(assetPortfolioTrade.AssociateBrokerId,
                    assetPortfolioTrade.WalletName,
                    assetPortfolioTrade.FeeAsset, 
                    assetPortfolioTrade.FeeVolume, 
                    feeUsdVolume,
                    feeIndexPrice.UsdPrice);
                
                balanceList.Add(feeAssetDiff);
            }
            
             
            _portfolioManager.UpdateBalance(balanceList);
            assetPortfolioTrade.TotalReleasePnl = _portfolioManager.FixReleasedPnl();
        }
        
        public async Task SaveChangeBalanceHistoryAsync(ChangeBalanceHistory balanceHistory)
        {
            _portfolioMetrics.SetChangeBalanceMetrics(balanceHistory);
            await _changeBalanceHistoryPublisher.PublishAsync(balanceHistory);
        }
        
        public async Task SaveManualSettlementHistoryAsync(ManualSettlement settlement)
        {
            await _manualSettlementPublisher.PublishAsync(settlement);
        }
    }
}
