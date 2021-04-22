using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.AssetsDictionary.Settings
{
    public class SettingsModel
    {
        [YamlProperty("AssetsDictionary.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("AssetsDictionary.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("AssetsDictionary.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
    }
}
