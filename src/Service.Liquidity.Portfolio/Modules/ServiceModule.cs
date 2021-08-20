using Autofac;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.BalanceHistory.Client;
using Service.Liquidity.Converter.Client;
using Service.Liquidity.Converter.Domain.Models;
using Service.Liquidity.Engine.Domain.Models.Portfolio;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Jobs;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.PortfolioHedger.Client;
using IPortfolioHandler = Service.Liquidity.Portfolio.Domain.Services.IPortfolioHandler;

namespace Service.Liquidity.Portfolio.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);
            builder.RegisterTradeHistoryServiceBusClient(serviceBusClient,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection,
                true);
            
            builder.RegisterPortfolioHedgerServiceBusClient(serviceBusClient,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection,
                true);
            
            builder.RegisterMyNoSqlWriter<AssetPortfolioBalanceNoSql>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), AssetPortfolioBalanceNoSql.TableName);
            
            builder.RegisterMyServiceBusPublisher<AssetPortfolioTrade>(serviceBusClient, AssetPortfolioTrade.TopicName, true);
            builder.RegisterMyServiceBusPublisher<ChangeBalanceHistory>(serviceBusClient, ChangeBalanceHistory.TopicName, true);
            builder.RegisterMyServiceBusPublisher<ManualSettlement>(serviceBusClient, ManualSettlement.TopicName, true);
            
            builder
                .RegisterType<AssetPortfolioManager>()
                .AsSelf()
                .SingleInstance();
            
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
                .RegisterType<PortfolioHedgerTradeReaderJob>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
            builder
                .RegisterType<ConvertorSwapsReaderJob>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
            builder
                .RegisterType<BalancePersistJob>()
                .AsSelf()
                .SingleInstance();
            
            builder
                .RegisterType<PortfolioHandler>()
                .As<IPortfolioHandler>()
                .SingleInstance();
            builder
                .RegisterType<TradeCacheStorage>()
                .AsSelf()
                .SingleInstance();
            builder
                .RegisterType<AssetPortfolioService>()
                .As<IAssetPortfolioService>()
                .SingleInstance();
            
            builder
                .RegisterType<PortfolioMetrics>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterType<AssetPortfolioMath>()
                .AsSelf();
            
            builder
                .RegisterType<LpWalletStorage>()
                .AsSelf()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterMyServiceBusSubscriberBatch<PortfolioTrade>(serviceBusClient,
                PortfolioTrade.TopicName,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection);

            builder.RegisterMyServiceBusSubscriberBatch<SwapMessage>(serviceBusClient,
                SwapMessage.TopicName,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection);
        }
    }
}
