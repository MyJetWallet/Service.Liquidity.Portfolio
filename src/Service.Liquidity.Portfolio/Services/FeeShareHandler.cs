using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.FeeShareEngine.Domain.Models.Models;
using Service.IndexPrices.Client;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Services.Grpc;

namespace Service.Liquidity.Portfolio.Services
{
    public class FeeShareHandler
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        private readonly ITradeHandler _tradeHandler;
        private readonly IIndexPricesClient _indexPricesClient;
        private readonly BalanceHandler _portfolioManager;
        private readonly FeeShareOperationCache _feeShareOperationCache;
        public FeeShareHandler(ILogger<AssetPortfolioService> logger, ITradeHandler tradeHandler, IIndexPricesClient indexPricesClient, BalanceHandler portfolioManager, ISubscriber<FeeShareEntity> subscriber, FeeShareOperationCache feeShareOperationCache)
        {
            _logger = logger;
            _tradeHandler = tradeHandler;
            _indexPricesClient = indexPricesClient;
            _portfolioManager = portfolioManager;
            _feeShareOperationCache = feeShareOperationCache;
            subscriber.Subscribe(HandleEvents);
        }

        private async ValueTask HandleEvents(FeeShareEntity feeShareEvent)
        {
                await ReportFeeShare(feeShareEvent);
        }

        private async Task ReportFeeShare(FeeShareEntity entity)
        {
            try
            {
                if(await _feeShareOperationCache.WasRecorded(entity.OperationId))
                    return;
                
                var feeShareSettlement = new FeeShareSettlement()
                {
                    BrokerId = entity.BrokerId,
                    WalletFrom = entity.ConverterWalletId,
                    WalletTo = entity.FeeShareWalletId,
                    Asset = entity.FeeAsset,
                    VolumeFrom = entity.FeeShareAmountInTargetAsset,
                    VolumeTo = entity.FeeShareAmountInTargetAsset,
                    Comment = $"FeeShareSettlement:{entity.OperationId}",
                    ReferrerClientId = entity.ReferrerClientId,
                    SettlementDate = DateTime.UtcNow,
                    OperationId = entity.OperationId
                };
                
                var (fromIndexPrice, shareVolumeInUsd) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(entity.FeeShareAsset, entity.FeeShareAmountInTargetAsset);

                var fromDiff = new AssetBalanceDifference(entity.BrokerId, 
                    entity.ConverterWalletId,
                    entity.FeeShareAsset, 
                    entity.FeeShareAmountInTargetAsset, 
                    shareVolumeInUsd,
                    fromIndexPrice.UsdPrice);

                var toDiff = new AssetBalanceDifference(entity.BrokerId, 
                    entity.FeeShareWalletId,
                    entity.FeeShareAsset, 
                    entity.FeeShareAmountInTargetAsset, 
                    shareVolumeInUsd,
                    1);

                _portfolioManager.UpdateBalance(new List<AssetBalanceDifference>() {fromDiff, toDiff}, false);
                feeShareSettlement.ReleasedPnl = _portfolioManager.FixReleasedPnl();
                
                await _tradeHandler.SaveFeeShareSettlementHistoryAsync(feeShareSettlement);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Update failed: {JsonConvert.SerializeObject(exception)}");
            }
        }
    }
}
