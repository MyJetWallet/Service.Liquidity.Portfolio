using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class BalanceHandler
    {
        private readonly ILogger<BalanceHandler> _logger;
        private readonly BalanceUpdater _balanceUpdater;

        public AssetPortfolio Portfolio = new AssetPortfolio();
        private readonly object _locker = new object();

        private bool _isInit = false;

        public BalanceHandler(ILogger<BalanceHandler> logger,
            BalanceUpdater balanceUpdater)
        {
            _logger = logger;
            _balanceUpdater = balanceUpdater;
        }
        
        public AssetPortfolio GetPortfolioSnapshot()
        {
            if (!_isInit)
            {
                throw new Exception($"{nameof(BalanceHandler)} is not init!!!");
            }
            using var a = MyTelemetry.StartActivity("GetPortfolioSnapshot");
            
            lock(_locker)
            {
                var portfolioCopy = new AssetPortfolio
                {
                    BalanceByAsset = Portfolio.BalanceByAsset.Select(e => e.GetCopy()).ToList(),
                    BalanceByWallet = Portfolio.BalanceByWallet.Select(e => e.GetCopy()).ToList()
                };
                _balanceUpdater.UpdateBalance(portfolioCopy);
                return portfolioCopy;
            }
        }

        public async Task ReloadBalance(AssetPortfolio balances)
        {
            lock (_locker)
            {
                Portfolio = balances ?? new AssetPortfolio();
            }
            _isInit = true;
        }
        
        public void UpdateBalance(IEnumerable<AssetBalanceDifference> differenceBalances, bool forceSet = false)
        {
            if (!_isInit)
            {
                throw new Exception($"{nameof(BalanceHandler)} is not init!!!");
            }
            lock (_locker)
            {
                foreach (var difference in differenceBalances)
                {
                    _balanceUpdater.UpdateBalance(Portfolio, difference, forceSet);
                }
            }
        }

        public decimal FixReleasedPnl()
        {
            lock (_locker)
            {
                var snapshot = GetPortfolioSnapshot();

                var netUsd = snapshot.BalanceByAsset.Sum(e => e.UsdVolume);
                var unrPnl = snapshot.BalanceByAsset.Sum(e => e.UnrealisedPnl);
                var releasedPnl = netUsd - unrPnl;
                
                _balanceUpdater.SetReleasedPnl(snapshot, releasedPnl);
                
                Portfolio = snapshot;
                
                return Math.Round(releasedPnl, 2);
            }
        }
    }
}
