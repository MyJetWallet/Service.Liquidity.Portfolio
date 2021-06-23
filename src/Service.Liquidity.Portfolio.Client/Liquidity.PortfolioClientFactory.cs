using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Liquidity.Portfolio.Grpc;

namespace Service.Liquidity.Portfolio.Client
{
    [UsedImplicitly]
    public class Liquidity.PortfolioClientFactory: MyGrpcClientFactory
    {
        public Liquidity.PortfolioClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IHelloService GetHelloService() => CreateGrpcService<IHelloService>();
    }
}
