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
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services
{
    public class PortfolioStorage : IPortfolioStorage
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<PortfolioStorage> _logger;
        
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;

        public PortfolioStorage(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient,
            ILogger<PortfolioStorage> logger)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            _logger = logger;
        }

        public async ValueTask SaveTrades(List<Trade> trades)
        {
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
    }
}
