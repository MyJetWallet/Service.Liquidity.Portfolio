using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Grpc
{
    public interface IAssetPortfolioService
    {
        Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request);
    }
}
