using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Service.Liquidity.Engine.Domain.Models.Portfolio;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class LiquidityEngineTradeReaderJob : IStartable
    {
        public LiquidityEngineTradeReaderJob(ISubscriber<IReadOnlyList<PortfolioTrade>> subscriber)
        {
            subscriber.Subscribe(HandleTrades);
        }

        private ValueTask HandleTrades(IReadOnlyList<PortfolioTrade> trades)
        {
            //var x = trades;
            //var y = x;
            return default;
        }

        public void Start()
        {
        }
    }
}
