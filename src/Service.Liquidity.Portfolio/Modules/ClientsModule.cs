using Autofac;
using MyJetWallet.Sdk.NoSql;
using Service.AssetsDictionary.Client;
using Service.BaseCurrencyConverter.Client;
using Service.IndexPrices.Client;
using Service.Liquidity.InternalWallets.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Models.NoSql;
using Service.Liquidity.Portfolio.Services;
using Service.MatchingEngine.PriceSource.Client;

namespace Service.Liquidity.Portfolio.Modules
{
    public class ClientsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var myNoSqlClient = builder.CreateNoSqlClient(Program.ReloadedSettings(e => e.MyNoSqlReaderHostPort));
            builder.RegisterAssetsDictionaryClients(myNoSqlClient);
            builder.RegisterBaseCurrencyConverterClient(Program.Settings.BaseCurrencyConverterGrpcServiceUrl, myNoSqlClient);
            builder.RegisterMatchingEnginePriceSourceClient(myNoSqlClient);
            builder.RegisterIndexPricesClient(myNoSqlClient);
            
            builder.RegisterMyNoSqlReader<LpWalletNoSql>(myNoSqlClient, LpWalletNoSql.TableName);
            builder.RegisterMyNoSqlWriter<FeeShareOperationNoSqlEntity>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), FeeShareOperationNoSqlEntity.TableName);
            
            builder.RegisterIndexAssetClients(myNoSqlClient);
            
        }
    }
}
