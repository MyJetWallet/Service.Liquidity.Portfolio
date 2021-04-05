using Autofac;
using Service.AssetsDictionary.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.AssetsDictionary.Client
{
    public static class AutofacHelper
    {
        public static void RegisterAssetsDictionaryClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new AssetsDictionaryClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}
