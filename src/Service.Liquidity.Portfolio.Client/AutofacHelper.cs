using System.Collections.Generic;
using Autofac;
using DotNetCoreDecorators;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models.GetBalances;
using Service.Liquidity.Portfolio.ServiceBus;

// ReSharper disable UnusedMember.Global

namespace Service.Liquidity.Portfolio.Client
{
    public static class AutofacHelper
    {
        public static void RegisterPortfolioClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new PortfolioClientFactory(grpcServiceUrl);
            builder.RegisterInstance(factory.GetHelloService()).As<IAssetPortfolioService>().SingleInstance();
        }
        
        public static void RegisterAssetBalanceServiceBusClient(this ContainerBuilder builder, MyServiceBusTcpClient client, string queueName, TopicQueueType queryType, bool batchSubscriber)
        {
            if (batchSubscriber)
            {
                builder
                    .RegisterInstance(new AssetBalanceServiceBusSubscriber(client, queueName, queryType, true))
                    .As<ISubscriber<IReadOnlyList<NetBalanceByAsset>>>()
                    .SingleInstance();
            }
            else
            {
                builder
                    .RegisterInstance(new AssetBalanceServiceBusSubscriber(client, queueName, queryType, false))
                    .As<ISubscriber<NetBalanceByAsset>>()
                    .SingleInstance();
            }
        }
    }
}
