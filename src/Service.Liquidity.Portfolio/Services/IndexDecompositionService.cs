using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Service.AssetsDictionary.Client;
using Service.IndexPrices.Client;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public interface IIndexDecompositionService
    {
        List<AssetBalanceDifference> CheckIndexDecomposition(AssetBalanceDifference difference);
    }

    public class IndexDecompositionService : IIndexDecompositionService
    {
        private readonly IIndexPricesClient _indexPrices;
        private readonly IIndexAssetDictionaryClient _indexAssetDictionaryClient;
        private readonly ILogger<IndexDecompositionService> _logger;

        public IndexDecompositionService(
            IIndexPricesClient indexPrices, 
            IIndexAssetDictionaryClient indexAssetDictionaryClient,
            ILogger<IndexDecompositionService> logger)
        {
            _indexPrices = indexPrices;
            _indexAssetDictionaryClient = indexAssetDictionaryClient;
            _logger = logger;
        }

        public List<AssetBalanceDifference> CheckIndexDecomposition(AssetBalanceDifference difference)
        {
            var indexes = _indexAssetDictionaryClient.GetAll();
            var index = indexes.FirstOrDefault(e => e.Broker == difference.BrokerId && e.Symbol == difference.Asset);

            if (index == null)
            {
                return new List<AssetBalanceDifference>() {difference};
            }

            var result = new List<AssetBalanceDifference>();
            
            foreach (var basketAsset in index.Basket)
            {
                var volume = basketAsset.Volume * difference.Volume;
                var (price, usdVolume) = _indexPrices.GetIndexPriceByAssetVolumeAsync(basketAsset.Symbol, volume);

                if (price == null || price.UsdPrice == 0m)
                {
                    throw new Exception(
                        $"Cannot decompose index {difference.Asset}. Usd price for {basketAsset.Symbol} does  ot found");
                }
                
                var diff = new AssetBalanceDifference(
                    index.Broker,
                    difference.WalletName,
                    basketAsset.Symbol,
                    volume,
                    usdVolume,
                    price.UsdPrice);
                
                result.Add(diff);
            }

            return result;
        }
    }
}
