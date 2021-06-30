using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public interface IPortfolioStorage
    {
        public ValueTask SaveTrades(IEnumerable<Trade> trades);
        public ValueTask UpdateBalances(string brokerId, List<Trade> trades);
    }
}
