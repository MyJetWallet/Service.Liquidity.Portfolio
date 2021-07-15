using MyJetWallet.Domain.ServiceBus;
using MyJetWallet.Domain.ServiceBus.Serializers;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.ServiceBus
{
    public class PortfolioTradeServiceBusSubscriber : Subscriber<AssetPortfolioTrade>
    {
        public PortfolioTradeServiceBusSubscriber(MyServiceBusTcpClient client, string queueName, TopicQueueType queryType, bool batchSubscriber) :
            base(client, AssetPortfolioTrade.TopicName, queueName, queryType,
                bytes => bytes.ByteArrayToServiceBusContract<AssetPortfolioTrade>(), batchSubscriber)
        {
        }
    }
}
