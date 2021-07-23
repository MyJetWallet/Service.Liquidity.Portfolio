using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain.Orders;
using Service.AssetsDictionary.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.PortfolioHedger.ServiceBus;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class PortfolioHedgerTradeReaderJob : IStartable
    {
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;

        public PortfolioHedgerTradeReaderJob(ISubscriber<IReadOnlyList<TradeMessage>> subscriber, 
            IPortfolioHandler portfolioHandler, 
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient)
        {
            _portfolioHandler = portfolioHandler;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<TradeMessage> trades)
        {

            var localTrades = new List<AssetPortfolioTrade>();

            foreach (var elem in trades)
            {
                localTrades.Add(new AssetPortfolioTrade(elem.Id,
                    elem.AssociateBrokerId,
                    elem.AssociateSymbol,
                    elem.BaseAsset,
                    elem.QuoteAsset,
                    elem.AssociateWalletId,
                    elem.Side,
                    Convert.ToDecimal(elem.Price),
                    Convert.ToDecimal(elem.Side == OrderSide.Buy ? elem.Volume : -elem.Volume),
                    Convert.ToDecimal(elem.Side == OrderSide.Buy ? -elem.OppositeVolume : elem.OppositeVolume),
                    elem.Timestamp,
                    TradeMessage.TopicName)
                {
                    Comment = elem.Comment,
                    User = elem.User
                });
            }

            await _portfolioHandler.HandleTradesAsync(localTrades);
        }


        public void Start()
        {
        }
    }
}
