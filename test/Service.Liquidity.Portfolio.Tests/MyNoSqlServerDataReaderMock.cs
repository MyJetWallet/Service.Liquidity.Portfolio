using System;
using System.Collections.Generic;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.InternalWallets.Domain.Models;

namespace Service.Liquidity.Portfolio.Tests
{
    public class MyNoSqlServerDataReaderMock : IMyNoSqlServerDataReader<LpWalletNoSql>
    {
        public LpWalletNoSql Get(string partitionKey, string rowKey)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<LpWalletNoSql> Get(string partitionKey)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<LpWalletNoSql> Get(string partitionKey, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<LpWalletNoSql> Get(string partitionKey, int skip, int take, Func<LpWalletNoSql, bool> condition)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<LpWalletNoSql> Get(string partitionKey, Func<LpWalletNoSql, bool> condition)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<LpWalletNoSql> Get(Func<LpWalletNoSql, bool> condition = null)
        {
            return new List<LpWalletNoSql>()
            {
                new LpWalletNoSql()
                {
                    Wallet = new LpWallet()
                    {
                        Name = "LP"
                    }
                }
            };
        }

        public int Count()
        {
            throw new NotImplementedException();
        }

        public int Count(string partitionKey)
        {
            throw new NotImplementedException();
        }

        public int Count(string partitionKey, Func<LpWalletNoSql, bool> condition)
        {
            throw new NotImplementedException();
        }

        public IMyNoSqlServerDataReader<LpWalletNoSql> SubscribeToUpdateEvents(Action<IReadOnlyList<LpWalletNoSql>> updateSubscriber, Action<IReadOnlyList<LpWalletNoSql>> deleteSubscriber)
        {
            throw new NotImplementedException();
        }
    }
}
