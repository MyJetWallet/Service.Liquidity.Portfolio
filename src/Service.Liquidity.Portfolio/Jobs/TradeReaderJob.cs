using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Service.BalanceHistory.ServiceBus;
using Service.Liquidity.Engine.Grpc;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class TradeReaderJob: IStartable
    {
        private readonly ILpWalletManagerGrpc _walletManager;
        
        public TradeReaderJob(ISubscriber<IReadOnlyList<WalletTradeMessage>> subscriber, ILpWalletManagerGrpc walletManager)
        {
            _walletManager = walletManager;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<WalletTradeMessage> trades)
        {
            var wallets = (await _walletManager.GetAllAsync()).Data.List.Select(elem => elem.WalletId);
            var ourTrades = trades.Where(trade => wallets.Contains(trade.WalletId)).ToList();
            var x = trades;
            var y = x;
        }

        public void Start()
        {
        }
    }
}
