using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public interface IPortfolioStorage
    {
        public ValueTask SaveTrades(List<Trade> trades);
        public ValueTask UpdateBalances(List<Trade> trades);
        public ValueTask UpdateBalances(List<AssetBalance> balances);
        public Task<List<AssetBalance>> GetBalances();
        public Task<List<Trade>> GetTrades(long lastId, int batchSize);
    }
}
