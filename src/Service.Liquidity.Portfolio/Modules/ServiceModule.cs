using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.BalanceHistory.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Jobs;
using Service.Liquidity.Engine.Client;
using Service.Liquidity.Engine.Grpc;

namespace Service.Liquidity.Portfolio.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);
            builder.RegisterTradeHistoryServiceBusClient(serviceBusClient, $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}", TopicQueueType.PermanentWithSingleConnection, true);

            builder.RegisterLiquidityEngineClient(Program.Settings.LiquidityEngineGrpcServiceUrl);
                
            builder
                .RegisterType<TradeReaderJob>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
            
            builder
                .RegisterInstance(new MyServiceBusPublisher<PortfolioTrade>(serviceBusClient, PortfolioTrade.TopicName, false))
                .As<IPublisher<PortfolioTrade>>()
                .SingleInstance();
        }
    }
}
