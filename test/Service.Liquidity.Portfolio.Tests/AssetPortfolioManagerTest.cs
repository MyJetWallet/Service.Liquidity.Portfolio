using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Service.AssetsDictionary.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Services;

namespace Service.Liquidity.Portfolio.Tests
{
    public class AssetPortfolioManagerTest
    {
        private static ILoggerFactory _loggerFactory;
        private MyNoSqlServerDataReaderMock _noSqlDataReader;
        private IndexPricesClientMock _indexPricesClient;
        private AssetPortfolioManager _assetPortfolioManager;
        
        [SetUp]
        public void SetUpTests()
        {
            _loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));
            _indexPricesClient = new IndexPricesClientMock();
            _noSqlDataReader = new MyNoSqlServerDataReaderMock();

            _assetPortfolioManager = new AssetPortfolioManager(_loggerFactory.CreateLogger<AssetPortfolioManager>(),
                _noSqlDataReader,
                _indexPricesClient);
        }

        [Test]
        public void Test1()
        {
            _assetPortfolioManager.ReloadBalance(null);

            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 3700}, {"USD", 1}};
            var pnl1 = ExecuteTrade("ETH", "BTC", 10, -0.5m);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 3700}, {"USD", 1}};
            var pnl2 = ExecuteTrade("ETH", "BTC", -10, 0.5m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");
        }
        
        [Test]
        public void Test2()
        {
            _assetPortfolioManager.ReloadBalance(null);

            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 3700}, {"USD", 1}};
            var pnl1 = ExecuteTrade("ETH", "BTC", 10, -0.5m);
            
            var portfolio1 = _assetPortfolioManager.GetPortfolioSnapshot();
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 3700}, {"USD", 1}};
            var pnl2 = ExecuteTrade("ETH", "BTC", -10, 0.51m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");

            var portfolio2 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0.01m, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(0.01 * (double) _indexPricesClient.PriceMap["BTC"], portfolio2.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            Assert.AreEqual(0, portfolio2.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd));
        }

        private decimal ExecuteTrade(string firstAsset, string secondAsset, decimal firstAmount, decimal secondAmount)
        {
            Console.WriteLine("Trade:");
            
            var balanceDifference = new List<AssetBalanceDifference>();
            var releasedPnl = 0m;

            var firstDiff = new AssetBalanceDifference("jetwallet", "LP", firstAsset, firstAmount, firstAmount * _indexPricesClient.PriceMap["ETH"], _indexPricesClient.PriceMap["ETH"]);
            var secondDiff = new AssetBalanceDifference("jetwallet", "LP", secondAsset, secondAmount, secondAmount * _indexPricesClient.PriceMap["BTC"], _indexPricesClient.PriceMap["BTC"]);
            
            balanceDifference.Add(firstDiff);
            balanceDifference.Add(secondDiff);
            
            var response = _assetPortfolioManager.UpdateBalance(balanceDifference);
            foreach (var e in response)
            {
                Console.WriteLine($"Asset:{e.Key} -- Pnl:{e.Value}");
                releasedPnl += e.Value;
            }
            return releasedPnl;
        }
    }
}
