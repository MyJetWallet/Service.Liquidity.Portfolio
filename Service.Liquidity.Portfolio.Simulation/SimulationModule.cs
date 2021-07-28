using Autofac;
using Service.Liquidity.Portfolio.Grpc.Simulation;

namespace Service.Liquidity.Portfolio.Simulation
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
