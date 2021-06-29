using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioService: IAssetPortfolioService
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger, DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            var response = new GetBalancesResponse()
            {
                Balances = ctx.Balances.ToList()
            };
            return response;
        }

        public async Task<GetTradesResponse> GetTradesAsync()
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            var response = new GetTradesResponse()
            {
                Trades = ctx.Trades.ToList()
            };
            return response;
        }
    }
}
