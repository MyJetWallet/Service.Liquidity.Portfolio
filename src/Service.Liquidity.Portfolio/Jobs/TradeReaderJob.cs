using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using ME.Contracts.OutgoingMessages;
using Service.BalanceHistory.ServiceBus;
using Service.Liquidity.Engine.Grpc;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class TradeReaderJob: IStartable
    {
        private readonly ILpWalletManagerGrpc _walletManager;
        private readonly IPortfolioStorage _portfolioStorage;
        
        public TradeReaderJob(ISubscriber<IReadOnlyList<WalletTradeMessage>> subscriber, ILpWalletManagerGrpc walletManager, IPortfolioStorage portfolioStorage)
        {
            _walletManager = walletManager;
            _portfolioStorage = portfolioStorage;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<WalletTradeMessage> trades)
        {
            var wallets = (await _walletManager.GetAllAsync()).Data.List.Select(elem => elem.WalletId);
            var ourTrades = trades.Where(trade => wallets.Contains(trade.WalletId)).ToList();
            var listForSave = ourTrades.Select(elem => new Trade(elem.Trade, elem.WalletId)).ToList();
            await _portfolioStorage.SaveAsync(listForSave);
        }

        public void Start()
        {
        }
    }
}
