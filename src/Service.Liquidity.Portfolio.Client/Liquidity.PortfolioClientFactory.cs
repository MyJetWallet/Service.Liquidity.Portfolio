using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Liquidity.Portfolio.Grpc;

namespace Service.Liquidity.Portfolio.Client
{
    [UsedImplicitly]
    public class PortfolioClientFactory: MyGrpcClientFactory
    {
        public PortfolioClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IAssetPortfolioService GetHelloService() => CreateGrpcService<IAssetPortfolioService>();
    }
}
