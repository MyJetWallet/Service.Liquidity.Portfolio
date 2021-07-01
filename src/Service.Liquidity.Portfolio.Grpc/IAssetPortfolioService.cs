using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Grpc
{
    [ServiceContract]
    public interface IAssetPortfolioService
    {
        [OperationContract]
        Task<GetBalancesResponse> GetBalancesAsync();
        
        [OperationContract]
        Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request);
        
        [OperationContract]
        Task<GetTradesResponse> GetTradesAsync(GetTradesRequest request);
    }
}
