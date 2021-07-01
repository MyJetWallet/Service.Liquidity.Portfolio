using Autofac;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.BalanceHistory.Client;
using Service.Liquidity.Engine.Domain.Models.Portfolio;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Jobs;
using Service.Liquidity.Portfolio.Postgres;
using Service.Liquidity.Portfolio.Services;

namespace Service.Liquidity.Portfolio.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);
            builder.RegisterTradeHistoryServiceBusClient(serviceBusClient, $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}", TopicQueueType.PermanentWithSingleConnection, true);
            
            builder
                .RegisterType<BalanceHistoryTradeReaderJob>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
            
            builder
                .RegisterType<LiquidityEngineTradeReaderJob>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
            
            builder
                .RegisterType<PortfolioStorage>()
                .As<IPortfolioStorage>()
                .SingleInstance();
            
            builder
                .RegisterType<AnotherAssetProjectionService>()
                .As<IAnotherAssetProjectionService>()
                .SingleInstance();

            builder.RegisterMyServiceBusSubscriberBatch<PortfolioTrade>(serviceBusClient,
                PortfolioTrade.TopicName,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection);
        }
    }
}
