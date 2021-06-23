using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Service.Liquidity.Portfolio.Grpc;

namespace Service.Service.Liquidity.Portfolio.Client
{
    [UsedImplicitly]
    public class Service.Liquidity.PortfolioClientFactory: MyGrpcClientFactory
    {
        public Service.Liquidity.PortfolioClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IHelloService GetHelloService() => CreateGrpcService<IHelloService>();
    }
}
