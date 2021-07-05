using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Sdk.Service;
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
        
        public BalanceHistoryTradeReaderJob(ISubscriber<IReadOnlyList<WalletTradeMessage>> subscriber,
            ILpWalletManagerGrpc walletManager,
            IPortfolioHandler portfolioHandler)
        {
            _walletManager = walletManager;
            _portfolioHandler = portfolioHandler;
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
                    
                    var listForSaveByWallet =
                        ourTrades.Select(elem => new Trade(elem.Trade.TradeUId, elem.BrokerId, elem.ClientId,
                            elem.WalletId, elem.Trade.InstrumentSymbol, elem.Trade.Side, elem.Trade.Price,
                            elem.Trade.BaseVolume, elem.Trade.QuoteVolume, elem.Trade.DateTime,
                            "spot-trades")).ToList();
                    
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
