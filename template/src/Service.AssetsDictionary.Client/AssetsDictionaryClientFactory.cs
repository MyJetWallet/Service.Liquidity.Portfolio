using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.AssetsDictionary.Grpc;

namespace Service.AssetsDictionary.Client
{
    [UsedImplicitly]
    public class AssetsDictionaryClientFactory: MyGrpcClientFactory
    {
        public AssetsDictionaryClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IHelloService GetHelloService() => CreateGrpcService<IHelloService>();
    }
}
