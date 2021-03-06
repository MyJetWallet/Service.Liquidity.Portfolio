using Autofac;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.BalanceHistory.Client;
using Service.FeeShareEngine.Client;
using Service.FeeShareEngine.Domain.Models.Models;
using Service.Liquidity.Converter.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Jobs;
using Service.Liquidity.Portfolio.Services;
using Service.Liquidity.Portfolio.Services.Grpc;
using Service.Liquidity.PortfolioHedger.Client;

namespace Service.Liquidity.Portfolio.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), Program.LogFactory);
            builder.RegisterTradeHistoryServiceBusClient(serviceBusClient,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection,
                true);
            
            builder.RegisterPortfolioHedgerServiceBusClient(serviceBusClient,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection,
                true);
            
            builder.RegisterMyServiceBusSubscriberSingle<FeeShareEntity>(serviceBusClient, FeeShareEntity.TopicName, $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}", TopicQueueType.PermanentWithSingleConnection);
            builder.RegisterMyNoSqlWriter<AssetPortfolioBalanceNoSql>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), AssetPortfolioBalanceNoSql.TableName);
            
            builder.RegisterMyServiceBusPublisher<AssetPortfolioTrade>(serviceBusClient, AssetPortfolioTrade.TopicName, true);
            builder.RegisterMyServiceBusPublisher<ChangeBalanceHistory>(serviceBusClient, ChangeBalanceHistory.TopicName, true);
            builder.RegisterMyServiceBusPublisher<ManualSettlement>(serviceBusClient, ManualSettlement.TopicName, true);
            builder.RegisterMyServiceBusPublisher<FeeShareSettlement>(serviceBusClient, FeeShareSettlement.TopicName, true);

            builder
                .RegisterType<BalanceHandler>()
                .AsSelf()
                .SingleInstance();
            builder
                .RegisterType<BalanceUpdater>()
                .AsSelf()
                .SingleInstance();
            
            builder
                .RegisterType<BalanceHistoryTradeReaderJob>()
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
                .RegisterType<TradeHandler>()
                .As<ITradeHandler>()
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
                .RegisterType<LpWalletStorage>()
                .AsSelf()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<FeeShareHandler>()
                .AsSelf()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterMyServiceBusSubscriberBatch<SwapMessage>(serviceBusClient,
                SwapMessage.TopicName,
                $"LiquidityPortfolio-{Program.Settings.ServiceBusQuerySuffix}",
                TopicQueueType.PermanentWithSingleConnection);
            
            builder
                .RegisterType<FeeShareOperationCache>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterType<IndexDecompositionService>()
                .As<IIndexDecompositionService>()
                .SingleInstance();
        }
    }
}
