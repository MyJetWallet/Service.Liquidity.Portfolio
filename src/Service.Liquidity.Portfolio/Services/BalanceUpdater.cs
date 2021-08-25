using System;
using System.Linq;
using MyJetWallet.Sdk.Service;
using Service.IndexPrices.Client;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class BalanceUpdater
    {
        private readonly IIndexPricesClient _indexPricesClient;

        public BalanceUpdater(IIndexPricesClient indexPricesClient)
        {
            _indexPricesClient = indexPricesClient;
        }

        public void UpdateBalance(AssetPortfolio portfolio, AssetBalanceDifference difference, bool forceSet)
        {
            var balanceByAsset = GetBalanceByAsset(portfolio, difference.Asset);
            var lastVolume = balanceByAsset.Volume;
            UpdateBalanceByAssetAndWallet(balanceByAsset, difference, forceSet);
            UpdateBalanceByAsset(balanceByAsset, portfolio, lastVolume); 
            UpdateBalanceByWallet(portfolio);
        }
        
        private void UpdateBalanceByWallet(AssetPortfolio portfolio)
        {
            using var a = MyTelemetry.StartActivity("UpdateBalanceByWallet");

            var balanceByWallet = portfolio.BalanceByAsset
                .SelectMany(elem => elem.WalletBalances)
                .GroupBy(x => new {x.WalletName, x.BrokerId, x.IsInternal})
                .Select(group => new BalanceByWallet()
                {
                    BrokerId = group.Key.BrokerId,
                    IsInternal = group.Key.IsInternal,
                    WalletName = group.Key.WalletName,
                    UsdVolume = group.Sum(e => e.UsdVolume)
                }).ToList();
            portfolio.BalanceByWallet = balanceByWallet;
        }
        
        private void UpdateBalanceByAsset(BalanceByAsset balanceByAsset, AssetPortfolio portfolio, decimal lastVolume)
        {
            var newVolume = balanceByAsset.WalletBalances.Sum(e => e.Volume);
            
            balanceByAsset.Volume = newVolume;
            balanceByAsset.UsdVolume = balanceByAsset.WalletBalances.Sum(e => e.UsdVolume);
            balanceByAsset.OpenPriceAvg = GetOpenPriceAvg(balanceByAsset.Asset, newVolume, lastVolume, balanceByAsset.OpenPriceAvg);
        }
        
        private decimal GetOpenPriceAvg(string asset, decimal volume, decimal lastVolume, decimal lastOpenPriceAvg)
        {
            if (string.IsNullOrWhiteSpace(asset))
                return 0;
            
            var indexPrice = _indexPricesClient.GetIndexPriceByAssetAsync(asset);
            
            // открытие позиции
            if (lastVolume == 0)
                return indexPrice.UsdPrice;
            
            // закрытие позиции
            if (volume == 0)
                return 0;
           
            if ((lastVolume > 0 && volume > 0) ||
                (lastVolume < 0 && volume < 0))
            {
                // увеличение позиции
                if (Math.Abs(volume) > Math.Abs(lastVolume))
                {
                    var diff = Math.Abs(volume - lastVolume);
                    var avgPrice = (lastOpenPriceAvg * Math.Abs(lastVolume) + indexPrice.UsdPrice * diff) /
                                   Math.Abs(volume);
                    return avgPrice;
                }
                // уменьшение позиции
                return lastOpenPriceAvg;
            }
            // закрытие позиции в ноль и открытие новой позиции (переворот)
            return indexPrice.UsdPrice;
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
