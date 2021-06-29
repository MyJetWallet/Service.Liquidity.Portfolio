using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.EntityFrameworkCore;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service;
using Service.AssetsDictionary.Client;
using Service.BalanceHistory.ServiceBus;
using Service.Liquidity.Engine.Grpc;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class TradeReaderJob: IStartable
    {
        private readonly ILpWalletManagerGrpc _walletManager;
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;
        
        public TradeReaderJob(ISubscriber<IReadOnlyList<WalletTradeMessage>> subscriber,
            ILpWalletManagerGrpc walletManager,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient)
        {
            _walletManager = walletManager;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<WalletTradeMessage> trades)
        {
            trades.Count.AddToActivityAsTag("trades count");
            try
            {
                var walletCollection = (await _walletManager.GetAllAsync()).Data.List;
                var listForSave = new List<PortfolioTrade>();

                walletCollection.ForEach(async wallet =>
                {
                    var ourTrades = trades.Where(trade => trade.WalletId == wallet.WalletId).ToList();
                    var listForSaveByWallet =
                        ourTrades.Select(elem => new PortfolioTrade(elem.Trade, elem.WalletId)).ToList();

                    listForSave.AddRange(listForSaveByWallet);

                    await UpdateBalances(wallet.BrokerId, listForSaveByWallet);
                });

                await SaveTrades(listForSave);
            }
            catch (Exception exception)
            {
                exception.AddToActivityAsJsonTag("exception");
            }

        }

        private async Task UpdateBalances(string brokerId, List<PortfolioTrade> listForSave)
        {
            brokerId.AddToActivityAsTag("brokerId");
            listForSave.AddToActivityAsJsonTag("listForSave");
            
            var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
            {
                BrokerId = brokerId
            });

            var balances = new Dictionary<(string, string), double>();

            listForSave.ForEach(trade =>
            {
                var tradeInstrument = instruments.FirstOrDefault(elem => elem.Symbol == trade.Symbol);
                var baseAsset = tradeInstrument?.BaseAsset;
                var quoteAsset = tradeInstrument?.QuoteAsset;

                if (balances.ContainsKey((trade.WalletId, baseAsset)))
                {
                    balances[(trade.WalletId, baseAsset)] += trade.BaseVolume;
                }
                else
                {
                    balances.Add((trade.WalletId, baseAsset), trade.BaseVolume);
                }

                if (balances.ContainsKey((trade.WalletId, quoteAsset)))
                {
                    balances[(trade.WalletId, quoteAsset)] += trade.QuoteVolume;
                }
                else
                {
                    balances.Add((trade.WalletId, quoteAsset), trade.QuoteVolume);
                }

            });

            var balanceList = balances.Select(balance => new AssetBalance()
            {
                WalletId = balance.Key.Item1,
                Asset = balance.Key.Item2,
                UpdateDate = DateTime.UtcNow,
                Volume = balance.Value
            }).ToList();
            
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.UpdateBalancesAsync(balanceList);
        }

        private async Task SaveTrades(IEnumerable<PortfolioTrade> listForSave)
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            await ctx.SaveTradesAsync(listForSave);
        }

        public void Start()
        {
        }
    }
}
