using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services
{
    public class TradeCacheStorage
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly List<TradeCache> _tradeCache = new List<TradeCache>();

        private int _cacheLimit = 100;

        public TradeCacheStorage(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            SetTradeCacheOnInit().GetAwaiter().GetResult();
        }

        public void SaveInCache(AssetPortfolioTrade assetPortfolioTrade)
        {
            lock (_tradeCache)
            {


                if (_tradeCache.Any())
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
                return _tradeCache.FirstOrDefault(elem => elem.TradeId == id);
            }
        }

        private async Task SetTradeCacheOnInit()
        {
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            var trades = ctx.Trades
                .OrderByDescending(trade => trade.Id)
                .Take(100)
                .ToList();

            lock (_tradeCache)
            {
                foreach (var trade in trades.OrderByDescending(trade => trade.Id))
                {
                    var cacheEntity = new TradeCache(trade.TradeId, trade.ErrorMessage);
                    _tradeCache.Add(cacheEntity);
                }
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
