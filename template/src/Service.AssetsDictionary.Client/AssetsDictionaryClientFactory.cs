using System;
using Grpc.Net.Client;
using JetBrains.Annotations;
using ProtoBuf.Grpc.Client;
using Service.AssetsDictionary.Grpc;

namespace Service.AssetsDictionary.Client
{
    [UsedImplicitly]
    public class AssetsDictionaryClientFactory
    {
        //private readonly CallInvoker _channel;
        private readonly GrpcChannel _channel;

        public AssetsDictionaryClientFactory(string assetsDictionaryGrpcServiceUrl)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            _channel = GrpcChannel.ForAddress(assetsDictionaryGrpcServiceUrl);
            //_channel = channel.Intercept(new RequestDurationInterceptor());
            //_channel = channel;
        }

        public IHelloService GetHelloService() => _channel.CreateGrpcService<IHelloService>();
    }
}
