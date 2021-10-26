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

        public FeeShareHandler(ILogger<AssetPortfolioService> logger, ITradeHandler tradeHandler, IIndexPricesClient indexPricesClient, BalanceHandler portfolioManager, ISubscriber<IReadOnlyList<FeeShareEntity>> subscriber)
        {
            _logger = logger;
            _tradeHandler = tradeHandler;
            _indexPricesClient = indexPricesClient;
            _portfolioManager = portfolioManager;
            subscriber.Subscribe(HandleEvents);
        }

        private async ValueTask HandleEvents(IReadOnlyList<FeeShareEntity> events)
        {
            foreach (var feeShareEvent in events) 
                await ReportFeeShare(feeShareEvent);
        }

        private async Task ReportFeeShare(FeeShareEntity entity)
        {
            try
            {
                var feeShareSettlement = new FeeShareSettlement()
                {
                    BrokerId = entity.BrokerId,
                    WalletFrom = entity.ConverterWalletId,
                    WalletTo = entity.FeeShareWalletId,
                    Asset = entity.FeeAsset,
                    VolumeFrom = entity.FeeShareAmount,
                    VolumeTo = entity.FeeShareAmountInUsd,
                    Comment = $"FeeShareSettlement:{entity.OperationId}",
                    ReferrerClientId = entity.ReferrerClientId,
                    SettlementDate = DateTime.UtcNow
                };
                
                var (fromIndexPrice, fromUsdVolume) =
                    _indexPricesClient.GetIndexPriceByAssetVolumeAsync(entity.FeeAsset, entity.FeeShareAmount);

                var fromDiff = new AssetBalanceDifference(entity.BrokerId, 
                    entity.ConverterWalletId,
                    entity.FeeAsset, 
                    entity.FeeShareAmount, 
                    entity.FeeShareAmountInUsd,
                    fromIndexPrice.UsdPrice);
                
                var toDiff = new AssetBalanceDifference(entity.BrokerId, 
                    Program.Settings.FeeShareWalletId,
                    "USD", 
                    entity.FeeShareAmountInUsd, 
                    entity.FeeShareAmountInUsd,
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
