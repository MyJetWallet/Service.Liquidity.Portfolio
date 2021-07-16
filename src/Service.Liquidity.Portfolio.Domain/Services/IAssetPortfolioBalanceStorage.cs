using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Domain.Services
{
    public interface IAssetPortfolioBalanceStorage
    {
        Task UpdateAssetPortfolioBalanceAsync(AssetBalance balance);
    }
}
