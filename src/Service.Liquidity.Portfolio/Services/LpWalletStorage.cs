﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Engine.Domain.Models.NoSql;

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

        public List<string> GetWallets()
        {
            if (_noSqlDataReader.Get().Select(elem => elem.Wallet.Name).ToList().Any())
            {
                return _noSqlDataReader.Get().Select(elem => elem.Wallet.Name).ToList();
            }

            if (_timerCounter >= 60) 
                return _noSqlDataReader.Get().Select(elem => elem.Wallet.Name).ToList();
            
            _timerCounter++;
            Thread.Sleep(1000);

            return GetWallets();
        }
    }
}
