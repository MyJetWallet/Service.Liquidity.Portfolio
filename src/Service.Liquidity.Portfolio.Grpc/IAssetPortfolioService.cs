using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Grpc.Models.GetBalances;

namespace Service.Liquidity.Portfolio.Grpc
{
    [ServiceContract]
    public interface IAssetPortfolioService
    {
        [OperationContract]
        Task<GetBalancesResponse> GetBalancesAsync();
        [OperationContract]
        Task<GetChangeBalanceHistoryResponse> GetChangeBalanceHistoryAsync();
        
        [OperationContract]
        Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request);
    }
}
