using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Service.Liquidity.Portfolio.Tests
{
    public class OpenPriceTester : AssetPortfolioManagerTest
    {
        [Test]
        public void Test5()
        {
            BalanceHandler.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"USD", 1}};
            var pnl1 = ExecuteTrade("BTC", "USD", 1, -30000, "LP");
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 31000}, {"USD", 1}};
            var pnl2 = ExecuteTrade("BTC", "USD", -0.5m, 15500, "Binance");
            
            var portfolio = BalanceHandler.GetPortfolioSnapshot();
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(500, pnl2, "Pnl 2");
        }
        [Test]
        public void Test5_1() // релиз PNL
        {
            BalanceHandler.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"USD", 1}};
            var pnl1 = ExecuteTrade("BTC", "USD", 1, -30000, "LP");
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 31000}, {"USD", 1}};
            var pnl2 = ExecuteTrade("BTC", "USD", -1, 31000, "Binance");
            
            var portfolio = BalanceHandler.GetPortfolioSnapshot();
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(1000, pnl2, "Pnl 2");
        }
        
        [Test]
        public void Test5_2() // положительная доливка
        {
            BalanceHandler.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"USD", 1}};
            var pnl1 = ExecuteTrade("BTC", "USD", 1, -30000, "LP");
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 31000}, {"USD", 1}};
            var pnl2 = ExecuteTrade("BTC", "USD", 1, -31000, "Binance");
            
            var portfolio = BalanceHandler.GetPortfolioSnapshot();
            
            Assert.AreEqual(30500, portfolio.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.OpenPriceAvg, "Pnl 1");
        }
        [Test]
        public void Test5_3() // отрицательная доливка
        {
            BalanceHandler.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"USD", 1}};
            var pnl1 = ExecuteTrade("BTC", "USD", -1, 30000, "LP");
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 31000}, {"USD", 1}};
            var pnl2 = ExecuteTrade("BTC", "USD", -1, 31000, "Binance");
            
            var portfolio = BalanceHandler.GetPortfolioSnapshot();
            
            Assert.AreEqual(30500, portfolio.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.OpenPriceAvg, "Pnl 1");
        }
    }
}
