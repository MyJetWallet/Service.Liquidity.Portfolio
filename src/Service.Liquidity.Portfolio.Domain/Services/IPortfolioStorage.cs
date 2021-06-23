using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Domain.Services
{
    public interface IPortfolioStorage
    {
        Task SaveAsync(List<Trade> trades);
    }
}
