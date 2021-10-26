using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Domain.Services
{
    public interface ITradeHandler
    {
        ValueTask HandleTradesAsync(List<AssetPortfolioTrade> trades);
        Task SaveChangeBalanceHistoryAsync(ChangeBalanceHistory balanceHistory);
        Task SaveManualSettlementHistoryAsync(ManualSettlement settlement);
        Task SaveFeeShareSettlementHistoryAsync(FeeShareSettlement settlement);
    }
}
