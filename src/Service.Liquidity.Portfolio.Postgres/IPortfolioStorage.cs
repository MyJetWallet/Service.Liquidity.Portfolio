using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public interface IPortfolioStorage
    {
      
        public ValueTask SaveTrades(List<Trade> trades);
        public void UpdateBalances(List<Trade> trades);
        public void UpdateBalances(List<AssetBalance> differenceBalances);
        public Task SaveChangeBalanceHistoryAsync(List<AssetBalance> balances, double volumeDifference);
        public List<AssetBalance> GetBalancesSnapshot();
        public Task<List<ChangeBalanceHistory>> GetHistories();
        public Task<List<Trade>> GetTrades(long lastId, int batchSize);
    }
}
