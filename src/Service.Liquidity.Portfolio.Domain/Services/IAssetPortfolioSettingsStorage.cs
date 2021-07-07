using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Domain.Services
{
    public interface IAssetPortfolioSettingsStorage
    {
        AssetPortfolioSettings GetAssetPortfolioSettingsByAsset(string asset);
        List<AssetPortfolioSettings> GetAssetPortfolioSettings();
        Task UpdateAssetPortfolioSettingsAsync(AssetPortfolioSettings settings);
    }
}
