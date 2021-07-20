using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Service.AssetsDictionary.Client;
using Service.BalanceHistory.ServiceBus;
using Service.Liquidity.Engine.Domain.Models.NoSql;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class BalanceHistoryTradeReaderJob: IStartable
    {
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;
        private readonly IMyNoSqlServerDataReader<LpWalletNoSql> _noSqlDataReader;
        
        public BalanceHistoryTradeReaderJob(ISubscriber<IReadOnlyList<WalletTradeMessage>> subscriber,
            IPortfolioHandler portfolioHandler, ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient,
            IMyNoSqlServerDataReader<LpWalletNoSql> noSqlDataReader)
        {
            _portfolioHandler = portfolioHandler;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            _noSqlDataReader = noSqlDataReader;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<WalletTradeMessage> trades)
        {
            trades.Count.AddToActivityAsTag("trades count");
            try
            {
                var walletCollection = _noSqlDataReader.Get().ToList();

                walletCollection.ForEach(async wallet =>
                {
                    var ourTrades = trades
                        .Where(trade => trade.WalletId == wallet.Wallet.WalletId).
                        ToList();

                    var listForSaveByWallet = new List<AssetPortfolioTrade>();
                    foreach (var elem in ourTrades)
                    {
                        var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
                        {
                            BrokerId = elem.BrokerId
                        });

                        var instrument = instruments.FirstOrDefault(e => e.Symbol == elem.Trade.InstrumentSymbol);

                        listForSaveByWallet.Add(new AssetPortfolioTrade(
                            elem.Trade.TradeUId,
                            wallet.Wallet.BrokerId,
                            elem.Trade.InstrumentSymbol,
                            instrument?.BaseAsset,
                            instrument?.QuoteAsset,
                            wallet.Wallet.Name,
                            elem.Trade.Side, 
                            Convert.ToDecimal(elem.Trade.Price),
                            Convert.ToDecimal(elem.Trade.BaseVolume), 
                            Convert.ToDecimal(elem.Trade.QuoteVolume), 
                            elem.Trade.DateTime,
                            "spot-trades"));
                    }
                    await _portfolioHandler.HandleTradesAsync(listForSaveByWallet);
                });
            }
            catch (Exception exception)
            {
                exception.AddToActivityAsJsonTag("exception");
            }
        }

        public void Start()
        {
        }
    }
}
