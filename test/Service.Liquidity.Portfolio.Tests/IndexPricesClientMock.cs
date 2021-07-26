using System;
using System.Collections.Generic;
using System.Linq;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;

namespace Service.Liquidity.Portfolio.Tests
{
    public class IndexPricesClientMock : IIndexPricesClient
    {
        public Dictionary<string, decimal> PriceMap = new Dictionary<string, decimal>();
        
        public IndexPrice GetIndexPriceByAssetAsync(string asset)
        {
            if (PriceMap.TryGetValue(asset, out var price))
            {
                return new IndexPrice() {Asset = asset, UsdPrice = price};
            }
            throw new Exception($"Price for {asset} not found");
        }

        public (IndexPrice, decimal) GetIndexPriceByAssetVolumeAsync(string asset, decimal volume)
        {
            var price = GetIndexPriceByAssetAsync(asset);
            var resultVolume = volume * price.UsdPrice;

            return (price, resultVolume);
        }

        public List<IndexPrice> GetIndexPricesAsync()
        {
            return PriceMap.Select(e => new IndexPrice() {Asset = e.Key, UsdPrice = e.Value}).ToList();
        }
    }
}
