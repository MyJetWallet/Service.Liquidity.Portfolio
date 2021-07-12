using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Postgres;
using Service.Liquidity.PortfolioHedger.ServiceBus;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class PortfolioHedgerTradeReaderJob : IStartable
    {
        private readonly IPortfolioHandler _portfolioHandler;
        
        public PortfolioHedgerTradeReaderJob(ISubscriber<IReadOnlyList<ExchangeTrade>> subscriber, IPortfolioHandler portfolioHandler)
        {
            _portfolioHandler = portfolioHandler;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<ExchangeTrade> trades)
        {
            var localTrades = trades.Select(elem => new Trade(elem.Id,
                elem.AssociateBrokerId,
                elem.Source,
                elem.AssociateSymbol,
                elem.Side,
                elem.Price,
                elem.Side == OrderSide.Buy ? elem.Volume : -elem.Volume,
                elem.Side == OrderSide.Buy ? -elem.OppositeVolume : elem.OppositeVolume,
                elem.Timestamp,
                PortfolioHedgerServiceBusSubscriber.TopicName))
                .ToList();
                
            await _portfolioHandler.HandleTradesAsync(localTrades);
        }


        public void Start()
        {
        }
    }
}
