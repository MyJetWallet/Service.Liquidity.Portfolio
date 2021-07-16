using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioBalanceStorage : IAssetPortfolioBalanceStorage, IStartable
    {
        private readonly ILogger<AssetPortfolioBalanceStorage> _logger;
        private readonly IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> _settingsDataWriter;
        private List<AssetBalance> _balances = new List<AssetBalance>();

        public AssetPortfolioBalanceStorage(ILogger<AssetPortfolioBalanceStorage> logger,
            IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> settingsDataWriter)
        {
            _logger = logger;
            _settingsDataWriter = settingsDataWriter;
        }

        public async Task UpdateAssetPortfolioBalanceAsync(AssetBalance balance)
        {
            var lastBalance = _balances.FirstOrDefault(elem =>
                elem.WalletName == balance.WalletName && elem.Asset == balance.Asset);

            if (lastBalance != null)
            {
                balance.Volume += lastBalance.Volume;
            }

            await _settingsDataWriter.InsertOrReplaceAsync(AssetPortfolioBalanceNoSql.Create(balance));
            await ReloadBalance();
        }

        private async Task ReloadBalance()
        {
            var balances = (await _settingsDataWriter.GetAsync()).ToList();
            if (balances.Any())
            {
                _balances = new List<AssetBalance>();
                balances.ForEach(noSqlBalance =>
                {
                    _balances.Add(noSqlBalance.Balance);
                });
            }
        }

        public void Start()
        {
            ReloadBalance().GetAwaiter().GetResult();
        }
    }
}
