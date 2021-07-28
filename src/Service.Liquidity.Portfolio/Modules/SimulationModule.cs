using Autofac;
using Service.Liquidity.Portfolio.Grpc.Simulation;
using Service.Liquidity.Portfolio.Simulation;

namespace Service.Liquidity.Portfolio.Modules
{
    public class SimulationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<AssetPortfolioSimulationService>()
                .As<IAssetPortfolioSimulationService>();
            
            builder
                .RegisterType<AssetPortfolioSimulationStorage>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
