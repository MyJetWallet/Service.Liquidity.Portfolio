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
using Service.Liquidity.Portfolio.Postgres.Model;

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

        public async ValueTask UpdateBalancesAsync(List<Trade> trades)
        {
            var brokerId = trades.Select(elem => elem.BrokerId).Distinct().FirstOrDefault();
            
            brokerId.AddToActivityAsTag("brokerId");
            trades.AddToActivityAsJsonTag("listForSave");
            
            var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
            {
                BrokerId = brokerId
            });

            // clientId, walletId, asset
            var balanceDictionary = new Dictionary<(string, string, string), double>();

            trades.ForEach(trade =>
            {
                
                _logger.LogInformation($"Trade is on balance. Id: {trade.Id}");
                
                var tradeInstrument = instruments.FirstOrDefault(elem => elem.Symbol == trade.Symbol);
                var baseAsset = tradeInstrument?.BaseAsset;
                var quoteAsset = tradeInstrument?.QuoteAsset;

                if (balanceDictionary.ContainsKey((trade.ClientId, trade.WalletId, baseAsset)))
                {
                    balanceDictionary[(trade.ClientId, trade.WalletId, baseAsset)] += trade.BaseVolume;
                }
                else
                {
                    balanceDictionary.Add((trade.ClientId, trade.WalletId, baseAsset), trade.BaseVolume);
                }

                if (balanceDictionary.ContainsKey((trade.ClientId, trade.WalletId, quoteAsset)))
                {
                    balanceDictionary[(trade.ClientId, trade.WalletId, quoteAsset)] += trade.QuoteVolume;
                }
                else
                {
                    balanceDictionary.Add((trade.ClientId, trade.WalletId, quoteAsset), trade.QuoteVolume);
                }
            });

            var balanceList = balanceDictionary.Select(balance => new AssetBalance()
            {
                BrokerId = brokerId,
                ClientId = balance.Key.Item1,
                WalletId = balance.Key.Item2,
                Asset = balance.Key.Item3,
                UpdateDate = DateTime.UtcNow,
                Volume = balance.Value
            }).ToList();
            
            await UpdateBalancesAsync(balanceList);
        }

        public async ValueTask UpdateBalancesAsync(List<AssetBalance> balances)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            ctx.Balances
                .ToList()
                .ForEach(dbElem =>
                {
                    balances.ForEach(newElem =>
                    {
                        if (dbElem.WalletId == newElem.WalletId && dbElem.Asset == newElem.Asset)
                        {
                            newElem.Volume += dbElem.Volume;
                        }
                    });
                });
            await ctx.UpdateBalancesAsync(balances);
        }

        public async Task SaveChangeBalanceHistoryAsync(List<AssetBalance> balances, double volumeDifference)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.SaveChangeBalanceHistoryAsync(balances.Select(balance => new ChangeBalanceHistory()
            {
                BrokerId = balance.BrokerId,
                ClientId = balance.ClientId,
                WalletId = balance.WalletId,
                Asset = balance.Asset,
                UpdateDate = DateTime.UtcNow,
                VolumeDifference = volumeDifference
            }).ToList());
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
