using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioService: IAssetPortfolioService
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            IAnotherAssetProjectionService anotherAssetProjectionService)
        {
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _anotherAssetProjectionService = anotherAssetProjectionService;
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            var response = new GetBalancesResponse();
            try
            {
                await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                response.SetBalances(ctx.Balances.ToList());

                const string projectionAsset = "USD";
                response.Balances.ForEach(async elem =>
                {
                    if (elem.Volume == 0)
                        return;
                    if (elem.Asset == projectionAsset)
                    {
                        elem.UsdProjection = elem.Volume;
                        return;
                    }
                    var usdProjectionEntity = await _anotherAssetProjectionService.GetProjectionAsync(
                        new GetProjectionRequest()
                        {
                            BrokerId = elem.BrokerId,
                            FromAsset = elem.Asset,
                            FromVolume = elem.Volume,
                            ToAsset = projectionAsset
                        });
                    
                    // if usdProjectionEntity.Success = false - set zero UsdProjection
                    elem.UsdProjection = usdProjectionEntity.ProjectionVolume;
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(JsonConvert.SerializeObject(exception));
            }
            return response;
        }

        public async Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request)
        {
            if (request.AssetBalance == null ||
                string.IsNullOrWhiteSpace(request.AssetBalance.BrokerId) ||
                string.IsNullOrWhiteSpace(request.AssetBalance.ClientId) ||
                string.IsNullOrWhiteSpace(request.AssetBalance.WalletId) ||
                string.IsNullOrWhiteSpace(request.AssetBalance.Asset))
            {
                _logger.LogError($"Bad request entity: {JsonConvert.SerializeObject(request)}");
                return new UpdateBalanceResponse() {Success = false, ErrorMessage = "Incorrect entity"};
            }
            
            if (request.BalanceDifference == 0)
            {
                const string message = "Balance difference is zero.";
                _logger.LogError(message);
                return new UpdateBalanceResponse() {Success = false, ErrorMessage = message};
            }

            try
            {
                await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                
                var newBalance = request.AssetBalance.GetDomainModel();
                newBalance.Volume = request.BalanceDifference;
                await ctx.UpdateBalancesAsync(new List<AssetBalance>() {newBalance});
            }
            catch (Exception exception)
            {
                _logger.LogError($"Update failed: {JsonConvert.SerializeObject(exception)}");
                return new UpdateBalanceResponse() {Success = false, ErrorMessage = exception.Message};
            }

            return new UpdateBalanceResponse() {Success = true};
        }

        public async Task<GetTradesResponse> GetTradesAsync(GetTradesRequest request)
        {
           
            var response = new GetTradesResponse();
            try
            {
                await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                List<Trade> trades;
                
                if (request.LastId != 0)
                {
                    trades = ctx.Trades
                        .Where(trade => trade.Id < request.LastId)
                        .OrderByDescending(trade => trade.Id)
                        .Take(request.BatchSize)
                        .ToList();
                }
                else 
                {
                    trades = ctx.Trades
                        .OrderByDescending(trade => trade.Id)
                        .Take(request.BatchSize)
                        .ToList();
                }

                long idForNextQuery = 0;
                if (trades.Any())
                {
                    idForNextQuery = trades.Select(elem => elem.Id).Min();
                }

                response.Success = true;
                response.Trades = trades;
                response.IdForNextQuery = idForNextQuery;
            } 
            catch (Exception exception)
            {
                _logger.LogError(JsonConvert.SerializeObject(exception));
                
                response.Success = false;
                response.ErrorMessage = exception.Message;
            }

            return response;
        }

        public async Task<CreateTradeManualResponse> CreateManualTradeAsync(CreateTradeManualRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BrokerId) ||
                string.IsNullOrWhiteSpace(request.ClientId) ||
                string.IsNullOrWhiteSpace(request.WalletId) ||
                string.IsNullOrWhiteSpace(request.Symbol) ||
                request.Price == 0 ||
                request.BaseVolume == 0 ||
                request.QuoteVolume == 0 ||
                (request.BaseVolume > 0 && request.QuoteVolume > 0) ||
                (request.BaseVolume < 0 && request.QuoteVolume < 0))
            {
                _logger.LogError($"Bad request entity: {JsonConvert.SerializeObject(request)}");
                return new CreateTradeManualResponse() {Success = false, ErrorMessage = "Incorrect entity"};
            }

            var trade = new Trade(request.BrokerId, request.ClientId, request.WalletId,
                request.Symbol, request.Price, request.BaseVolume,
                request.QuoteVolume, "manual");
            try
            {
                await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                await ctx.SaveTradesAsync(new List<Trade>() {trade});
            }
            catch (Exception exception)
            {
                _logger.LogError($"Creating failed: {JsonConvert.SerializeObject(exception)}");
                return new CreateTradeManualResponse() {Success = false, ErrorMessage = exception.Message};
            }

            return new CreateTradeManualResponse() {Success = true, Trade = trade};
        }
    }
}
