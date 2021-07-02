using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service;
using Service.AssetsDictionary.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services
{
    public class PortfolioStorage : IPortfolioStorage
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<PortfolioStorage> _logger;
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;

        public PortfolioStorage(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient,
            ILogger<PortfolioStorage> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
        }

        public async ValueTask SaveTrades(List<Trade> trades)
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
            
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.SaveTradesAsync(trades);
        }

        public async ValueTask UpdateBalances(List<Trade> trades)
        {
            var brokerId = trades.Select(elem => elem.BrokerId).Distinct().FirstOrDefault();
            
            brokerId.AddToActivityAsTag("brokerId");
            trades.AddToActivityAsJsonTag("listForSave");
            
            var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
            {
                BrokerId = brokerId
            });

            // clientId, walletId, asset
            var balances = new Dictionary<(string, string, string), double>();

            trades.ForEach(trade =>
            {
                var tradeInstrument = instruments.FirstOrDefault(elem => elem.Symbol == trade.Symbol);
                var baseAsset = tradeInstrument?.BaseAsset;
                var quoteAsset = tradeInstrument?.QuoteAsset;

                if (balances.ContainsKey((trade.ClientId, trade.WalletId, baseAsset)))
                {
                    balances[(trade.ClientId, trade.WalletId, baseAsset)] += trade.BaseVolume;
                }
                else
                {
                    balances.Add((trade.ClientId, trade.WalletId, baseAsset), trade.BaseVolume);
                }

                if (balances.ContainsKey((trade.ClientId, trade.WalletId, quoteAsset)))
                {
                    balances[(trade.ClientId, trade.WalletId, quoteAsset)] += trade.QuoteVolume;
                }
                else
                {
                    balances.Add((trade.ClientId, trade.WalletId, quoteAsset), trade.QuoteVolume);
                }
            });

            var balanceList = balances.Select(balance => new AssetBalance()
            {
                BrokerId = brokerId,
                ClientId = balance.Key.Item1,
                WalletId = balance.Key.Item2,
                Asset = balance.Key.Item3,
                UpdateDate = DateTime.UtcNow,
                Volume = balance.Value
            }).ToList();
            
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.UpdateBalancesAsync(balanceList);
        }

        public async ValueTask UpdateBalances(List<AssetBalance> balances)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.UpdateBalancesAsync(balances);
        }

        public async Task<List<AssetBalance>> GetBalances()
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            return ctx.Balances.ToList();
        }

        public async Task<List<Trade>> GetTrades(long lastId, int batchSize)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            if (lastId != 0)
            {
                return ctx.Trades
                    .Where(trade => trade.Id < lastId)
                    .OrderByDescending(trade => trade.Id)
                    .Take(batchSize)
                    .ToList();
            }
            return ctx.Trades
                .OrderByDescending(trade => trade.Id)
                .Take(batchSize)
                .ToList();
        }
    }
}
