using System;
using System.Linq;
using Prometheus;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class PortfolioMetrics
    {
        private static readonly Gauge VolumeByAssetAndWallet = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_amount",
                "Volume of by asset and wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });
        
        private static readonly Gauge OpenPriceByAssetAndWallet = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_open_price",
                "Open price by asset and wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });
        
        private static readonly Gauge VolumeUsdByAssetAndWallet = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_usd_amount",
                "Volume in USD by asset and wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });
        
        private static readonly Gauge UnreleasedPnlByAssetAndWallet = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_unrealised_pnl",
                "Unreleased pnl by asset and wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });
        
        
        
        private static readonly Gauge VolumeByAsset = Metrics
            .CreateGauge("jet_portfolio_asset_amount",
                "Volume of by asset.",
                new GaugeConfiguration { LabelNames = new[] { "asset"} });
        
        private static readonly Gauge OpenPriceByAsset = Metrics
            .CreateGauge("jet_portfolio_asset_open_price",
                "Open price by asset.",
                new GaugeConfiguration { LabelNames = new[] { "asset"} });
        
        private static readonly Gauge VolumeUsdByAsset = Metrics
            .CreateGauge("jet_portfolio_asset_usd_amount",
                "Volume in USD by asset.",
                new GaugeConfiguration { LabelNames = new[] { "asset"} });
        
        private static readonly Gauge UnreleasedPnlByAsset = Metrics
            .CreateGauge("jet_portfolio_asset_unrealised_pnl",
                "Unreleased pnl by asset.",
                new GaugeConfiguration { LabelNames = new[] { "asset"} });
        
        
        private static readonly Gauge VolumeByWallet = Metrics
            .CreateGauge("jet_portfolio_wallet_amount",
                "Volume of by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "wallet"} });
        
        private static readonly Gauge OpenPriceByWallet = Metrics
            .CreateGauge("jet_portfolio_wallet_open_price",
                "Open price by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "wallet"} });
        
        private static readonly Gauge VolumeUsdByWallet = Metrics
            .CreateGauge("jet_portfolio_wallet_usd_amount",
                "Volume in USD by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "wallet"} });
        
        private static readonly Gauge UnreleasedPnlByWallet = Metrics
            .CreateGauge("jet_portfolio_wallet_unrealised_pnl",
                "Unreleased pnl by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "wallet"} });
        
        
        private static readonly Gauge VolumeUsdTotal = Metrics
            .CreateGauge("jet_portfolio_total_net",
                "Total volume in USD.",
                new GaugeConfiguration ());
        
        private static readonly Gauge UnreleasedPnlTotal = Metrics
            .CreateGauge("jet_portfolio_total_unrealised_pnl",
                "Total unreleased pnl.",
                new GaugeConfiguration());
        
        
        private static readonly Counter TradeCounter = Metrics
            .CreateCounter("jet_portfolio_trade_count",
                "Trade count of portfolio processed.",
                new CounterConfiguration() { LabelNames = new[] { "market", "wallet", "source"} });
        
        private static readonly Gauge TradeVolume = Metrics
            .CreateGauge("jet_portfolio_trade_volume",
                "Trade volume of portfolio processed.",
                new GaugeConfiguration { LabelNames = new[] { "market", "wallet", "source"} });
        
        private static readonly Gauge TradeReleasedPnl = Metrics
            .CreateGauge("jet_portfolio_total_releasedPnl",
                "Trade released pnl of portfolio processed.",
                new GaugeConfiguration { LabelNames = new[] { "market", "wallet", "source"} });


        private static readonly Counter ChangeBalanceCounter = Metrics
            .CreateCounter("jet_portfolio_manual_change_balance",
                "Change balance operation count.",
                new CounterConfiguration{ LabelNames = new []{"broker", "wallet", "asset"}});


        public void SetPortfolioMetrics(AssetPortfolio portfolio)
        {
            foreach (var balanceByAsset in portfolio.BalanceByAsset)
            {
                SetMetricsByAsset(balanceByAsset);

                foreach (var balanceByWallet in balanceByAsset.WalletBalances)
                {
                    SetMetricsByAssetAndWallet(balanceByAsset, balanceByWallet);
                }
            }

            foreach (var balanceByWallet in portfolio.BalanceByWallet)
            {
                SetMetricsByWallet(balanceByWallet);
            }
            
            SetMetricsByTotal(portfolio);
        }

        public void SetTradeMetrics(AssetPortfolioTrade trade)
        {
            TradeCounter
                .WithLabels(trade.AssociateSymbol, trade.WalletName, trade.Source)
                .Inc();
            
            TradeVolume
                .WithLabels(trade.AssociateSymbol, trade.WalletName, trade.Source)
                .Inc(Math.Abs(Convert.ToDouble(trade.BaseVolume)));

            TradeReleasedPnl
                .WithLabels(trade.AssociateSymbol, trade.WalletName, trade.Source)
                .Inc(Convert.ToDouble(trade.ReleasePnl.Sum(e => e.Pnl)));
        }

        public void SetChangeBalanceMetrics(ChangeBalanceHistory changeBalanceHistory)
        {
            ChangeBalanceCounter
                .WithLabels(changeBalanceHistory.BrokerId, changeBalanceHistory.WalletName, changeBalanceHistory.Asset)
                .Inc();
        }

        private void SetMetricsByTotal(AssetPortfolio portfolio)
        {
            var totalNetUsd = portfolio.BalanceByWallet.Sum(e => e.NetUsdVolume);
            var totalPnl = portfolio.BalanceByWallet.Sum(e => e.UnreleasedPnlUsd);
            
            VolumeUsdTotal.Set(Convert.ToDouble(totalNetUsd));
            UnreleasedPnlTotal.Set(Convert.ToDouble(totalPnl));
        }

        private void SetMetricsByWallet(NetBalanceByWallet balanceByWallet)
        {
            VolumeByWallet
                .WithLabels(balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.NetVolume));
                    
            OpenPriceByWallet
                .WithLabels(balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.OpenPrice));
                    
            VolumeUsdByWallet
                .WithLabels(balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.NetUsdVolume));
                    
            UnreleasedPnlByWallet
                .WithLabels(balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.UnreleasedPnlUsd));
        }

        private void SetMetricsByAsset(NetBalanceByAsset balanceByAsset)
        {
            VolumeByAsset
                .WithLabels(balanceByAsset.Asset)
                .Set(Convert.ToDouble(balanceByAsset.NetVolume));
                    
            OpenPriceByAsset
                .WithLabels(balanceByAsset.Asset)
                .Set(Convert.ToDouble(balanceByAsset.OpenPriceAvg));
                    
            VolumeUsdByAsset
                .WithLabels(balanceByAsset.Asset)
                .Set(Convert.ToDouble(balanceByAsset.NetUsdVolume));
                    
            UnreleasedPnlByAsset
                .WithLabels(balanceByAsset.Asset)
                .Set(Convert.ToDouble(balanceByAsset.UnrealisedPnl));
        }

        private void SetMetricsByAssetAndWallet(NetBalanceByAsset balanceByAsset, NetBalanceByWallet balanceByWallet)
        {
            VolumeByAssetAndWallet
                .WithLabels(balanceByAsset.Asset, balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.NetVolume));
                    
            OpenPriceByAssetAndWallet
                .WithLabels(balanceByAsset.Asset, balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.OpenPrice));
                    
            VolumeUsdByAssetAndWallet
                .WithLabels(balanceByAsset.Asset, balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.NetUsdVolume));
                    
            UnreleasedPnlByAssetAndWallet
                .WithLabels(balanceByAsset.Asset, balanceByWallet.WalletName)
                .Set(Convert.ToDouble(balanceByWallet.UnreleasedPnlUsd));
        }
    }
}
