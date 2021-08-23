using Service.Liquidity.Portfolio.Grpc.Simulation.Models;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.Portfolio.Tests;

namespace Service.Liquidity.Portfolio.Simulation.Models
{
    public class SimulationStorage
    {
        public PortfolioSimulation SimulationEntity { get; set; }
        public readonly BalanceHandler BalanceHandler;
        public readonly IndexPricesClientMock IndexPricesClientMock;

        public SimulationStorage(BalanceHandler balanceHandler,
            IndexPricesClientMock indexPricesClientMock,
            PortfolioSimulation simulationEntity)
        {
            BalanceHandler = balanceHandler;
            IndexPricesClientMock = indexPricesClientMock;
            SimulationEntity = simulationEntity;
        }
    }
}
