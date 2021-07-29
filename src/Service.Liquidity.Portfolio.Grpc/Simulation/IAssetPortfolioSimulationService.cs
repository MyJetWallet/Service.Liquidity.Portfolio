using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Grpc.Simulation.Models;

namespace Service.Liquidity.Portfolio.Grpc.Simulation
{
    [ServiceContract]
    public interface IAssetPortfolioSimulationService
    {
        [OperationContract]
        public Task<CreateNewSimulationResponse> CreateNewSimulation();

        [OperationContract]
        public Task<GetSimulationListResponse> GetSimulationList();
        
        [OperationContract]
        public Task<GetSimulationResponse> GetSimulation(GetSimulationRequest request);
        
        [OperationContract]
        public Task<DeleteSimulationResponse> DeleteSimulation(DeleteSimulationRequest request);

        [OperationContract]
        public Task<ReportSimulationTradeResponse> ReportSimulationTrade(ReportSimulationTradeRequest request);
        
        [OperationContract]
        public Task<SetSimulationPricesResponse> SetSimulationPrices(SetSimulationPricesRequest request);
    }
}
