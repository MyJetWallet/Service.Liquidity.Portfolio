using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class BalanceHandler
    {
        private readonly ILogger<BalanceHandler> _logger;
        private readonly LpWalletStorage _lpWalletStorage;
        private readonly IIndexPricesClient _indexPricesClient;
        private readonly BalanceUpdater _balanceUpdater;

        public AssetPortfolio Portfolio = new AssetPortfolio();
        private readonly object _locker = new object();

        private bool _isInit = false;

        public BalanceHandler(ILogger<BalanceHandler> logger,
            IIndexPricesClient indexPricesClient,
            LpWalletStorage lpWalletStorage,
            BalanceUpdater balanceUpdater)
        {
            _logger = logger;
            _indexPricesClient = indexPricesClient;
            _lpWalletStorage = lpWalletStorage;
            _balanceUpdater = balanceUpdater;
        }
        
        public AssetPortfolio GetPortfolioSnapshot()
        {
            if (!_isInit)
            {
                throw new Exception($"{nameof(BalanceHandler)} is not init!!!");
            }
            using var a = MyTelemetry.StartActivity("GetPortfolioSnapshot");

            var indexPrices = _indexPricesClient.GetIndexPricesAsync();
            
            lock(_locker)
            {
                var portfolioCopy = new AssetPortfolio
                {
                    BalanceByAsset = Portfolio.BalanceByAsset.Select(e => e.GetCopy()).ToList(),
                    BalanceByWallet = Portfolio.BalanceByWallet.Select(e => e.GetCopy()).ToList()
                };

                SetNetUsd(portfolioCopy, indexPrices);
                SetUnrPnl(portfolioCopy, indexPrices);
                return portfolioCopy;
            }
        }

        private void SetNetUsd(AssetPortfolio portfolio, List<IndexPrice> indexPrices)
        {
            foreach (var balanceByAsset in portfolio.BalanceByAsset)
            {
                var indexPrice = indexPrices.FirstOrDefault(e => e.Asset == balanceByAsset.Asset);

                if (indexPrice != null)
                {
                    var netUsd = balanceByAsset.Volume * indexPrice.UsdPrice;
                    balanceByAsset.UsdVolume = netUsd;
                }
                else
                {
                    _logger.LogError("Cannot found index price for : {symbol}", balanceByAsset.Asset);
                    balanceByAsset.UsdVolume = 0;
                }
            }
        }

        private void SetUnrPnl(AssetPortfolio portfolio, List<IndexPrice> indexPrices)
        {
            foreach (var balanceByAsset in portfolio.BalanceByAsset)
            {
                var indexPrice = indexPrices.FirstOrDefault(e => e.Asset == balanceByAsset.Asset);

                if (indexPrice != null)
                {
                    var unrPnl = balanceByAsset.Volume * (indexPrice.UsdPrice - balanceByAsset.OpenPriceAvg);
                    balanceByAsset.UnrealisedPnl = unrPnl;
                }
                else
                {
                    _logger.LogError("Cannot found index price for : {symbol}", balanceByAsset.Asset);
                    balanceByAsset.UnrealisedPnl = 0;
                }
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
