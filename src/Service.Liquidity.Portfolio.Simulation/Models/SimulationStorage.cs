using Service.Liquidity.Portfolio.Grpc.Simulation.Models;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.Portfolio.Tests;

namespace Service.Liquidity.Portfolio.Simulation.Models
{
    public class SimulationStorage
    {
        public PortfolioSimulation SimulationEntity { get; set; }
        public readonly AssetPortfolioMath AssetPortfolioMath;
        public readonly AssetPortfolioManager AssetPortfolioManager;
        public readonly IndexPricesClientMock IndexPricesClientMock;

        public SimulationStorage(AssetPortfolioMath assetPortfolioMath,
            AssetPortfolioManager assetPortfolioManager,
            IndexPricesClientMock indexPricesClientMock,
            PortfolioSimulation simulationEntity)
        {
            AssetPortfolioMath = assetPortfolioMath;
            AssetPortfolioManager = assetPortfolioManager;
            IndexPricesClientMock = indexPricesClientMock;
            SimulationEntity = simulationEntity;
        }
    }
}
