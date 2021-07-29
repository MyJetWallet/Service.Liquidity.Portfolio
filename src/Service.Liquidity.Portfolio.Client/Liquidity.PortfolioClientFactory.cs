using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Simulation;

namespace Service.Liquidity.Portfolio.Client
{
    [UsedImplicitly]
    public class PortfolioClientFactory: MyGrpcClientFactory
    {
        public PortfolioClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IAssetPortfolioService GetAssetPortfolioService() => CreateGrpcService<IAssetPortfolioService>();
        public IAssetPortfolioSimulationService GetAssetPortfolioSimulationService() => CreateGrpcService<IAssetPortfolioSimulationService>();
    }
}
