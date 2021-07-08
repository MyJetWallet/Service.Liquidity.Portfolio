using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioSettingsStorage : IAssetPortfolioSettingsStorage, IStartable
    {
        private readonly ILogger<AssetPortfolioSettingsStorage> _logger;
        private readonly IMyNoSqlServerDataWriter<AssetPortfolioSettingsNoSql> _settingsDataWriter;
        
        private Dictionary<string, AssetPortfolioSettings> _settings = new Dictionary<string, AssetPortfolioSettings>();

        public AssetPortfolioSettingsStorage(ILogger<AssetPortfolioSettingsStorage> logger,
            IMyNoSqlServerDataWriter<AssetPortfolioSettingsNoSql> settingsDataWriter)
        {
            _logger = logger;
            _settingsDataWriter = settingsDataWriter;
        }

        public AssetPortfolioSettings GetAssetPortfolioSettingsByAsset(string asset)
        {
            return _settings.TryGetValue(asset, out var result) ? result : null;
        }

        public List<AssetPortfolioSettings> GetAssetPortfolioSettings()
        {
            return _settings.Values.ToList();
        }

        public async Task UpdateAssetPortfolioSettingsAsync(AssetPortfolioSettings settings)
        {
            await _settingsDataWriter.InsertOrReplaceAsync(AssetPortfolioSettingsNoSql.Create(settings));

            await ReloadSettings();

            _logger.LogInformation("Updated LiquidityConverterSettings Settings: {jsonText}",
                JsonConvert.SerializeObject(settings));
        }

        private async Task ReloadSettings()
        {
            var settings = (await _settingsDataWriter.GetAsync()).ToList();

            var settingsMap = new Dictionary<string, AssetPortfolioSettings>();
            foreach (var settingsLiquidityConverterNoSql in settings)
            {
                settingsMap[settingsLiquidityConverterNoSql.Settings.Asset] =
                    settingsLiquidityConverterNoSql.Settings;
            }

            _settings = settingsMap;
        }

        public void Start()
        {
            ReloadSettings().GetAwaiter().GetResult();
        }
    }
}
