using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public interface IPortfolioHandler
    {
        ValueTask HandleTradesAsync(List<AssetPortfolioTrade> trades);
        void UpdateBalance(List<AssetBalance> differenceBalances);
        Task SaveChangeBalanceHistoryAsync(ChangeBalanceHistory balanceHistory);
        List<AssetBalance> GetBalancesSnapshot();
        Task<List<ChangeBalanceHistory>> GetHistories();
    }
}
