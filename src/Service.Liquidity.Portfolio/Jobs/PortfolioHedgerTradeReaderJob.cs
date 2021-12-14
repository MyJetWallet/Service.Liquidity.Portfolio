using System.Collections.Generic;
using System.Threading;
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
        private static SemaphoreSlim _semaphore;
        private readonly ITradeHandler _tradeHandler;

        public PortfolioHedgerTradeReaderJob(ISubscriber<IReadOnlyList<TradeMessage>> subscriber, 
            ITradeHandler tradeHandler)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _tradeHandler = tradeHandler;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<TradeMessage> trades)
        {
            await _semaphore.WaitAsync();
            try
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
                        elem.Source,
                        elem.FeeAsset,
                        elem.FeeVolume) {Comment = elem.Comment, User = elem.User});
                }
                await _tradeHandler.HandleTradesAsync(localTrades);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Start()
        {
        }
    }
}
