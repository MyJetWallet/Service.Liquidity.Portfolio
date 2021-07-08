using System;
using JetBrains.Annotations;
using MyJetWallet.Domain.ServiceBus;
using MyJetWallet.Domain.ServiceBus.Serializers;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.Liquidity.Portfolio.Grpc.Models.GetBalances;

namespace Service.Liquidity.Portfolio.ServiceBus
{
    [UsedImplicitly]
    public class AssetBalanceServiceBusSubscriber : Subscriber<NetBalanceByAsset>
    {
        public AssetBalanceServiceBusSubscriber(MyServiceBusTcpClient client, string queueName, TopicQueueType queryType, bool batchSubscriber) :
            base(client, NetBalanceByAsset.TopicName, queueName, queryType,
                bytes => bytes.ByteArrayToServiceBusContract<NetBalanceByAsset>(), batchSubscriber)
        {
        }
    }
}
