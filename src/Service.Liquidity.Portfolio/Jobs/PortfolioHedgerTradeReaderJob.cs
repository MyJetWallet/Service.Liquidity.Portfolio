using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using Service.AssetsDictionary.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Postgres;
using Service.Liquidity.PortfolioHedger.ServiceBus;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class PortfolioHedgerTradeReaderJob : IStartable
    {
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;

        public PortfolioHedgerTradeReaderJob(ISubscriber<IReadOnlyList<ExchangeTradeMessage>> subscriber, 
            IPortfolioHandler portfolioHandler, 
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient)
        {
            _portfolioHandler = portfolioHandler;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<ExchangeTradeMessage> trades)
        {

            var localTrades = new List<PortfolioTrade>();

            foreach (var elem in trades)
            {
                localTrades.Add(new PortfolioTrade(elem.Id,
                    elem.AssociateBrokerId,
                    elem.AssociateSymbol,
                    elem.BaseAsset,
                    elem.QuoteAsset,
                    elem.Source,
                    elem.Side,
                    elem.Price,
                    elem.Side == OrderSide.Buy ? elem.Volume : -elem.Volume,
                    elem.Side == OrderSide.Buy ? -elem.OppositeVolume : elem.OppositeVolume,
                    elem.Timestamp,
                    ExchangeTradeMessage.TopicName)
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
