using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Grpc
{
    public interface IAssetPortfolioService
    {
        Task<AssetPortfolio> GetBalancesAsync();
        Task<GetChangeBalanceHistoryResponse> GetChangeBalanceHistoryAsync();
        
        Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request);

        List<NetBalanceByAsset> GetBalanceByAsset(List<AssetBalance> balancesSnapshot,
            ICollection<string> internalWallets);

        List<NetBalanceByWallet> GetBalanceByWallet(List<AssetBalance> balancesSnapshot,
            ICollection<string> internalWallets);
    }
}
