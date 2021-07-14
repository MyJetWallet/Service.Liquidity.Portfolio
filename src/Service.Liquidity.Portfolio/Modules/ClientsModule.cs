using Autofac;
using MyJetWallet.Sdk.NoSql;
using Service.AssetsDictionary.Client;
using Service.BaseCurrencyConverter.Client;
using Service.Liquidity.Engine.Client;
using Service.Liquidity.Engine.Domain.Models.NoSql;
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
            
            builder.RegisterMyNoSqlReader<LpWalletNoSql>(myNoSqlClient, LpWalletNoSql.TableName);
        }
    }
}
