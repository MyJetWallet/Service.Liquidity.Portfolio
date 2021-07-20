using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.IndexPrices.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using IPortfolioHandler = Service.Liquidity.Portfolio.Domain.Services.IPortfolioHandler;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioService : IAssetPortfolioService
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly IIndexPricesClient _indexPricesClient;

        private readonly AssetPortfolioManager _portfolioManager;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger,
            IPortfolioHandler portfolioHandler,
            AssetPortfolioManager portfolioManager,
            IIndexPricesClient indexPricesClient)
        {
            _logger = logger;
            _portfolioHandler = portfolioHandler;
            _portfolioManager = portfolioManager;
            _indexPricesClient = indexPricesClient;
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
                var (indexPrice, usdVolume) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(request.Asset, request.BalanceDifference);
                
                var updateDate = DateTime.UtcNow;
                var newBalance = new AssetBalanceDifference(request.BrokerId, 
                    request.WalletName,
                    request.Asset, 
                    request.BalanceDifference, 
                    usdVolume,
                    indexPrice.UsdPrice);
                var pnlByAsset = _portfolioManager.UpdateBalance(new List<AssetBalanceDifference>() {newBalance});
                
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
