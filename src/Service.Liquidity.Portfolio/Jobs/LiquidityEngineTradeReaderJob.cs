using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain.Orders;
using Service.Liquidity.Engine.Domain.Models.Portfolio;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class LiquidityEngineTradeReaderJob : IStartable
    {
        private readonly IPortfolioHandler _portfolioHandler;
        public LiquidityEngineTradeReaderJob(ISubscriber<IReadOnlyList<PortfolioTrade>> subscriber,
            IPortfolioHandler portfolioHandler)
        {
            _portfolioHandler = portfolioHandler;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<PortfolioTrade> trades)
        {
            var localTrades = trades
                .Where(elem => !elem.IsInternal)
                .Select(elem => new Trade(elem.TradeId,
                    elem.AssociateBrokerId,
                    elem.AssociateClientId,
                    elem.AssociateWalletId,
                    elem.AssociateSymbol,
                    elem.Side,
                    elem.Price,
                    elem.Side == OrderSide.Buy ? elem.BaseVolume : -elem.BaseVolume,
                    elem.Side == OrderSide.Buy ? -elem.QuoteVolume : elem.QuoteVolume,
                    elem.DateTime,
                    PortfolioTrade.TopicName))
                .ToList();
            await _portfolioHandler.HandleTradesAsync(localTrades);
        }

        public void Start()
        {
        }
    }
}
