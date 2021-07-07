using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Grpc
{
    [ServiceContract]
    public interface IAssetPortfolioSettingsManager
    {
        [OperationContract]
        GetAssetPortfolioSettingsResponse GetAssetPortfolioSettings();
        
        [OperationContract]
        Task UpdateAssetPortfolioSettingsAsync(AssetPortfolioSettings settings);
    }
}
