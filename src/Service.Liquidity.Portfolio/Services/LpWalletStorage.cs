using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.InternalWallets.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class LpWalletStorage : IStartable
    {
        private readonly IMyNoSqlServerDataReader<LpWalletNoSql> _noSqlDataReader;
        private int _timerCounter = 0;

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
            var cache = _noSqlDataReader.Get().Select(elem => elem.Wallet.Name);
        }

        public IEnumerable<LpWallet> GetWallets()
        {
            if (_noSqlDataReader.Get().Any())
            {
                return _noSqlDataReader.Get().Select(e => e.Wallet);
            }

            if (_timerCounter >= 60) 
                return _noSqlDataReader.Get().Select(e => e.Wallet);
            
            _timerCounter++;
            Thread.Sleep(1000);

            return GetWallets();
        }

        public string GetWalletNameById(string walletId)
        {
            return _noSqlDataReader.Get().FirstOrDefault(elem => elem.Wallet.WalletId == walletId)?.Wallet.Name;
        }
    }
}
