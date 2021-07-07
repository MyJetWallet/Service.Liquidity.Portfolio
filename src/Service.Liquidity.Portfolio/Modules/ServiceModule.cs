using Autofac;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataWriter;
using MyServiceBus.Abstractions;
using Service.BalanceHistory.Client;
using Service.Liquidity.Engine.Domain.Models.Portfolio;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Jobs;
using Service.Liquidity.Portfolio.Postgres;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.Portfolio.Services.Grpc;

namespace Service.Liquidity.Portfolio.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);
            builder.RegisterTradeHistoryServiceBusClient(serviceBusClient, $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}", TopicQueueType.PermanentWithSingleConnection, true);
            
            RegisterMyNoSqlWriter<AssetPortfolioSettingsNoSql>(builder, AssetPortfolioSettingsNoSql.TableName);
            
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

            builder.RegisterMyServiceBusSubscriberBatch<PortfolioTrade>(serviceBusClient,
                PortfolioTrade.TopicName,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection);
        }
        
        private void RegisterMyNoSqlWriter<TEntity>(ContainerBuilder builder, string table)
            where TEntity : IMyNoSqlDbEntity, new()
        {
            builder.Register(ctx => new MyNoSqlServerDataWriter<TEntity>(
                    Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), table, true))
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}
