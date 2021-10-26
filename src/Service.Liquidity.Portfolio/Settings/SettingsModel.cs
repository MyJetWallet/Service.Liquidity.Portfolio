using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.Liquidity.Portfolio.Settings
{
    public class SettingsModel
    {
        [YamlProperty("LiquidityPortfolio.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("LiquidityPortfolio.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("LiquidityPortfolio.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("LiquidityPortfolio.ServiceBusQuerySuffix")]
        public string ServiceBusQuerySuffix { get; set; }
        
        [YamlProperty("LiquidityPortfolio.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }
        
        [YamlProperty("LiquidityPortfolio.PostgresConnectionString")]
        public string PostgresConnectionString { get; set; }

        [YamlProperty("LiquidityPortfolio.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }
        
        [YamlProperty("LiquidityPortfolio.BaseCurrencyConverterGrpcServiceUrl")]
        public string BaseCurrencyConverterGrpcServiceUrl { get; set; }

        [YamlProperty("LiquidityPortfolio.UpdateNoSqlBalancesTimerInSeconds")]
        public int UpdateNoSqlBalancesTimerInSeconds { get; set; }

        [YamlProperty("LiquidityPortfolio.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }
        
        [YamlProperty("LiquidityPortfolio.FeeShareWalletId")]
        public string FeeShareWalletId { get; set; }
    }
}
