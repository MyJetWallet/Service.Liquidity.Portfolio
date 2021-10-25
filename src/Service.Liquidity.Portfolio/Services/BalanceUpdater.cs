using System;
using System.Collections.Generic;
using System.Linq;
using MyJetWallet.Sdk.Service;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class BalanceUpdater
    {
        private readonly IIndexPricesClient _indexPricesClient;
        private readonly LpWalletStorage _lpWalletStorage;
        
        public const string UsdAsset = "USD"; // todo: get from config ASSET AND BROKER
        public const string Broker = "jetwallet"; // todo: get from config ASSET AND BROKER
        public const string PlWalletName = "PL Balance";// todo: get from config

        public BalanceUpdater(IIndexPricesClient indexPricesClient,
            LpWalletStorage lpWalletStorage)
        {
            _indexPricesClient = indexPricesClient;
            _lpWalletStorage = lpWalletStorage;
        }

        public void UpdateBalance(AssetPortfolio portfolio, AssetBalanceDifference difference, bool forceSet)
        {
            var balanceByAsset = GetBalanceByAsset(portfolio, difference.Asset);
            var lastVolume = balanceByAsset.Volume;
            UpdateBalanceByAssetAndWallet(balanceByAsset, difference, forceSet);
            UpdateBalanceByAsset(balanceByAsset, lastVolume, difference.CurrentPriceInUsd); 
            UpdateBalanceByWallet(portfolio);
        }
        
        public void UpdateBalance(AssetPortfolio portfolio)
        {
            var indexPrices = _indexPricesClient.GetIndexPricesAsync();
            UpdateBalanceByAssetAndWallet(portfolio, indexPrices);
            UpdateBalanceByAsset(portfolio, indexPrices); 
            UpdateBalanceByWallet(portfolio);
        }

        private void UpdateBalanceByAsset(AssetPortfolio portfolio, List<IndexPrice> indexPrices)
        {
            foreach (var balanceByAsset in portfolio.BalanceByAsset)
            {
                var indexPrice = indexPrices.FirstOrDefault(e => e.Asset == balanceByAsset.Asset) ?? new IndexPrice()
                {
                    UsdPrice = 0
                };
                var unrPnl = balanceByAsset.Volume * (indexPrice.UsdPrice - balanceByAsset.OpenPriceAvg);
                balanceByAsset.UnrealisedPnl = unrPnl;
                balanceByAsset.UsdVolume = balanceByAsset.WalletBalances.Sum(e => e.UsdVolume);
            }
        }

        private void UpdateBalanceByAssetAndWallet(AssetPortfolio portfolio, List<IndexPrice> indexPrices)
        {
            foreach (var balanceByAsset in portfolio.BalanceByAsset)
            {
                var indexPrice = indexPrices.FirstOrDefault(e => e.Asset == balanceByAsset.Asset) ?? new IndexPrice()
                {
                    UsdPrice = 0
                };
                foreach (var balanceByWallet in balanceByAsset.WalletBalances)
                {
                    balanceByWallet.UsdVolume = balanceByWallet.Volume * indexPrice.UsdPrice;
                }
            }
        }

        private void UpdateBalanceByWallet(AssetPortfolio portfolio)
        {
            using var a = MyTelemetry.StartActivity("UpdateBalanceByWallet");

            var internalWallets = _lpWalletStorage.GetWallets();

            var balanceByWallet = portfolio.BalanceByAsset
                .SelectMany(elem => elem.WalletBalances)
                .GroupBy(x => new {x.WalletName, x.BrokerId, x.IsInternal})
                .Select(group => new BalanceByWallet()
                {
                    BrokerId = group.Key.BrokerId,
                    IsInternal = (internalWallets.Select(e=> e.Name).Contains(group.Key.WalletName) || group.Key.WalletName == PlWalletName),
                    WalletName = group.Key.WalletName,
                    UsdVolume = group.Sum(e => e.UsdVolume),
                    Volume = group.Sum(e => e.Volume)
                }).ToList();
            portfolio.BalanceByWallet = balanceByWallet;
            
            foreach (var balanceByAsset in portfolio.BalanceByAsset)
            {
                foreach (var walletBalance in balanceByAsset.WalletBalances)
                {
                    var walletType =
                        portfolio.BalanceByWallet.FirstOrDefault(e => e.WalletName == walletBalance.WalletName);
                    walletBalance.IsInternal = walletType?.IsInternal ?? false;
                }
            }
        }

        private static void UpdateBalanceByAsset(BalanceByAsset balanceByAsset, decimal lastVolume, decimal currentPrice)
        {
            var newVolume = balanceByAsset.WalletBalances.Sum(e => e.Volume);
            
            balanceByAsset.Volume = newVolume;
            balanceByAsset.UsdVolume = balanceByAsset.WalletBalances.Sum(e => e.UsdVolume);
            balanceByAsset.OpenPriceAvg = GetOpenPriceAvg(balanceByAsset.Asset, newVolume, lastVolume, balanceByAsset.OpenPriceAvg, currentPrice);
            Console.WriteLine($"Set open price {balanceByAsset.OpenPriceAvg}; asset {balanceByAsset.Asset}");
        }

        private static decimal GetOpenPriceAvg(string asset, decimal newVolume, decimal lastVolume, decimal lastOpenPriceAvg, decimal currentPrice)
        {
            if (string.IsNullOrWhiteSpace(asset))
                return 0;
            
            // закрытие позиции
            if (newVolume == 0)
                return 0;
            
            // открытие позиции
            if (lastVolume == 0)
                return currentPrice;

            if ((lastVolume > 0 && newVolume > 0) ||
                (lastVolume < 0 && newVolume < 0))
            {
                // увеличение позиции
                if (Math.Abs(newVolume) > Math.Abs(lastVolume))
                {
                    var diff = Math.Abs(newVolume - lastVolume);
                    var avgPrice = (lastOpenPriceAvg * Math.Abs(lastVolume) + currentPrice * diff) /
                                   Math.Abs(newVolume);
                    return avgPrice;
                }
                // уменьшение позиции
                return lastOpenPriceAvg;
            }
            // закрытие позиции в ноль и открытие новой позиции (переворот)
            return currentPrice;
        }

        private void SetBalance(string asset, BalanceByWallet balanceByWallet, decimal volume)
        {
            var (indexPrice, usdVolume) = _indexPricesClient.GetIndexPriceByAssetVolumeAsync(asset, volume);
            
            balanceByWallet.Volume = volume;
            balanceByWallet.UsdVolume = usdVolume;
        }

        public BalanceByAsset GetBalanceByAsset(AssetPortfolio portfolio, string asset)
        {
            var balance = portfolio.BalanceByAsset.FirstOrDefault(elem => elem.Asset == asset);
            if (balance == null)
            {
                balance = new BalanceByAsset()
                {
                    Asset = asset
                };
                portfolio.BalanceByAsset.Add(balance);
            }
            return balance;
        }
        
        private void UpdateBalanceByAssetAndWallet(BalanceByAsset balanceByAsset, AssetBalanceDifference difference, bool forceSet)
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
                SetBalance(balanceByAsset.Asset, balanceByWallet, 0m);
            }

            if ((balanceByWallet.Volume >= 0 && difference.Volume > 0) || (balanceByWallet.Volume <= 0 && difference.Volume < 0))
            {
                var volume = balanceByWallet.Volume + difference.Volume;
                SetBalance(balanceByAsset.Asset, balanceByWallet, volume);
                return;
            }
            var originalVolume = balanceByWallet.Volume;
            var decreaseVolumeAbs = Math.Min(Math.Abs(balanceByWallet.Volume), Math.Abs(difference.Volume));
            if (decreaseVolumeAbs > 0)
            {
                decimal volume;
                if (balanceByWallet.Volume > 0)
                {
                    volume = balanceByWallet.Volume - decreaseVolumeAbs;
                }
                else
                {   
                    volume = balanceByWallet.Volume + decreaseVolumeAbs;
                }
                SetBalance(balanceByAsset.Asset, balanceByWallet, volume);
            }
            if (decreaseVolumeAbs < Math.Abs(difference.Volume))
            {
                var volume = difference.Volume + originalVolume;
                SetBalance(balanceByAsset.Asset, balanceByWallet, volume);
            }
        }

        public void SetReleasedPnl(AssetPortfolio portfolio, decimal releasedPnl)
        {
            var (balanceByAsset, balanceByWallet) = GetBalanceByPnlWallet(portfolio);
            var lastAssetVolume = portfolio.BalanceByAsset.Sum(e => e.Volume);
            var volume = balanceByWallet.Volume - releasedPnl;
            SetBalance(UsdAsset, balanceByWallet, volume);
            
            UpdateBalanceByAsset(balanceByAsset, lastAssetVolume, 1); 
            UpdateBalanceByWallet(portfolio);
        }
        
        private (BalanceByAsset, BalanceByWallet) GetBalanceByPnlWallet(AssetPortfolio portfolio)
        {
            var balanceByAsset = portfolio.BalanceByAsset.FirstOrDefault(elem => elem.Asset == UsdAsset);
            if (balanceByAsset == null)
            {
                balanceByAsset = new BalanceByAsset()
                {
                    Asset = UsdAsset
                };
                portfolio.BalanceByAsset.Add(balanceByAsset);
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
            return (balanceByAsset, balanceByWallet);
        }
    }
}
