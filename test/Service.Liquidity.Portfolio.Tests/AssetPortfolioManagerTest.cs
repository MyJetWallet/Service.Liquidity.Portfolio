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
        private AssetPortfolioMath _assetPortfolioMath;

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
            _assetPortfolioMath = new AssetPortfolioMath();

            _assetPortfolioManager = new AssetPortfolioManager(_loggerFactory.CreateLogger<AssetPortfolioManager>(),
                _noSqlDataReader,
                _indexPricesClient,
                _assetPortfolioMath);
        }

        [Test]
        public void Test1()
        {
            _assetPortfolioManager.ReloadBalance(null);

            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 1750}, {"USD", 1}};
            var pnl1 = ExecuteTrade("ETH", "BTC", 10, -0.5m);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 1750}, {"USD", 1}};
            var pnl2 = ExecuteTrade("ETH", "BTC", -10, 0.5m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");
        }
        
        [Test]
        public void Test1_1()
        {
            _assetPortfolioManager.ReloadBalance(null);

            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 1750}, {"USD", 1}};
            var pnl1 = ExecuteTrade("ETH", "BTC", 10, -0.5m);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 37000}, {"ETH", 1850}, {"USD", 1}};
            var pnl2 = ExecuteTrade("ETH", "BTC", -10, 0.5m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");
        }
        
        [Test]
        public void Test1_2()
        {
            _assetPortfolioManager.ReloadBalance(null);

            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 1750}, {"USD", 1}};
            var pnl1 = ExecuteTrade("ETH", "BTC", 12, -0.5m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
        }
        
        [Test]
        public void Test2()
        {
            _assetPortfolioManager.ReloadBalance(null);

            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 1750}, {"USD", 1}};
            var pnl1 = ExecuteTrade("ETH", "BTC", 10, -0.5m);
            
            var portfolio1 = _assetPortfolioManager.GetPortfolioSnapshot();
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 34313.7254901961M}, {"ETH", 1750}, {"USD", 1}};
            var pnl2 = ExecuteTrade("ETH", "BTC", -10, 0.51m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0.01m * _indexPricesClient.PriceMap["BTC"], pnl2, "Pnl 2");

            var portfolio2 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0.01m, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            Assert.AreEqual(0, portfolio2.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd));
            Assert.AreEqual(-pnl2, 
                portfolio2.BalanceByAsset
                .FirstOrDefault(e => e.Asset == AssetPortfolioManager.UsdAsset)?
                .WalletBalances
                .FirstOrDefault(e => e.WalletName == AssetPortfolioManager.PlWalletName)?
                .NetVolume);
        }
        
        [Test]
        public void Test3()
        {
            _assetPortfolioManager.ReloadBalance(null);

            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 35000}, {"ETH", 1750}, {"USD", 1}};
            var pnl1 = ExecuteTrade("ETH", "BTC", 10, -0.5m);
            
            var portfolio1 = _assetPortfolioManager.GetPortfolioSnapshot();
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 34000}, {"ETH", 1750}, {"USD", 1}};
            
            var snapshot = _assetPortfolioManager.GetPortfolioSnapshot();
            var netUsd = snapshot.BalanceByWallet.Sum(e => e.NetUsdVolume);
            var unrPnl = snapshot.BalanceByWallet.Sum(e => e.UnreleasedPnlUsd);
            
            var pnl2 = ExecuteTrade("ETH", "BTC", -10, 0.51m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            var secondPrice = Math.Abs(_indexPricesClient.PriceMap["ETH"] * -10 / 0.51m);
            Assert.AreEqual(0.01m * secondPrice, pnl2, "Pnl 2");

            var portfolio2 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0.01m, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(portfolio2.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd), portfolio2.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            
            Assert.AreEqual(-pnl2, 
                portfolio2.BalanceByAsset
                    .FirstOrDefault(e => e.Asset == AssetPortfolioManager.UsdAsset)?
                    .WalletBalances
                    .FirstOrDefault(e => e.WalletName == AssetPortfolioManager.PlWalletName)?
                    .NetVolume);
        }
        
        [Test]
        public void Test4()
        {
            _assetPortfolioManager.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl1 = ExecuteTrade("BTC", "EUR", 1, -25000);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl2 = ExecuteTrade("ETH", "CHF", -10, 37500);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl3 = ExecuteTrade("ETH", "BTC", 10, -1);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");
            Assert.AreEqual(0, pnl3, "Pnl 3");

            var portfolio2 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(-25000, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "EUR")?.NetVolume);
            Assert.AreEqual(37500, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "CHF")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "USD")?.NetVolume);
            
            Assert.AreEqual(0, portfolio2.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            Assert.AreEqual(0, portfolio2.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd));
            
            Assert.AreEqual(0, 
                portfolio2.BalanceByAsset
                    .FirstOrDefault(e => e.Asset == AssetPortfolioManager.UsdAsset)?
                    .WalletBalances
                    .FirstOrDefault(e => e.WalletName == AssetPortfolioManager.PlWalletName)?
                    .NetVolume);
        }
        
        [Test]
        public void Test4_1()
        {
            _assetPortfolioManager.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl1 = ExecuteTrade("BTC", "EUR", 1, -25000);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl2 = ExecuteTrade("ETH", "CHF", -10, 37500);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 29000}, {"ETH", 3100}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl3 = ExecuteTrade("ETH", "BTC", 10, -1);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");
            Assert.AreEqual(0, pnl3, "Pnl 3");

            var portfolio2 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(-25000, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "EUR")?.NetVolume);
            Assert.AreEqual(37500, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "CHF")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "USD")?.NetVolume);
            
            Assert.AreEqual(0, portfolio2.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            Assert.AreEqual(0, portfolio2.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd));
            
            Assert.AreEqual(0, 
                portfolio2.BalanceByAsset
                    .FirstOrDefault(e => e.Asset == AssetPortfolioManager.UsdAsset)?
                    .WalletBalances
                    .FirstOrDefault(e => e.WalletName == AssetPortfolioManager.PlWalletName)?
                    .NetVolume);
        }
        
        [Test]
        public void Test4_2()
        {
            _assetPortfolioManager.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl1 = ExecuteTrade("BTC", "EUR", 1, -25000);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl2 = ExecuteTrade("ETH", "CHF", -10, 37500);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 37500M}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            
            var snapshot = _assetPortfolioManager.GetPortfolioSnapshot();
            var netUsd = snapshot.BalanceByWallet.Sum(e => e.NetUsdVolume);
            var unrPnl = snapshot.BalanceByWallet.Sum(e => e.UnreleasedPnlUsd);
            
            var pnl3 = ExecuteTrade("ETH", "BTC", 10, -0.8m);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");
            Assert.AreEqual(6000, pnl3, "Pnl 3");

            var portfolio2 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0.2m, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(-25000, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "EUR")?.NetVolume);
            Assert.AreEqual(37500, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "CHF")?.NetVolume);
            Assert.AreEqual(-6000, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "USD")?.NetVolume);
            
            Assert.AreEqual(1500, portfolio2.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            Assert.AreEqual(1500, portfolio2.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd));
        }
        
        [Test]
        public void Test4_3()
        {
            _assetPortfolioManager.ReloadBalance(null);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl1 = ExecuteTrade("BTC", "EUR", 1, -25000);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 30000}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl2 = ExecuteTrade("ETH", "CHF", -10, 37500);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 37500M}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl3 = ExecuteTrade("ETH", "BTC", 10, -0.8m);
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 37500M}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl4 = ExecuteTrade("CHF", "EUR", -37500, 25000);
            
            Assert.AreEqual(0, pnl1, "Pnl 1");
            Assert.AreEqual(0, pnl2, "Pnl 2");
            Assert.AreEqual(6000, pnl3, "Pnl 3");
            Assert.AreEqual(0, pnl4, "Pnl 4");

            var portfolio2 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0.2, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "EUR")?.NetVolume);
            Assert.AreEqual(0, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "CHF")?.NetVolume);
            Assert.AreEqual(-6000, portfolio2.BalanceByAsset.FirstOrDefault(e => e.Asset == "USD")?.NetVolume);
            
            Assert.AreEqual(1500, portfolio2.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            Assert.AreEqual(1500, portfolio2.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd));
            
            _indexPricesClient.PriceMap = new Dictionary<string, decimal>() {{"BTC", 37500M}, {"ETH", 3000}, {"USD", 1}, {"EUR", 1.2m}, {"CHF", 0.8m}};
            var pnl5 = ExecuteTrade("BTC", "USD", -0.2m, 7500);
            
            Assert.AreEqual(1500, pnl5, "Pnl 5");
            
            var portfolio3 = _assetPortfolioManager.GetPortfolioSnapshot();
            Assert.AreEqual(0, portfolio3.BalanceByAsset.FirstOrDefault(e => e.Asset == "BTC")?.NetVolume);
            Assert.AreEqual(0, portfolio3.BalanceByAsset.FirstOrDefault(e => e.Asset == "ETH")?.NetVolume);
            Assert.AreEqual(0, portfolio3.BalanceByAsset.FirstOrDefault(e => e.Asset == "EUR")?.NetVolume);
            Assert.AreEqual(0, portfolio3.BalanceByAsset.FirstOrDefault(e => e.Asset == "CHF")?.NetVolume);
            Assert.AreEqual(0, portfolio3.BalanceByAsset.FirstOrDefault(e => e.Asset == "USD")?.NetVolume);
            
            Assert.AreEqual(0, portfolio3.BalanceByWallet.Sum(ee => ee.NetUsdVolume));
            Assert.AreEqual(0, portfolio3.BalanceByWallet.Sum(ee => ee.UnreleasedPnlUsd));
        }
        
        private decimal ExecuteTrade(string firstAsset, string secondAsset, decimal firstAmount, decimal secondAmount)
        {
            Console.WriteLine("Trade:");
            
            var balanceDifference = new List<AssetBalanceDifference>();

            var firstDiff = new AssetBalanceDifference("jetwallet", "LP", firstAsset,
                firstAmount, firstAmount * _indexPricesClient.PriceMap[firstAsset], _indexPricesClient.PriceMap[firstAsset]);
            
            var secondUsdPrice = Math.Abs(_indexPricesClient.PriceMap[firstAsset] * firstAmount / secondAmount);
            var secondUsdVolume = secondAmount * secondUsdPrice;
            
            var secondDiff = new AssetBalanceDifference("jetwallet", "LP", secondAsset, 
                secondAmount, secondUsdVolume, secondUsdPrice);
            
            balanceDifference.Add(firstDiff);
            balanceDifference.Add(secondDiff);
            
            _assetPortfolioManager.UpdateBalance(balanceDifference);

            var releasedPnl = _assetPortfolioManager.FixReleasedPnl();
            
            Console.WriteLine($"ReleasedPnl = {releasedPnl}");
            
            return releasedPnl;
        }
    }
}
