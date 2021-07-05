using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public interface IPortfolioHandler
    {
        ValueTask HandleTradesAsync(List<Trade> trades);
        void UpdateBalance(List<AssetBalance> differenceBalances);
        Task SaveChangeBalanceHistoryAsync(List<AssetBalance> balances, double volumeDifference);
        List<AssetBalance> GetBalancesSnapshot();
        Task<List<ChangeBalanceHistory>> GetHistories();
        Task<List<Trade>> GetTrades(long lastId, int batchSize);
    }
}
