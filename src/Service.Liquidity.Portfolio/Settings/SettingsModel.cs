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
    }
}
