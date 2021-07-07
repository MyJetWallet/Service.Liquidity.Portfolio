using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Services.Grpc
{
    public class AssetPortfolioSettingsManager : IAssetPortfolioSettingsManager
    {
        private readonly IAssetPortfolioSettingsStorage _assetPortfolioSettingsStorage;

        public AssetPortfolioSettingsManager(IAssetPortfolioSettingsStorage assetPortfolioSettingsStorage)
        {
            _assetPortfolioSettingsStorage = assetPortfolioSettingsStorage;
        }

        public GetAssetPortfolioSettingsResponse GetAssetPortfolioSettings()
        {
            var settings = _assetPortfolioSettingsStorage.GetAssetPortfolioSettings();

            if (settings == null || settings.Count == 0)
                return new GetAssetPortfolioSettingsResponse()
                {
                    Success = false,
                    ErrorMessage = "Asset settings not found"
                };
            
            var response = new GetAssetPortfolioSettingsResponse()
            {
                Settings = settings,
                Success = true
            };
            return response;
        }

        public Task UpdateAssetPortfolioSettingsAsync(AssetPortfolioSettings settings)
        {
            return _assetPortfolioSettingsStorage.UpdateAssetPortfolioSettingsAsync(settings);
        }
    }
}
