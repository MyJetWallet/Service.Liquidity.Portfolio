using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using ME.Contracts.OutgoingMessages;
using Service.Liquidity.Engine.Domain.Models.Portfolio;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class LiquidityEngineTradeReaderJob : IStartable
    {
        private readonly IPortfolioStorage _portfolioStorage;
        public LiquidityEngineTradeReaderJob(ISubscriber<IReadOnlyList<PortfolioTrade>> subscriber,
            IPortfolioStorage portfolioStorage)
        {
            _portfolioStorage = portfolioStorage;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<PortfolioTrade> trades)
        {
            var localTrades = trades
                .Select(elem => new Trade(elem.TradeId, elem.AssociateWalletId,
                                                        elem.AssociateSymbol, elem.Side, elem.Price,
                                                        elem.BaseVolume, elem.QuoteVolume, elem.DateTime,
                                                        PortfolioTrade.TopicName))
                .ToList();
            foreach (var brokerId in trades.Select(elem => elem.AssociateBrokerId).Distinct())
            {
                await _portfolioStorage.UpdateBalances(brokerId, localTrades);
            }
            await _portfolioStorage.SaveTrades(localTrades);
        }

        public void Start()
        {
        }
    }
}
