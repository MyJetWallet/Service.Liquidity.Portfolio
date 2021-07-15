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
        private readonly IPublisher<PortfolioTrade> _publisher;

        private readonly object _locker = new object();

        private readonly List<AssetBalance> _localBalances = new List<AssetBalance>();

        public PortfolioHandler(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ILogger<PortfolioHandler> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            TradeCacheStorage tradeCacheStorage, IPublisher<PortfolioTrade> publisher)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _tradeCacheStorage = tradeCacheStorage;
            _publisher = publisher;
        }
        
        public async ValueTask HandleTradesAsync(List<PortfolioTrade> trades)
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

        private void HandleTradeAsync(PortfolioTrade portfolioTrade)
        {
            using var activity = MyTelemetry.StartActivity("HandleTradeAsync");
            
            var cache = _tradeCacheStorage.GetFromCache(portfolioTrade.TradeId);
            if (cache != null)
            {
                portfolioTrade.ErrorMessage = cache.ErrorMessage;
                return;
            }

            try
            {
                UpdateBalanceByTrade(portfolioTrade);
            }
            catch (Exception exception)
            {
                portfolioTrade.ErrorMessage = exception.Message;
                exception.FailActivity();
                throw;
            }
            finally
            {
                _tradeCacheStorage.SaveInCache(portfolioTrade);
            }
        }

        private async ValueTask SetUsdProjection(List<PortfolioTrade> trades)
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

        private async ValueTask SaveTrades(List<PortfolioTrade> trades)
        {
            //await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            //await ctx.SaveTradesAsync(trades);
            
            trades.ForEach(async elem =>
            {
                await _publisher.PublishAsync(elem);
            });
        }

        private void UpdateBalanceByTrade(PortfolioTrade portfolioTrade)
        {
            var baseAsset = portfolioTrade.BaseAsset;
            var quoteAsset = portfolioTrade.QuoteAsset;

            var balanceList = new List<AssetBalance>();

            var baseAssetBalance = new AssetBalance()
            {
                BrokerId = portfolioTrade.AssociateBrokerId,
                WalletName = portfolioTrade.WalletName,
                Asset = baseAsset,
                UpdateDate = DateTime.UtcNow,
                Volume = portfolioTrade.BaseVolume
            };
            var quoteAssetBalance = new AssetBalance()
            {
                BrokerId = portfolioTrade.AssociateBrokerId,
                WalletName = portfolioTrade.WalletName,
                Asset = quoteAsset,
                UpdateDate = DateTime.UtcNow,
                Volume = portfolioTrade.QuoteVolume
            };
            balanceList.Add(baseAssetBalance);
            balanceList.Add(quoteAssetBalance);
            
            UpdateBalance(balanceList);
        }

        public void UpdateBalance(List<AssetBalance> differenceBalances)
        {
            lock (_locker)
            {
                foreach (var difference in differenceBalances)
                {
                    var balance = _localBalances.FirstOrDefault(elem =>
                        elem.WalletName == difference.WalletName && elem.Asset == difference.Asset);
                    if (balance == null)
                    {
                        balance = difference;
                        _localBalances.Add(balance);
                    }
                    else
                    {
                        balance.Volume += difference.Volume;
                    }
                }
            }
        }
        
        public List<AssetBalance> GetBalancesSnapshot()
        {
            using var a = MyTelemetry.StartActivity("GetBalancesSnapshot");
            
            lock(_locker)
            {
                var newList = _localBalances.Select(elem => elem.Copy()).ToList();
                return newList;
            }
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

        public async Task<List<PortfolioTrade>> GetTrades(long lastId, int batchSize, string assetFilter)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            if (lastId != 0)
            {
                if (string.IsNullOrWhiteSpace(assetFilter))
                {
                    return ctx.Trades
                        .Where(trade => trade.Id < lastId)
                        .OrderByDescending(trade => trade.Id)
                        .Take(batchSize)
                        .ToList();
                }
                return ctx.Trades
                    .Where(trade => trade.Id < lastId && (trade.BaseAsset.Contains(assetFilter) || trade.QuoteAsset.Contains(assetFilter)))
                    .OrderByDescending(trade => trade.Id)
                    .Take(batchSize)
                    .ToList();
            }
            if (string.IsNullOrWhiteSpace(assetFilter))
            {
                return ctx.Trades
                    .OrderByDescending(trade => trade.Id)
                    .Take(batchSize)
                    .ToList();
            }
            return ctx.Trades
                .Where(trade => trade.BaseAsset.Contains(assetFilter) || trade.QuoteAsset.Contains(assetFilter))
                .OrderByDescending(trade => trade.Id)
                .Take(batchSize)
                .ToList();
        }
    }
}
