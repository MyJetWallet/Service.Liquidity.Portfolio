using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Grpc.Simulation.Models;

namespace Service.Liquidity.Portfolio.Grpc.Simulation
{
    [ServiceContract]
    public interface IAssetPortfolioSimulationService
    {
        [OperationContract]
        public Task<PortfolioSimulation> CreateNewSimulation();

        public Task<GetSimulationListResponse> GetSimulationList();
        
        public Task<GetSimulationResponse> GetSimulation(GetSimulationRequest request);

        public Task<ReportSimulationTradeResponse> ReportSimulationTrade(ReportSimulationTradeRequest request);
    }
}
