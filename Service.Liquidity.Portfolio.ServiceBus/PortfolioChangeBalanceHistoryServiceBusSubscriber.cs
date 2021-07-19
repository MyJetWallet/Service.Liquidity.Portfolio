using MyJetWallet.Domain.ServiceBus;
using MyJetWallet.Domain.ServiceBus.Serializers;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.ServiceBus
{
    public class PortfolioChangeBalanceHistoryServiceBusSubscriber : Subscriber<ChangeBalanceHistory>
    {
        public PortfolioChangeBalanceHistoryServiceBusSubscriber(MyServiceBusTcpClient client, string queueName, TopicQueueType queryType, bool batchSubscriber) :
            base(client, ChangeBalanceHistory.TopicName, queueName, queryType,
                bytes => bytes.ByteArrayToServiceBusContract<ChangeBalanceHistory>(), batchSubscriber)
        {
        }
    }
}
