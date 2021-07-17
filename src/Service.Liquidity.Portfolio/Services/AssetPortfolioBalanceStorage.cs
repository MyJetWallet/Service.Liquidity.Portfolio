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

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioBalanceStorage : IAssetPortfolioBalanceStorage, IStartable
    {
        private readonly ILogger<AssetPortfolioBalanceStorage> _logger;
        private readonly IMyNoSqlServerDataReader<LpWalletNoSql> _noSqlDataReader;
        private readonly IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> _settingsDataWriter;
        private readonly IAssetPortfolioService _assetPortfolioService;
        
        private AssetPortfolio _portfolio = new AssetPortfolio();
        private List<AssetBalance> _assetBalances = new List<AssetBalance>();
        private readonly object _locker = new object();

        public AssetPortfolioBalanceStorage(ILogger<AssetPortfolioBalanceStorage> logger,
            IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> settingsDataWriter,
            IAssetPortfolioService assetPortfolioService,
            IMyNoSqlServerDataReader<LpWalletNoSql> noSqlDataReader)
        {
            _logger = logger;
            _settingsDataWriter = settingsDataWriter;
            _assetPortfolioService = assetPortfolioService;
            _noSqlDataReader = noSqlDataReader;
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

            _portfolio.BalanceByWallet = _assetPortfolioService.GetBalanceByWallet(assetBalanceCopy, internalWallets);
            _portfolio.BalanceByAsset = _assetPortfolioService.GetBalanceByAsset(assetBalanceCopy, internalWallets);
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
    }
}
