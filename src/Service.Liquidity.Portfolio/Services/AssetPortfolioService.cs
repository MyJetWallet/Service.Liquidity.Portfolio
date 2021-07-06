using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
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
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;
        private readonly IPortfolioHandler _portfolioHandler;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            IPortfolioHandler portfolioHandler)
        {
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _portfolioHandler = portfolioHandler;
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            var response = new GetBalancesResponse();
            try
            {
                response.SetBalances(_portfolioHandler.GetBalancesSnapshot());

                const string projectionAsset = "USD";
                response.Balances.ForEach(async elem =>
                {
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

        public async Task<GetChangeBalanceHistoryResponse> GetChangeBalanceHistoryAsync()
        {
            var response = new GetChangeBalanceHistoryResponse();
            try
            {
                response.Histories = await _portfolioHandler.GetHistories();
                response.Success = true;
            }
            catch (Exception exception)
            {
                response.Success = false;
                response.ErrorText = exception.Message;
            }

            return response;
        }

        public async Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BrokerId) ||
                string.IsNullOrWhiteSpace(request.WalletName) ||
                string.IsNullOrWhiteSpace(request.Asset) ||
                string.IsNullOrWhiteSpace(request.Comment) ||
                string.IsNullOrWhiteSpace(request.User))
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
                var updateDate = DateTime.UtcNow;
                var newBalance = new AssetBalance()
                {
                    BrokerId = request.BrokerId,
                    Asset = request.Asset,
                    UpdateDate = updateDate,
                    Volume = request.BalanceDifference,
                    WalletName = request.WalletName
                };
                _portfolioHandler.UpdateBalance(new List<AssetBalance>() {newBalance});
                await _portfolioHandler.SaveChangeBalanceHistoryAsync(new ChangeBalanceHistory()
                {
                    Asset = request.Asset,
                    BrokerId = request.BrokerId,
                    Comment = request.Comment,
                    UpdateDate = updateDate,
                    User = request.User,
                    VolumeDifference = request.BalanceDifference,
                    WalletName = request.WalletName
                });
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
                var trades = await _portfolioHandler.GetTrades(request.LastId, request.BatchSize);

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
            
            using var activity = MyTelemetry.StartActivity("CreateManualTradeAsync");

            request.AddToActivityAsJsonTag("CreateTradeManualRequest");
            
            _logger.LogInformation($"CreateManualTradeAsync receive request: {JsonConvert.SerializeObject(request)}");
            
            if (string.IsNullOrWhiteSpace(request.BrokerId) ||
                string.IsNullOrWhiteSpace(request.WalletName) ||
                string.IsNullOrWhiteSpace(request.Symbol) ||
                string.IsNullOrWhiteSpace(request.Comment) ||
                string.IsNullOrWhiteSpace(request.User) ||
                request.Price == 0 ||
                request.BaseVolume == 0 ||
                request.QuoteVolume == 0 ||
                (request.BaseVolume > 0 && request.QuoteVolume > 0) ||
                (request.BaseVolume < 0 && request.QuoteVolume < 0))
            {
                _logger.LogError($"Bad request entity: {JsonConvert.SerializeObject(request)}");
                return new CreateTradeManualResponse() {Success = false, ErrorMessage = "Incorrect entity"};
            }

            var trade = new Trade(request.BrokerId, request.WalletName,
                request.Symbol, request.Price, request.BaseVolume,
                request.QuoteVolume, request.Comment, request.User, "manual");
            try
            {
                await _portfolioHandler.HandleTradesAsync(new List<Trade>() {trade});
            }
            catch (Exception exception)
            {
                _logger.LogError($"Creating failed: {JsonConvert.SerializeObject(exception)}");
                return new CreateTradeManualResponse() {Success = false, ErrorMessage = exception.Message};
            }

            var response = new CreateTradeManualResponse() {Success = true, Trade = trade};
            
            _logger.LogInformation($"CreateManualTradeAsync return reponse: {JsonConvert.SerializeObject(response)}");
            return response;
        }
    }
}
