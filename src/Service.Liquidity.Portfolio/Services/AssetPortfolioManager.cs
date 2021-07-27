using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public const string UsdAsset = "USD"; // todo: get from config ASSET AND BROKER
        private const string Broker = "jetwallet"; // todo: get from config ASSET AND BROKER
        public const string PlWalletName = "PL Balance";// todo: get from config
        private bool _isInit = false;

        public AssetPortfolioManager(ILogger<AssetPortfolioManager> logger,
            IMyNoSqlServerDataReader<LpWalletNoSql> noSqlDataReader,
            IIndexPricesClient indexPricesClient)
        {
            _logger = logger;
            _noSqlDataReader = noSqlDataReader;
            _indexPricesClient = indexPricesClient;
        }
        
        public AssetPortfolio GetPortfolioSnapshot()
        {
            if (!_isInit)
            {
                throw new Exception($"{nameof(AssetPortfolioManager)} is not init!!!");
            }
            using var a = MyTelemetry.StartActivity("GetPortfolioSnapshot");

            lock(_locker)
            {
                UpdatePortfolio();
                
                var portfolioCopy = new AssetPortfolio
                {
                    BalanceByAsset = _portfolio.BalanceByAsset.Select(e => e.GetCopy()).ToList(),
                    BalanceByWallet = _portfolio.BalanceByWallet.Select(e => e.GetCopy()).ToList()
                };
                return portfolioCopy;
            }
        }

        private void UpdatePortfolio()
        {
            var assetBalanceCopy = new List<AssetBalance>();
            _assetBalances.ForEach(elem =>
            {
                assetBalanceCopy.Add(elem.Copy());
            });
            
            var internalWallets = _noSqlDataReader.Get().Select(elem => elem.Wallet.Name).ToList();

            _portfolio.BalanceByAsset = GetBalanceByAsset(assetBalanceCopy, internalWallets);
            _portfolio.BalanceByWallet = GetBalanceByWallet(_portfolio.BalanceByAsset);
        }

        public async Task ReloadBalance(AssetPortfolio balances)
        {
            _portfolio = balances ?? new AssetPortfolio();
            ReloadAssetBalances(_portfolio);

            _isInit = true;
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
                            WalletName = balanceByAssetAndWallet.WalletName,
                            OpenPrice = balanceByAssetAndWallet.OpenPrice
                        });
                    });
                });
            }
        }
        
        public void UpdateBalance(IEnumerable<AssetBalanceDifference> differenceBalances, bool forceSet = false)
        {
            if (!_isInit)
            {
                throw new Exception($"{nameof(AssetPortfolioManager)} is not init!!!");
            }
            lock (_locker)
            {
                foreach (var difference in differenceBalances)
                {
                    var balance = GetBalanceEntity(difference.BrokerId, difference.WalletName, difference.Asset);
                    
                    // for SetBalance
                    if (forceSet)
                    {
                        balance.Volume = 0m;
                    }
                    if ((balance.Volume >= 0 && difference.Volume > 0) || (balance.Volume <= 0 && difference.Volume < 0))
                    {
                        balance.OpenPrice =
                            (balance.Volume * balance.OpenPrice + difference.Volume * difference.CurrentPriceInUsd) /
                            (difference.Volume + balance.Volume);
                        balance.Volume += difference.Volume;
                        continue;
                    }
                    var originalVolume = balance.Volume;
                    var decreaseVolumeAbs = Math.Min(Math.Abs(balance.Volume), Math.Abs(difference.Volume));
                    if (decreaseVolumeAbs > 0)
                    {
                        if (balance.Volume > 0)
                            balance.Volume -= decreaseVolumeAbs;
                        else
                            balance.Volume += decreaseVolumeAbs;
                        
                        
                        if (balance.Volume == 0)
                            balance.OpenPrice = 0;
                    }
                    if (decreaseVolumeAbs < Math.Abs(difference.Volume))
                    {
                        balance.Volume = difference.Volume + originalVolume;
                        balance.OpenPrice = difference.CurrentPriceInUsd; 
                    }
                }
            }
        }

        public decimal FixReleasedPnl()
        {
            lock (_locker)
            {
                var snapshot = GetPortfolioSnapshot();

                var netUsd = snapshot.BalanceByWallet.Sum(e => e.NetUsdVolume);
                var unrPnl = snapshot.BalanceByWallet.Sum(e => e.UnreleasedPnlUsd);

                var releasedPnl = netUsd - unrPnl;
                var usdBalance = GetReleasedPnlEntity();
                usdBalance.Volume -= releasedPnl;
                return releasedPnl;
            }
        }

        private AssetBalance GetReleasedPnlEntity()
        {
            var balance = _assetBalances.FirstOrDefault(elem =>
                elem.WalletName == PlWalletName && elem.Asset == UsdAsset);
            if (balance == null)
            {
                balance = new AssetBalance()
                {
                    Asset = UsdAsset,
                    BrokerId = Broker,
                    Volume = 0,
                    WalletName = PlWalletName,
                    OpenPrice = 1
                };
                _assetBalances.Add(balance);
            }
            return balance;
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
                    WalletName = walletName,
                    OpenPrice = 0
                };
                _assetBalances.Add(balance);
            }

            return balance;
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

                    var entity =
                        balancesSnapshot.FirstOrDefault(elem => elem.Asset == asset && elem.WalletName == wallet);

                    if (entity != null)
                    {
                        balanceByWallet.NetVolume = entity.Volume;
                        balanceByWallet.OpenPrice = entity.OpenPrice;
                        balanceByWallet.NetUsdVolume = GetUsdProjectionByBalance(entity);
                        balanceByWallet.UnreleasedPnlUsd = balanceByWallet.NetUsdVolume -
                                                           balanceByWallet.NetVolume * balanceByWallet.OpenPrice;
                    }
                    balanceByWallet.IsInternal = internalWallets.Contains(wallet);
                    
                    balanceByAsset.WalletBalances.Add(balanceByWallet);
                }
                
                balanceByAsset.NetVolume = balanceByAsset.WalletBalances
                    .Sum(elem => elem.NetVolume);
                
                balanceByAsset.NetUsdVolume = balanceByAsset.WalletBalances
                    .Sum(elem => elem.NetUsdVolume);
                
                balanceByAsset.UnrealisedPnl = balanceByAsset.WalletBalances
                    .Sum(elem => elem.UnreleasedPnlUsd);

                if (balanceByAsset.NetVolume != 0)
                {
                    balanceByAsset.OpenPriceAvg = balanceByAsset.WalletBalances
                        .Sum(elem => elem.OpenPrice * elem.NetVolume) / balanceByAsset.NetVolume;
                }
                else
                {
                    balanceByAsset.OpenPriceAvg = 0;
                }

                balanceByAssetCollection.Add(balanceByAsset);
            }

            return balanceByAssetCollection;
        }
        
        private List<NetBalanceByWallet> GetBalanceByWallet(List<NetBalanceByAsset> balancesByAssets)
        {
            using var a = MyTelemetry.StartActivity("GetBalanceByWallet");

            var balanceByWallet = balancesByAssets
                .SelectMany(elem => elem.WalletBalances)
                .GroupBy(x => new {x.WalletName, x.BrokerId, x.IsInternal})
                .Select(group => new NetBalanceByWallet()
                {
                    BrokerId = group.Key.BrokerId,
                    IsInternal = group.Key.IsInternal,
                    WalletName = group.Key.WalletName,
                    NetVolume = group.Sum(e => e.NetVolume),
                    NetUsdVolume = group.Sum(e => e.NetUsdVolume),
                    UnreleasedPnlUsd = group.Sum(e => e.UnreleasedPnlUsd)
                }).ToList();
            
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
