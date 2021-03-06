using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain.Orders;
using Service.Liquidity.Converter.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.PortfolioHedger.Domain.Models;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class ConvertorSwapsReaderJob : IStartable
    {
        private readonly ITradeHandler _tradeHandler;
        private readonly LpWalletStorage _lpWalletStorage;

        public ConvertorSwapsReaderJob(ISubscriber<IReadOnlyList<SwapMessage>> subscriber, 
            ITradeHandler tradeHandler,
            LpWalletStorage lpWalletStorage)
        {
            _tradeHandler = tradeHandler;
            _lpWalletStorage = lpWalletStorage;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<SwapMessage> swaps)
        {
            var localTrades = new List<AssetPortfolioTrade>();
            var internalWallets = _lpWalletStorage.GetWallets().Select(e=>e.WalletId).ToList();

            foreach (var swap in swaps)
            {
                if (internalWallets.Contains(swap.WalletId1))
                {
                    localTrades.Add(new AssetPortfolioTrade(swap.Id,
                        swap.BrokerId,
                        swap.AssetId1 + "|" + swap.AssetId2,
                        swap.AssetId1,
                        swap.AssetId2,
                        _lpWalletStorage.GetWalletNameById(swap.WalletId1),
                        OrderSide.Sell,
                        Convert.ToDecimal(swap.Volume2) / Convert.ToDecimal(swap.Volume1),
                        -Convert.ToDecimal(swap.Volume1),
                        Convert.ToDecimal(swap.Volume2),
                        swap.Timestamp,
                        TradeMessage.TopicName,
                        swap.AssetId2,
                        0)
                    {
                        Comment = $"ClientId: {swap.AccountId2}"
                    });
                }
                if (internalWallets.Contains(swap.WalletId2))
                {
                    localTrades.Add(new AssetPortfolioTrade(swap.Id,
                        swap.BrokerId,
                        swap.AssetId2 + "|" + swap.AssetId1,
                        swap.AssetId2,
                        swap.AssetId1,
                        _lpWalletStorage.GetWalletNameById(swap.WalletId2),
                        OrderSide.Sell,
                        Convert.ToDecimal(swap.Volume1) / Convert.ToDecimal(swap.Volume2),
                        -Convert.ToDecimal(swap.Volume2),
                        Convert.ToDecimal(swap.Volume1),
                        swap.Timestamp,
                        TradeMessage.TopicName,
                        swap.AssetId1,
                        0)
                    {
                        Comment = $"ClientId: {swap.AccountId1}"
                    });
                }
            }
            await _tradeHandler.HandleTradesAsync(localTrades);
        }

        public void Start()
        {
        }
    }
}
