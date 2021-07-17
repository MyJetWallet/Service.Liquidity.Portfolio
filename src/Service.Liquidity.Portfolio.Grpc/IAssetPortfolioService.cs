using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Grpc
{
    public interface IAssetPortfolioService
    {
        Task<GetChangeBalanceHistoryResponse> GetChangeBalanceHistoryAsync();
        
        Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request);
    }
}
