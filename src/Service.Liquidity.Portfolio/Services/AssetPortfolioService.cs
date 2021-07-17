﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioService: IAssetPortfolioService
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        private readonly IPortfolioHandler _portfolioHandler;
        
        private readonly IAssetPortfolioBalanceStorage _portfolioBalanceStorage;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger,
            IPortfolioHandler portfolioHandler,
            IAssetPortfolioBalanceStorage portfolioBalanceStorage)
        {
            _logger = logger;
            _portfolioHandler = portfolioHandler;
            _portfolioBalanceStorage = portfolioBalanceStorage;
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
                    Volume = request.BalanceDifference,
                    WalletName = request.WalletName
                };
                _portfolioBalanceStorage.UpdateBalance(new List<AssetBalance>() {newBalance});
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
        }
}
