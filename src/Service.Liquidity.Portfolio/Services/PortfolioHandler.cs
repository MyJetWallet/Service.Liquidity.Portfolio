using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.AssetsDictionary.Client;
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
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;
        private readonly TradeCacheStorage _tradeCacheStorage;

        private readonly object _locker = new object();

        private readonly List<AssetBalance> _localBalances = new List<AssetBalance>();

        public PortfolioHandler(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient,
            ILogger<PortfolioHandler> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            TradeCacheStorage tradeCacheStorage)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _tradeCacheStorage = tradeCacheStorage;
        }
        
        public async ValueTask HandleTradesAsync(List<Trade> trades)
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

        private void HandleTradeAsync(Trade trade)
        {
            using var activity = MyTelemetry.StartActivity("HandleTradeAsync");
            
            var cache = _tradeCacheStorage.GetFromCache(trade.TradeId);
            if (cache != null)
            {
                trade.ErrorMessage = cache.ErrorMessage;
                return;
            }

            try
            {
                UpdateBalanceByTrade(trade);
            }
            catch (Exception exception)
            {
                trade.ErrorMessage = exception.Message;
                exception.FailActivity();
                throw;
            }
            finally
            {
                _tradeCacheStorage.SaveInCache(trade);
            }
        }

        private async ValueTask SetUsdProjection(List<Trade> trades)
        {
            trades.ForEach(async trade =>
            {
                var instrument = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
                {
                    BrokerId = trade.BrokerId
                });

                var projectionOnBaseAsset = await _anotherAssetProjectionService.GetProjectionAsync(new GetProjectionRequest()
                {
                    BrokerId = trade.BrokerId,
                    FromAsset = instrument.FirstOrDefault(elem => elem.Symbol == trade.Symbol)?.BaseAsset,
                    FromVolume = trade.BaseVolume,
                    ToAsset = "USD"
                });
                trade.BaseVolumeInUsd = projectionOnBaseAsset.ProjectionVolume;
                
                var projectionOnQuoteAsset = await _anotherAssetProjectionService.GetProjectionAsync(new GetProjectionRequest()
                {
                    BrokerId = trade.BrokerId,
                    FromAsset = instrument.FirstOrDefault(elem => elem.Symbol == trade.Symbol)?.QuoteAsset,
                    FromVolume = trade.QuoteVolume,
                    ToAsset = "USD"
                });
                trade.QuoteVolumeInUsd = projectionOnQuoteAsset.ProjectionVolume;
            });
        }

        private async ValueTask SaveTrades(List<Trade> trades)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.SaveTradesAsync(trades);
        }

        private void UpdateBalanceByTrade(Trade trade)
        {
            var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
            {
                BrokerId = trade.BrokerId
            });

            var tradeInstrument = instruments.FirstOrDefault(elem => elem.Symbol == trade.Symbol);
            if (tradeInstrument == null)
            {
                _logger.LogError("Not found instrument for trade: {tradeJson}", JsonConvert.SerializeObject(trade));
                throw new Exception("Instrument not found.");
            }
            var baseAsset = tradeInstrument?.BaseAsset;
            var quoteAsset = tradeInstrument?.QuoteAsset;

            var balanceList = new List<AssetBalance>();

            var baseAssetBalance = new AssetBalance()
            {
                BrokerId = trade.BrokerId,
                WalletName = trade.WalletName,
                Asset = baseAsset,
                UpdateDate = DateTime.UtcNow,
                Volume = trade.BaseVolume
            };
            var quoteAssetBalance = new AssetBalance()
            {
                BrokerId = trade.BrokerId,
                WalletName = trade.WalletName,
                Asset = quoteAsset,
                UpdateDate = DateTime.UtcNow,
                Volume = trade.QuoteVolume
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

        public async Task<List<Trade>> GetTrades(long lastId, int batchSize, string assetFilter)
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
                    .Where(trade => trade.Id < lastId && trade.Symbol.Contains(assetFilter))
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
                .Where(trade => trade.Symbol.Contains(assetFilter))
                .OrderByDescending(trade => trade.Id)
                .Take(batchSize)
                .ToList();
        }
    }
}
