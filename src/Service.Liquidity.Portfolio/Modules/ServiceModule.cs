using Autofac;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.BalanceHistory.Client;
using Service.Liquidity.Engine.Domain.Models.Portfolio;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models.GetBalances;
using Service.Liquidity.Portfolio.Jobs;
using Service.Liquidity.Portfolio.Postgres;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.Portfolio.Services.Grpc;
using Service.Liquidity.PortfolioHedger.Client;

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
            
            builder.RegisterMyNoSqlWriter<AssetPortfolioSettingsNoSql>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), AssetPortfolioSettingsNoSql.TableName);
            builder.RegisterMyServiceBusPublisher<NetBalanceByAsset>(serviceBusClient, NetBalanceByAsset.TopicName, true);
            
            builder.RegisterMyServiceBusPublisher<AssetPortfolioTrade>(serviceBusClient, AssetPortfolioTrade.TopicName, true);
            
            builder
                .RegisterType<AssetPortfolioSettingsStorage>()
                .As<IAssetPortfolioSettingsStorage>()
                .As<IStartable>()
                .AutoActivate()
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
                .RegisterType<AssetBalanceWriterJob>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
            
            builder
                .RegisterType<PortfolioHandler>()
                .As<IPortfolioHandler>()
                .SingleInstance();
            
            builder
                .RegisterType<BalancePersistJob>()
                .AsSelf()
                .SingleInstance();
            
            builder
                .RegisterType<TradeCacheStorage>()
                .AsSelf()
                .SingleInstance();
            
            builder
                .RegisterType<AnotherAssetProjectionService>()
                .As<IAnotherAssetProjectionService>()
                .SingleInstance();

            builder
                .RegisterType<AssetPortfolioService>()
                .As<IAssetPortfolioService>();

            builder.RegisterMyServiceBusSubscriberBatch<Engine.Domain.Models.Portfolio.PortfolioTrade>(serviceBusClient,
                Engine.Domain.Models.Portfolio.PortfolioTrade.TopicName,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection);
        }
    }
}
