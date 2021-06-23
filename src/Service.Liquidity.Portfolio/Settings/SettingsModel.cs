using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.Liquidity.Portfolio.Settings
{
    public class SettingsModel
    {
        [YamlProperty("Liquidity.Portfolio.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("Liquidity.Portfolio.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("Liquidity.Portfolio.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
    }
}
