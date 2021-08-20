using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Engine.Domain.Models.NoSql;

namespace Service.Liquidity.Portfolio.Services
{
    public class LpWalletStorage : IStartable
    {
        private readonly IMyNoSqlServerDataReader<LpWalletNoSql> _noSqlDataReader;
        private List<string> Wallets { get; set; } = new List<string>();

        public LpWalletStorage(IMyNoSqlServerDataReader<LpWalletNoSql> noSqlDataReader)
        {
            _noSqlDataReader = noSqlDataReader;
        }

        public void Start()
        {
            RefreshData().GetAwaiter().GetResult();
        }

        private async Task RefreshData()
        {
            Wallets = _noSqlDataReader.Get().Select(elem => elem.Wallet.Name).ToList();
        }

        public List<string> GetWallets()
        {
            if (Wallets.Any())
            {
                return Wallets;
            }
            RefreshData().GetAwaiter().GetResult();
            return Wallets;
        }
    }
}
