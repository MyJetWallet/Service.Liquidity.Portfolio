using System.Collections.Generic;
using System.Linq;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class TradeCacheStorage
    {
        private readonly List<TradeCache> _tradeCache = new List<TradeCache>();

        private int _cacheLimit = 100;

        public void SaveInCache(AssetPortfolioTrade assetPortfolioTrade)
        {
            lock (_tradeCache)
            {
                if (_tradeCache != null && _tradeCache.Count >= _cacheLimit)
                {
                    _tradeCache.RemoveAt(0);
                }

                var cacheEntity = new TradeCache(assetPortfolioTrade.TradeId, assetPortfolioTrade.ErrorMessage);
                _tradeCache.Add(cacheEntity);
            }
        }

        public TradeCache GetFromCache(string id)
        {
            lock (_tradeCache)
            {
                return _tradeCache?.FirstOrDefault(elem => elem.TradeId == id);
            }
        }
    }

    public class TradeCache
    {
        public TradeCache(string tradeId, string errorMessage)
        {
            TradeId = tradeId;
            ErrorMessage = errorMessage;
        }

        public string TradeId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
