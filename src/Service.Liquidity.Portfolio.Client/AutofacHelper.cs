using Autofac;
using Service.Liquidity.Portfolio.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Liquidity.Portfolio.Client
{
    public static class AutofacHelper
    {
        public static void RegisterLiquidity.PortfolioClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new Liquidity.PortfolioClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}
