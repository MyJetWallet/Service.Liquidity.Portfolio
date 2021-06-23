using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Postgres
{
    public class PortfolioStorage : IPortfolioStorage
    {
        
        private readonly ILogger<PortfolioStorage> _logger;
        private readonly DbContextOptionsBuilder<TradeContext> _dbContextOptionsBuilder;

        public PortfolioStorage(ILogger<PortfolioStorage> logger, DbContextOptionsBuilder<TradeContext> dbContextOptionsBuilder)
        {
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task SaveAsync(List<Trade> trades)
        {
            await using var context = new TradeContext(_dbContextOptionsBuilder.Options);
            context.Trades.AddRange(trades);
            await context.SaveChangesAsync();
        }
    }
}
