using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Domain.Services
{
    public interface IAssetPortfolioBalanceStorage
    {
        Task SavePortfolioToNoSql();
        void UpdateBalance(IEnumerable<AssetBalance> differenceBalances);
        List<AssetBalance> GetBalancesSnapshot();
    }
}
