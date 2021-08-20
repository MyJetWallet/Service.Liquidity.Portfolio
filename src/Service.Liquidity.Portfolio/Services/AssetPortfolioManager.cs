using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.IndexPrices.Client;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioManager
    {
        private readonly ILogger<AssetPortfolioManager> _logger;
        private readonly LpWalletStorage _lpWalletStorage;
        private readonly IIndexPricesClient _indexPricesClient;
        private readonly AssetPortfolioMath _assetPortfolioMath;

        public AssetPortfolio Portfolio = new AssetPortfolio();
        private List<AssetBalance> _assetBalances = new List<AssetBalance>();
        private readonly object _locker = new object();
        public const string UsdAsset = "USD"; // todo: get from config ASSET AND BROKER
        public const string Broker = "jetwallet"; // todo: get from config ASSET AND BROKER
        public const string PlWalletName = "PL Balance";// todo: get from config
        private bool _isInit = false;

        public AssetPortfolioManager(ILogger<AssetPortfolioManager> logger,
            IIndexPricesClient indexPricesClient,
            AssetPortfolioMath assetPortfolioMath,
            LpWalletStorage lpWalletStorage)
        {
            _logger = logger;
            _indexPricesClient = indexPricesClient;
            _assetPortfolioMath = assetPortfolioMath;
            _lpWalletStorage = lpWalletStorage;
        }
        
        public AssetPortfolio GetPortfolioSnapshot(AssetPortfolio portfolio = null, List<AssetBalance> assetBalances = null)
        {
            if (!_isInit)
            {
                throw new Exception($"{nameof(AssetPortfolioManager)} is not init!!!");
            }
            using var a = MyTelemetry.StartActivity("GetPortfolioSnapshot");

            lock(_locker)
            {
                if (portfolio == null || assetBalances == null)
                {
                    portfolio = Portfolio;
                    assetBalances = _assetBalances;
                }
                UpdatePortfolio(portfolio, assetBalances);
                
                var portfolioCopy = new AssetPortfolio
                {
                    BalanceByAsset = portfolio.BalanceByAsset.Select(e => e.GetCopy()).ToList(),
                    BalanceByWallet = portfolio.BalanceByWallet.Select(e => e.GetCopy()).ToList()
                };
                return portfolioCopy;
            }
        }

        public void UpdatePortfolio(AssetPortfolio portfolio, List<AssetBalance> assetBalances)
        {
            var assetBalanceCopy = new List<AssetBalance>();
            assetBalances.ForEach(elem =>
            {
                assetBalanceCopy.Add(elem.Copy());
            });
            
            var internalWallets = _lpWalletStorage.GetWallets();

            portfolio.BalanceByAsset = GetBalanceByAsset(assetBalanceCopy, internalWallets);
            portfolio.BalanceByWallet = GetBalanceByWallet(portfolio.BalanceByAsset);
        }

        public async Task ReloadBalance(AssetPortfolio balances)
        {
            Portfolio = balances ?? new AssetPortfolio();
            ReloadAssetBalances(Portfolio);

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
                    var balance = GetBalanceEntity(_assetBalances, difference.BrokerId, difference.WalletName, difference.Asset);
                    _assetPortfolioMath.UpdateBalance(balance, difference, forceSet);
                }
            }
        }

        public decimal FixReleasedPnl(AssetPortfolio portfolio = null, List<AssetBalance> assetBalances = null)
        {
            lock (_locker)
            {
                if (portfolio == null || assetBalances == null)
                {
                    portfolio = Portfolio;
                    assetBalances = _assetBalances;
                }
                
                var snapshot = GetPortfolioSnapshot(portfolio, assetBalances);

                var netUsd = snapshot.BalanceByWallet.Sum(e => e.NetUsdVolume);
                var unrPnl = snapshot.BalanceByWallet.Sum(e => e.UnreleasedPnlUsd);

                var releasedPnl = netUsd - unrPnl;
                var usdBalance = GetReleasedPnlEntity(assetBalances);
                usdBalance.Volume -= releasedPnl;
                return Math.Round(releasedPnl, 2);
            }
        }

        private AssetBalance GetReleasedPnlEntity(List<AssetBalance> assetBalances)
        {
            var balance = assetBalances.FirstOrDefault(elem =>
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
                assetBalances.Add(balance);
            }
            return balance;
        }

        public AssetBalance GetBalanceEntity(List<AssetBalance> assetBalances, string brokerId, string walletName, string asset)
        {
            var balance = assetBalances.FirstOrDefault(elem =>
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
                assetBalances.Add(balance);
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
                    balanceByAsset.OpenPriceAvg = GetOpenPriceAvg(balanceByAsset);
                }
                else
                {
                    balanceByAsset.OpenPriceAvg = 0;
                }

                balanceByAssetCollection.Add(balanceByAsset);
            }

            return balanceByAssetCollection;
        }

        private decimal GetOpenPriceAvg(NetBalanceByAsset balanceByAsset)
        {
            if (balanceByAsset.WalletBalances.All(e => e.NetVolume > 0) ||
                balanceByAsset.WalletBalances.All(e => e.NetVolume < 0))
            {
                return balanceByAsset.WalletBalances
                    .Sum(elem => elem.OpenPrice * elem.NetVolume) / balanceByAsset.NetVolume;
            }
            
            return 0;
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
                    NetUsdVolume = group.Sum(e => e.NetUsdVolume),
                    UnreleasedPnlUsd = group.Sum(e => e.UnreleasedPnlUsd)
                }).ToList();
            
            return balanceByWallet;
        }
        
        private decimal GetUsdProjectionByBalance(AssetBalance balance)
        {
            if (balance.Volume == 0)
                return 0;
            
            var (indexPrice, usdVolume) =
                _indexPricesClient.GetIndexPriceByAssetVolumeAsync(balance.Asset, balance.Volume);
            
            return usdVolume;
        }
    }
}
