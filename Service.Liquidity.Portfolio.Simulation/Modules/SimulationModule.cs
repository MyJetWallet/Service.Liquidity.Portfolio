using Autofac;
using Service.Liquidity.Portfolio.Grpc.Simulation;
using Service.Liquidity.Portfolio.Simulation.Services;

namespace Service.Liquidity.Portfolio.Simulation.Modules
{
    public class SimulationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<AssetPortfolioSimulationService>()
                .As<IAssetPortfolioSimulationService>();
            
            builder
                .RegisterType<AssetPortfolioSimulationManager>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
