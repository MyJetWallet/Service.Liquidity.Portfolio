using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service;
using Service.AssetsDictionary.Client;
using Service.BalanceHistory.ServiceBus;
using Service.Liquidity.Engine.Grpc;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class BalanceHistoryTradeReaderJob: IStartable
    {
        private readonly ILpWalletManagerGrpc _walletManager;
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;
        
        public BalanceHistoryTradeReaderJob(ISubscriber<IReadOnlyList<WalletTradeMessage>> subscriber,
            ILpWalletManagerGrpc walletManager,
            IPortfolioHandler portfolioHandler, ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient)
        {
            _walletManager = walletManager;
            _portfolioHandler = portfolioHandler;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<WalletTradeMessage> trades)
        {
            trades.Count.AddToActivityAsTag("trades count");
            try
            {
                var walletCollection = (await _walletManager.GetAllAsync()).Data.List;

                walletCollection.ForEach(async wallet =>
                {
                    var ourTrades = trades
                        .Where(trade => trade.WalletId == wallet.WalletId).
                        ToList();

                    var listForSaveByWallet = new List<Trade>();
                    foreach (var elem in ourTrades)
                    {
                        var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
                        {
                            BrokerId = elem.BrokerId
                        });

                        var instrument = instruments.FirstOrDefault(e => e.Symbol == elem.Trade.InstrumentSymbol);
                        
                        listForSaveByWallet.Add(new Trade(
                            elem.Trade.TradeUId, 
                            wallet.BrokerId,
                            elem.Trade.InstrumentSymbol,
                            instrument?.BaseAsset,
                            instrument?.QuoteAsset,
                            wallet.Name,
                            elem.Trade.Side, 
                            elem.Trade.Price,
                            elem.Trade.BaseVolume, 
                            elem.Trade.QuoteVolume, 
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
