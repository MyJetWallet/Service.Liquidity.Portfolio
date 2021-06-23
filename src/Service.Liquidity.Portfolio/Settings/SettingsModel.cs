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

        [YamlProperty("LiquidityPortfolio.LiquidityEngineGrpcServiceUrl")]
        public string LiquidityEngineGrpcServiceUrl { get; set; }
    }
}
