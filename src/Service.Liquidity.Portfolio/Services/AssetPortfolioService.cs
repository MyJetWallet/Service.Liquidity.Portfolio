using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            var response = new GetBalancesResponse();
            try
            {
                await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                response.Balances = ctx.Balances.ToList();
            }
            catch (Exception exception)
            {
                _logger.LogError(JsonConvert.SerializeObject(exception));
            }

            return response;
        }

        public async Task<GetTradesResponse> GetTradesAsync(GetTradesRequest request)
        {
            var lastDate = request.LastDate == new DateTime(0001, 01, 01)
                ? DateTime.UtcNow
                : request.LastDate;
            
            var response = new GetTradesResponse();
            try
            {
                await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                var trades = ctx.Trades
                    .Where(trade => trade.DateTime < lastDate)
                    .OrderByDescending(trade => trade.DateTime)
                    .Take(request.BatchSize)
                    .ToList();

                var dateForNextQuery = DateTime.UtcNow;
                trades.ForEach(trade =>
                {
                    if (trade.DateTime < dateForNextQuery)
                    {
                        dateForNextQuery = trade.DateTime;
                    }
                });

                response.Success = true;
                response.Trades = trades;
                response.DateForNextQuery = dateForNextQuery;
            } 
            catch (Exception exception)
            {
                _logger.LogError(JsonConvert.SerializeObject(exception));
                
                response.Success = false;
                response.ErrorMessage = exception.Message;
            }

            return response;
        }
    }
}
