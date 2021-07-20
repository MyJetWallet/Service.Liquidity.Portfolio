using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Service.IndexPrices.Client;
using Service.Liquidity.Engine.Domain.Models.NoSql;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioManager
    {
        private readonly ILogger<AssetPortfolioManager> _logger;
        private readonly IMyNoSqlServerDataReader<LpWalletNoSql> _noSqlDataReader;
        private readonly IIndexPricesClient _indexPricesClient;

        public AssetPortfolio _portfolio = new AssetPortfolio();
        private List<AssetBalance> _assetBalances = new List<AssetBalance>();
        private readonly object _locker = new object();
        public const string UsdAsset = "USD";
        private bool IsInit = false;

        public AssetPortfolioManager(ILogger<AssetPortfolioManager> logger,
            IMyNoSqlServerDataReader<LpWalletNoSql> noSqlDataReader,
            IIndexPricesClient indexPricesClient)
        {
            _logger = logger;
            _noSqlDataReader = noSqlDataReader;
            _indexPricesClient = indexPricesClient;
        }

        public async Task UpdatePortfolio()
        {
            var assetBalanceCopy = new List<AssetBalance>();
            lock (_locker)
            {
                _assetBalances.ForEach(elem =>
                {
                    assetBalanceCopy.Add(elem.Copy());
                });
            }
            var internalWallets = _noSqlDataReader.Get().Select(elem => elem.Wallet.Name).ToList();

            _portfolio.BalanceByWallet = GetBalanceByWallet(assetBalanceCopy, internalWallets);
            _portfolio.BalanceByAsset = GetBalanceByAsset(assetBalanceCopy, internalWallets);
        }

        public async Task ReloadBalance(IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> myNoSqlServerDataWriter)
        {
            var nosqlBalance = (await myNoSqlServerDataWriter.GetAsync()).FirstOrDefault();
            _portfolio = nosqlBalance?.Balance ?? new AssetPortfolio();
            ReloadAssetBalances(_portfolio);

            IsInit = true;
        }

        private void ReloadAssetBalances(AssetPortfolio assetPortfolio)
        {
            lock (_locker)
            {
                _assetBalances = new List<AssetBalance>();
                assetPortfolio.BalanceByAsset.ForEach(balanceByAsset =>
                {
                    balanceByAsset.WalletBalances.ForEach(balanceByAssetAndWallet =>
                    {
                        _assetBalances.Add(new AssetBalance()
                        {
                            Asset = balanceByAsset.Asset,
                            BrokerId = balanceByAssetAndWallet.BrokerId,
                            Volume = balanceByAssetAndWallet.NetVolume,
                            WalletName = balanceByAssetAndWallet.WalletName
                        });
                    });
                });
            }
        }
        
        public Dictionary<string, decimal> UpdateBalance(IEnumerable<AssetBalanceDifference> differenceBalances)
        {
            if (!IsInit)
            {
                throw new Exception($"{nameof(AssetPortfolioManager)} is not init!!!");
            }
            
            lock (_locker)
            {
                var pnlByAsset = new Dictionary<string, decimal>();
                foreach (var difference in differenceBalances)
                {
                    var balance = GetBalanceEntity(difference.BrokerId, difference.WalletName, difference.Asset);
                    var usdBalance = GetBalanceEntity(difference.BrokerId, difference.WalletName, UsdAsset);
                    
                    if ((balance.Volume > 0 && difference.Volume > 0) || (balance.Volume < 0 && difference.Volume < 0))
                    {
                        balance.Volume += difference.Volume;
                        balance.OpenPrice =
                            (balance.Volume * balance.OpenPrice + difference.Volume * difference.CurrentPriceInUsd) /
                            (difference.Volume + balance.Volume);
                        continue;
                    }

                    var originalVolume = balance.Volume;
                    var decreaseVolumeAbs = Math.Min(Math.Abs(balance.Volume), Math.Abs(difference.Volume));
                    var decreaseVolume = balance.Volume > 0 ? decreaseVolumeAbs : -decreaseVolumeAbs;

                    if (decreaseVolume > 0)
                    {
                        var releasePnl = (difference.CurrentPriceInUsd - balance.OpenPrice) / decreaseVolume;
                        usdBalance.Volume += releasePnl;
                        balance.Volume = 0;

                        if (!pnlByAsset.TryGetValue(balance.Asset, out var pnl))
                        {
                            pnl = 0;
                        }
                        pnl += releasePnl;
                        pnlByAsset[balance.Asset] = pnl;
                        continue;
                    }

                    if (decreaseVolumeAbs < Math.Abs(difference.Volume))
                    {
                        balance.Volume = difference.Volume + originalVolume;
                        balance.OpenPrice = difference.CurrentPriceInUsd; 
                        continue;
                    }

                    balance.Volume += difference.Volume;
                }

                return pnlByAsset;
            }
        }

        private AssetBalance GetBalanceEntity(string brokerId, string walletName, string asset)
        {
            var balance = _assetBalances.FirstOrDefault(elem =>
                elem.WalletName == walletName && elem.Asset == asset);
            if (balance == null)
            {
                balance = new AssetBalance()
                {
                    Asset = asset,
                    BrokerId = brokerId,
                    Volume = 0,
                    WalletName = walletName
                };
                _assetBalances.Add(balance);
            }

            return balance;
        }

        public List<AssetBalance> GetBalancesSnapshot()
        {
            if (!IsInit)
            {
                throw new Exception($"{nameof(AssetPortfolioManager)} is not init!!!");
            }
            
            using var a = MyTelemetry.StartActivity("GetBalancesSnapshot");
            
            lock(_locker)
            {
                var newList = _assetBalances.Select(elem => elem.Copy()).ToList();
                return newList;
            }
        }

        private List<NetBalanceByAsset> GetBalanceByAsset(List<AssetBalance> balancesSnapshot,
            ICollection<string> internalWallets)
        {
            using var a = MyTelemetry.StartActivity("GetBalanceByAsset");
            
            var balanceByAssetCollection = new List<NetBalanceByAsset>();
            
            var assets = balancesSnapshot
                .Select(elem => elem.Asset)
                .Distinct();
            
            var wallets = balancesSnapshot
                .Select(elem => elem.WalletName)
                .Distinct()
                .ToList();

            foreach (var asset in assets)
            {
                var balanceByAsset = new NetBalanceByAsset {Asset = asset, WalletBalances =  new List<NetBalanceByWallet>()};

                foreach (var wallet in wallets)
                {
                    var balanceByWallet = new NetBalanceByWallet()
                    {
                        BrokerId = balancesSnapshot.First(elem => elem.WalletName == wallet).BrokerId,
                        WalletName = wallet
                    };
                    var sumByWallet = balancesSnapshot
                        .Where(elem => elem.WalletName == wallet && elem.Asset == asset)
                        .Sum(elem => elem.Volume);
                    
                    if (sumByWallet != 0)
                    {
                        balanceByWallet.NetVolume = sumByWallet;
                        balanceByWallet.NetUsdVolume = balancesSnapshot
                            .Where(elem => elem.WalletName == wallet && elem.Asset == asset)
                            .Sum(GetUsdProjectionByBalance);
                    }

                    balanceByWallet.IsInternal = internalWallets.Contains(wallet);
                    balanceByAsset.WalletBalances.Add(balanceByWallet);
                }

                var netVolumeByInternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetVolume);
                var netVolumeByExternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => !internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetVolume);
                balanceByAsset.NetVolume = netVolumeByInternalWallets+netVolumeByExternalWallets;

                var netUsdVolumeByInternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetUsdVolume);
                var netUsdVolumeByExternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => !internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetUsdVolume);
                balanceByAsset.NetUsdVolume = netUsdVolumeByInternalWallets+netUsdVolumeByExternalWallets;
                
                balanceByAssetCollection.Add(balanceByAsset);
            }

            return balanceByAssetCollection;
        }

        private List<NetBalanceByWallet> GetBalanceByWallet(List<AssetBalance> balancesSnapshot,
            ICollection<string> internalWallets)
        {
            using var a = MyTelemetry.StartActivity("GetBalanceByWallet");
            
            var balanceByWallet = balancesSnapshot
                .Select(elem => elem.WalletName)
                .Distinct()
                .Select(walletName => new NetBalanceByWallet() {WalletName = walletName})
                .ToList();

            foreach (var balanceByWalletElem in balanceByWallet)
            {
                var balanceByWalletCollection = balancesSnapshot
                    .Where(assetBalance => balanceByWalletElem.WalletName == assetBalance.WalletName)
                    .ToList();

                balanceByWalletElem.BrokerId = balanceByWalletCollection.First().BrokerId;
                balanceByWalletElem.NetUsdVolume = balanceByWalletCollection
                    .Sum(GetUsdProjectionByBalance);
            }

            balanceByWallet.ForEach(elem =>
            {
                elem.IsInternal = internalWallets.Contains(elem.WalletName);
            });
            
            return balanceByWallet;
        }
        
        private decimal GetUsdProjectionByBalance(AssetBalance balance)
        {
            var (indexPrice, usdVolume) =
                _indexPricesClient.GetIndexPriceByAssetVolumeAsync(balance.Asset, balance.Volume);
            
            return usdVolume;
        }
    }
}
