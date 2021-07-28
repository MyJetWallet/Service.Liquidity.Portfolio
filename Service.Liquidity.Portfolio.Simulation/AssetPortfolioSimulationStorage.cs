using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Grpc.Simulation.Models;

namespace Service.Liquidity.Portfolio.Simulation
{
    public class AssetPortfolioSimulationStorage
    {
        private readonly List<PortfolioSimulation> _simulationList = new List<PortfolioSimulation>();
        
        public async Task<PortfolioSimulation> CreateNewSimulation()
        {
            var newSimulation = new PortfolioSimulation(GenerateNewSimulationId());
            _simulationList.Add(newSimulation);
            return newSimulation;
        }

        public async Task<List<PortfolioSimulation>> GetSimulationList()
        {
            return _simulationList;
        }

        public async Task<PortfolioSimulation> GetSimulation(long simulationId)
        {
            return _simulationList.FirstOrDefault(e => e.SimulationId == simulationId);
        }

        public Task ReportSimulationTrade(ReportSimulationTradeRequest request)
        {
            throw new System.NotImplementedException();
        }

        private long GenerateNewSimulationId()
        {
            var lastId = _simulationList.Max(e => e.SimulationId);
            return ++lastId;
        }
    }
}
