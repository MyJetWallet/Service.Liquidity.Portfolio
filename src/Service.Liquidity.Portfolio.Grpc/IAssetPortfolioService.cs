using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Grpc
{
    [ServiceContract]
    public interface IAssetPortfolioService
    {
        [OperationContract]
        Task<SetBalanceResponse> SetBalance(SetBalanceRequest request);

        Task<ReportSettlementResponse> ReportSettlement(ManualSettlement request);
    }
}
