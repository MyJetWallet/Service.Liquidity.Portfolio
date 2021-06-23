using Autofac;
using Service.Service.Liquidity.Portfolio.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Service.Liquidity.Portfolio.Client
{
    public static class AutofacHelper
    {
        public static void RegisterService.Liquidity.PortfolioClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new Service.Liquidity.PortfolioClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}
