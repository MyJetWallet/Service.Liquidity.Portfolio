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

        public AssetPortfolio Portfolio = new AssetPortfolio();
        private readonly object _locker = new object();
        public const string UsdAsset = "USD"; // todo: get from config ASSET AND BROKER
        public const string Broker = "jetwallet"; // todo: get from config ASSET AND BROKER
        public const string PlWalletName = "PL Balance";// todo: get from config
        private bool _isInit = false;

        public BalanceHandler(ILogger<BalanceHandler> logger,
            IIndexPricesClient indexPricesClient,
            LpWalletStorage lpWalletStorage)
        {
            _logger = logger;
            _indexPricesClient = indexPricesClient;
            _lpWalletStorage = lpWalletStorage;
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
                    var balanceByAsset = GetBalanceByAsset(difference.Asset);
                    UpdateBalanceByWallet(balanceByAsset, difference, forceSet);
                }
                UpdateBalanceByAsset(); // OPEN PRICE
                UpdateBalanceByWallet();
            }
        }

        private void UpdateBalanceByWallet()
        {
            using var a = MyTelemetry.StartActivity("UpdateBalanceByWallet");

            var balanceByWallet = Portfolio.BalanceByAsset
                .SelectMany(elem => elem.WalletBalances)
                .GroupBy(x => new {x.WalletName, x.BrokerId, x.IsInternal})
                .Select(group => new BalanceByWallet()
                {
                    BrokerId = group.Key.BrokerId,
                    IsInternal = group.Key.IsInternal,
                    WalletName = group.Key.WalletName,
                    UsdVolume = group.Sum(e => e.UsdVolume)
                }).ToList();
            Portfolio.BalanceByWallet = balanceByWallet;
        }

        private void UpdateBalanceByAsset()
        {
            foreach (var balanceByAsset in Portfolio.BalanceByAsset)
            {
                balanceByAsset.LastVolume = balanceByAsset.Volume;
                balanceByAsset.Volume = balanceByAsset.WalletBalances.Sum(e => e.Volume);
                balanceByAsset.UsdVolume = balanceByAsset.WalletBalances.Sum(e => e.UsdVolume);
                balanceByAsset.OpenPriceAvg = GetOpenPriceAvg(balanceByAsset);
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
                var usdBalance = GetBalanceByPnlWallet();
                usdBalance.Volume -= releasedPnl;
                return Math.Round(releasedPnl, 2);
            }
        }

        private BalanceByWallet GetBalanceByPnlWallet()
        {
            var balanceByAsset = Portfolio.BalanceByAsset.FirstOrDefault(elem => elem.Asset == UsdAsset);
            if (balanceByAsset == null)
            {
                balanceByAsset = new BalanceByAsset()
                {
                    Asset = UsdAsset
                };
                Portfolio.BalanceByAsset.Add(balanceByAsset);
            }

            var balanceByWallet = balanceByAsset.WalletBalances.FirstOrDefault(e => e.WalletName == PlWalletName);

            if (balanceByWallet == null)
            {
                balanceByWallet = new BalanceByWallet()
                {
                    WalletName = PlWalletName,
                    BrokerId = Broker,
                    Volume = 0,
                    UsdVolume = 0,
                    IsInternal = true
                };
                balanceByAsset.WalletBalances.Add(balanceByWallet);
            }
            return balanceByWallet;
        }

        public BalanceByAsset GetBalanceByAsset(string asset)
        {
            var balance = Portfolio.BalanceByAsset.FirstOrDefault(elem => elem.Asset == asset);
            if (balance == null)
            {
                balance = new BalanceByAsset()
                {
                    Asset = asset
                };
                Portfolio.BalanceByAsset.Add(balance);
            }
            return balance;
        }


        private decimal GetOpenPriceAvg(BalanceByAsset balanceByAsset)
        {
            var asset = balanceByAsset.Asset;
            if (string.IsNullOrWhiteSpace(asset))
                return 0;
            
            var indexPrice = _indexPricesClient.GetIndexPriceByAssetAsync(asset);
            
            // открытие позиции
            if (balanceByAsset.LastVolume == 0)
                return indexPrice.UsdPrice;
            
            // закрытие позиции
            if (balanceByAsset.Volume == 0)
                return 0;
           
            if ((balanceByAsset.LastVolume > 0 && balanceByAsset.Volume > 0) ||
                (balanceByAsset.LastVolume < 0 && balanceByAsset.Volume < 0))
            {
                // увеличение позиции
                if (Math.Abs(balanceByAsset.Volume) > Math.Abs(balanceByAsset.LastVolume))
                {
                    var diff = Math.Abs(balanceByAsset.Volume - balanceByAsset.LastVolume);
                    var avgPrice = (balanceByAsset.OpenPriceAvg * Math.Abs(balanceByAsset.LastVolume) + indexPrice.UsdPrice * diff) /
                                   Math.Abs(balanceByAsset.Volume);
                    return avgPrice;
                }
                // уменьшение позиции
                return balanceByAsset.OpenPriceAvg;
            }
            // закрытие позиции в ноль и открытие новой позиции (переворот)
            return indexPrice.UsdPrice;
        }
        private void UpdateBalanceByWallet(BalanceByAsset balanceByAsset, AssetBalanceDifference difference, bool forceSet = false)
        {
            var balanceByWallet =
                balanceByAsset.WalletBalances.FirstOrDefault(e => e.WalletName == difference.WalletName);

            if (balanceByWallet == null)
            {
                balanceByWallet = new BalanceByWallet()
                {
                    WalletName = difference.WalletName,
                    BrokerId = difference.BrokerId,
                    Volume = 0,
                    UsdVolume = 0
                };
                balanceByAsset.WalletBalances.Add(balanceByWallet);
            }
            
            // for SetBalance
            if (forceSet)
            {
                balanceByWallet.Volume = 0m;
            }

            if ((balanceByWallet.Volume >= 0 && difference.Volume > 0) || (balanceByWallet.Volume <= 0 && difference.Volume < 0))
            {
                balanceByWallet.Volume += difference.Volume;
                return;
            }
            var originalVolume = balanceByWallet.Volume;
            var decreaseVolumeAbs = Math.Min(Math.Abs(balanceByWallet.Volume), Math.Abs(difference.Volume));
            if (decreaseVolumeAbs > 0)
            {
                if (balanceByWallet.Volume > 0)
                    balanceByWallet.Volume -= decreaseVolumeAbs;
                else
                    balanceByWallet.Volume += decreaseVolumeAbs;
            }
            if (decreaseVolumeAbs < Math.Abs(difference.Volume))
            {
                balanceByWallet.Volume = difference.Volume + originalVolume;
            }
        }
    }
}
