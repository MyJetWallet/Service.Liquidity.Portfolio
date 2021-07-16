using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services
{
    public class PortfolioHandler : IPortfolioHandler
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<PortfolioHandler> _logger;
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;
        private readonly TradeCacheStorage _tradeCacheStorage;
        private readonly IPublisher<AssetPortfolioTrade> _publisher;
        private readonly IAssetPortfolioBalanceStorage _assetPortfolioBalanceStorage;

        private readonly object _locker = new object();

        private readonly List<AssetBalance> _localBalances = new List<AssetBalance>();

        public PortfolioHandler(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ILogger<PortfolioHandler> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            TradeCacheStorage tradeCacheStorage,
            IPublisher<AssetPortfolioTrade> publisher,
            IAssetPortfolioBalanceStorage assetPortfolioBalanceStorage)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _tradeCacheStorage = tradeCacheStorage;
            _publisher = publisher;
            _assetPortfolioBalanceStorage = assetPortfolioBalanceStorage;
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
                
                var projectionOnQuoteAsset = await _anotherAssetProjectionService.GetProjectionAsync(new GetProjectionRequest()
                {
                    BrokerId = trade.AssociateBrokerId,
                    FromAsset = trade.QuoteAsset,
                    FromVolume = trade.QuoteVolume,
                    ToAsset = "USD"
                });
                trade.QuoteVolumeInUsd = projectionOnQuoteAsset.ProjectionVolume;
            });
        }

        private async ValueTask SaveTrades(List<AssetPortfolioTrade> trades)
        {
            trades.ForEach(async elem =>
            {
                await _publisher.PublishAsync(elem);
            });
        }

        private void UpdateBalanceByTrade(AssetPortfolioTrade assetPortfolioTrade)
        {
            var baseAsset = assetPortfolioTrade.BaseAsset;
            var quoteAsset = assetPortfolioTrade.QuoteAsset;

            var balanceList = new List<AssetBalance>();

            var baseAssetBalance = new AssetBalance()
            {
                BrokerId = assetPortfolioTrade.AssociateBrokerId,
                WalletName = assetPortfolioTrade.WalletName,
                Asset = baseAsset,
                UpdateDate = DateTime.UtcNow,
                Volume = assetPortfolioTrade.BaseVolume
            };
            var quoteAssetBalance = new AssetBalance()
            {
                BrokerId = assetPortfolioTrade.AssociateBrokerId,
                WalletName = assetPortfolioTrade.WalletName,
                Asset = quoteAsset,
                UpdateDate = DateTime.UtcNow,
                Volume = assetPortfolioTrade.QuoteVolume
            };
            balanceList.Add(baseAssetBalance);
            balanceList.Add(quoteAssetBalance);
            
            UpdateBalance(balanceList);
        }

        public void UpdateBalance(List<AssetBalance> differenceBalances)
        {
            differenceBalances.ForEach(elem =>
            {
                _assetPortfolioBalanceStorage.UpdateAssetPortfolioBalanceAsync(elem);
            });
            
        }
        
        public async Task SaveChangeBalanceHistoryAsync(ChangeBalanceHistory balanceHistory)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.SaveChangeBalanceHistoryAsync(balanceHistory);
        }

        public async Task<List<ChangeBalanceHistory>> GetHistories()
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            return ctx.ChangeBalanceHistories.ToList();
        }
    }
}
