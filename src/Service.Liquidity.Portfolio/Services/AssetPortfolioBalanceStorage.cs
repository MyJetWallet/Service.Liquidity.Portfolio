using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Engine.Domain.Models.NoSql;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioBalanceStorage : IAssetPortfolioBalanceStorage, IStartable
    {
        private readonly ILogger<AssetPortfolioBalanceStorage> _logger;
        private readonly IMyNoSqlServerDataReader<LpWalletNoSql> _noSqlDataReader;
        private readonly IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> _settingsDataWriter;
        
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;
        
        private readonly IAssetPortfolioSettingsStorage _assetPortfolioSettingsStorage;
        
        private AssetPortfolio _portfolio = new AssetPortfolio();
        private List<AssetBalance> _assetBalances = new List<AssetBalance>();
        private readonly object _locker = new object();

        public AssetPortfolioBalanceStorage(ILogger<AssetPortfolioBalanceStorage> logger,
            IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> settingsDataWriter,
            IMyNoSqlServerDataReader<LpWalletNoSql> noSqlDataReader,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            IAssetPortfolioSettingsStorage assetPortfolioSettingsStorage)
        {
            _logger = logger;
            _settingsDataWriter = settingsDataWriter;
            _noSqlDataReader = noSqlDataReader;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _assetPortfolioSettingsStorage = assetPortfolioSettingsStorage;
        }

        public async Task SavePortfolioToNoSql()
        {
            await UpdatePortfolio();
            await _settingsDataWriter.InsertOrReplaceAsync(AssetPortfolioBalanceNoSql.Create(_portfolio));
            await ReloadBalance();
        }

        private async Task UpdatePortfolio()
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

        private async Task ReloadBalance()
        {
            var nosqlBalance = (await _settingsDataWriter.GetAsync()).FirstOrDefault();
            _portfolio = nosqlBalance?.Balance ?? new AssetPortfolio();
            
            ReloadAssetBalances(_portfolio);
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

        public void Start()
        {
            ReloadBalance().GetAwaiter().GetResult();
        }
        
        public void UpdateBalance(IEnumerable<AssetBalance> differenceBalances)
        {
            lock (_locker)
            {
                foreach (var difference in differenceBalances)
                {
                    var balance = _assetBalances.FirstOrDefault(elem =>
                        elem.WalletName == difference.WalletName && elem.Asset == difference.Asset);
                    if (balance == null)
                    {
                        balance = difference;
                        _assetBalances.Add(balance);
                    }
                    else
                    {
                        balance.Volume += difference.Volume;
                    }
                }
            }
        }
        
        public List<AssetBalance> GetBalancesSnapshot()
        {
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
                balanceByAsset.NetVolume = netVolumeByInternalWallets-netVolumeByExternalWallets;

                var netUsdVolumeByInternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetUsdVolume);
                var netUsdVolumeByExternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => !internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetUsdVolume);
                balanceByAsset.NetUsdVolume = netUsdVolumeByInternalWallets-netUsdVolumeByExternalWallets;

                var assetBalanceSettings = _assetPortfolioSettingsStorage.GetAssetPortfolioSettingsByAsset(asset);
                if (assetBalanceSettings != null)
                {
                    balanceByAsset.Settings = assetBalanceSettings;
                    balanceByAsset.SetState(assetBalanceSettings);
                }
                
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
        
        private double GetUsdProjectionByBalance(AssetBalance balance)
        {
            const string projectionAsset = "USD";
            
            var usdProjectionEntity = _anotherAssetProjectionService.GetProjectionAsync(
                new GetProjectionRequest()
                {
                    BrokerId = balance.BrokerId,
                    FromAsset = balance.Asset,
                    FromVolume = balance.Volume,
                    ToAsset = projectionAsset
                }).Result;

            return Math.Round(usdProjectionEntity.ProjectionVolume, 2);
        }
    }
}
