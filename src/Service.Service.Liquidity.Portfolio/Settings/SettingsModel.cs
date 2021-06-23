using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.Service.Liquidity.Portfolio.Settings
{
    public class SettingsModel
    {
        [YamlProperty("Service.Liquidity.Portfolio.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("Service.Liquidity.Portfolio.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("Service.Liquidity.Portfolio.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
    }
}
