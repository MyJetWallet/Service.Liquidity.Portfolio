using Autofac;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Simulation;

// ReSharper disable UnusedMember.Global

namespace Service.Liquidity.Portfolio.Client
{
    public static class AutofacHelper
    {
        public static void RegisterPortfolioClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new PortfolioClientFactory(grpcServiceUrl);
            builder.RegisterInstance(factory.GetAssetPortfolioService()).As<IAssetPortfolioService>().SingleInstance();
        }
        
        public static void RegisterPortfolioSimulationClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new PortfolioClientFactory(grpcServiceUrl);
            builder.RegisterInstance(factory.GetAssetPortfolioSimulationService()).As<IAssetPortfolioSimulationService>().SingleInstance();
        }
    }
}
