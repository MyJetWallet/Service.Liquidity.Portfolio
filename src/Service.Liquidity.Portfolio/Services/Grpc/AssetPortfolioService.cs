using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.IndexPrices.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Liquidity.Portfolio.Services.Grpc
{
    public class AssetPortfolioService : IAssetPortfolioService
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        private readonly ITradeHandler _tradeHandler;
        private readonly IIndexPricesClient _indexPricesClient;
        private readonly BalanceHandler _portfolioManager;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger,
            ITradeHandler tradeHandler,
            BalanceHandler portfolioManager,
            IIndexPricesClient indexPricesClient)
        {
            _logger = logger;
            _tradeHandler = tradeHandler;
            _portfolioManager = portfolioManager;
            _indexPricesClient = indexPricesClient;
        }

        public async Task<SetBalanceResponse> SetBalance(SetBalanceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BrokerId) ||
                string.IsNullOrWhiteSpace(request.WalletName) ||
                string.IsNullOrWhiteSpace(request.Asset) ||
                string.IsNullOrWhiteSpace(request.Comment) ||
                string.IsNullOrWhiteSpace(request.User))
            {
                _logger.LogError($"Bad request entity: {JsonConvert.SerializeObject(request)}");
                return new SetBalanceResponse() {Success = false, ErrorMessage = "Incorrect entity"};
            }
            try
            {
                var (indexPrice, usdVolume) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(request.Asset, request.NewBalance);
                
                var updateDate = DateTime.UtcNow;
                var newBalance = new AssetBalanceDifference(request.BrokerId, 
                    request.WalletName,
                    request.Asset, 
                    request.NewBalance, 
                    usdVolume,
                    indexPrice.UsdPrice);
                
                var balanceBeforeUpdate = _portfolioManager
                    .Portfolio?
                    .BalanceByAsset?
                    .FirstOrDefault(elem => elem.Asset == request.Asset)?
                    .WalletBalances?
                    .FirstOrDefault(elem => elem.WalletName == request.WalletName)?
                    .Volume;
                
                _portfolioManager.UpdateBalance(new List<AssetBalanceDifference>() {newBalance}, true);

                await _tradeHandler.SaveChangeBalanceHistoryAsync(new ChangeBalanceHistory()
                {
                    Asset = request.Asset,
                    BrokerId = request.BrokerId,
                    Comment = request.Comment,
                    UpdateDate = updateDate,
                    User = request.User,
                    VolumeDifference = request.NewBalance,
                    WalletName = request.WalletName,
                    BalanceBeforeUpdate = balanceBeforeUpdate ?? 0m
                });
            }
            catch (Exception exception)
            {
                _logger.LogError($"Update failed: {JsonConvert.SerializeObject(exception)}");
                return new SetBalanceResponse() {Success = false, ErrorMessage = exception.Message};
            }
            return new SetBalanceResponse() {Success = true};
        }

        public async Task<ReportSettlementResponse> ReportSettlement(ReportSettlementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BrokerId) ||
                string.IsNullOrWhiteSpace(request.WalletFrom) ||
                string.IsNullOrWhiteSpace(request.WalletTo) ||
                string.IsNullOrWhiteSpace(request.Asset) ||
                string.IsNullOrWhiteSpace(request.Comment) ||
                string.IsNullOrWhiteSpace(request.User) ||
                request.VolumeFrom == 0 ||
                request.VolumeTo == 0)
            {
                _logger.LogError($"Bad request entity: {JsonConvert.SerializeObject(request)}");
                return new ReportSettlementResponse() {Success = false, ErrorMessage = "Incorrect entity"};
            }
            try
            {
                var manualSettlement = new ManualSettlement()
                {
                    BrokerId = request.BrokerId,
                    WalletFrom = request.WalletFrom,
                    WalletTo = request.WalletTo,
                    Asset = request.Asset,
                    VolumeFrom = request.VolumeFrom,
                    VolumeTo = request.VolumeTo,
                    Comment = request.Comment,
                    User = request.User,
                    SettlementDate = DateTime.UtcNow
                };
                
                var (fromIndexPrice, fromUsdVolume) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(manualSettlement.Asset, manualSettlement.VolumeFrom);

                var fromDiff = new AssetBalanceDifference(manualSettlement.BrokerId, 
                    manualSettlement.WalletFrom,
                    manualSettlement.Asset, 
                    manualSettlement.VolumeFrom, 
                    fromUsdVolume,
                    fromIndexPrice.UsdPrice);
            
                var (toIndexPrice, toUsdVolume) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(manualSettlement.Asset, manualSettlement.VolumeTo);
            
                var toDiff = new AssetBalanceDifference(manualSettlement.BrokerId, 
                    manualSettlement.WalletTo,
                    manualSettlement.Asset, 
                    manualSettlement.VolumeTo, 
                    toUsdVolume,
                    toIndexPrice.UsdPrice);

                _portfolioManager.UpdateBalance(new List<AssetBalanceDifference>() {fromDiff, toDiff}, false);
                manualSettlement.ReleasedPnl = _portfolioManager.FixReleasedPnl();
                
                await _tradeHandler.SaveManualSettlementHistoryAsync(manualSettlement);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Update failed: {JsonConvert.SerializeObject(exception)}");
                return new ReportSettlementResponse() {Success = false, ErrorMessage = exception.Message};
            }
            return new ReportSettlementResponse() {Success = true};
        }
    }
}
