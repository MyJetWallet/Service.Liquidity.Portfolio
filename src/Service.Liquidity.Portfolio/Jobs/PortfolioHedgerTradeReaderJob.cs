using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.PortfolioHedger.Domain.Models;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class PortfolioHedgerTradeReaderJob : IStartable
    {
        private readonly ITradeHandler _tradeHandler;

        public PortfolioHedgerTradeReaderJob(ISubscriber<IReadOnlyList<TradeMessage>> subscriber, 
            ITradeHandler tradeHandler)
        {
            _tradeHandler = tradeHandler;
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
                    elem.Price,
                    elem.Volume,
                    elem.OppositeVolume,
                    elem.Timestamp,
                    TradeMessage.TopicName,
                    elem.FeeAsset,
                    elem.FeeVolume)
                {
                    Comment = elem.Comment,
                    User = elem.User
                });
            }
            await _tradeHandler.HandleTradesAsync(localTrades);
        }

        public void Start()
        {
        }
    }
}
