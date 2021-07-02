using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public interface IPortfolioStorage
    {
        public ValueTask SaveTrades(List<Trade> trades);
        public ValueTask UpdateBalancesAsync(List<Trade> trades);
        public ValueTask UpdateBalancesAsync(List<AssetBalance> balances);
        public Task SaveChangeBalanceHistoryAsync(List<AssetBalance> balances, double volumeDifference);
        public Task<List<AssetBalance>> GetBalances();
        public Task<List<ChangeBalanceHistory>> GetHistories();
        public Task<List<Trade>> GetTrades(long lastId, int batchSize);
    }
}
