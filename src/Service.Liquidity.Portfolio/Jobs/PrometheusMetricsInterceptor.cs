using System;
using Prometheus;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class PrometheusMetricsInterceptor
    {
        private static readonly Gauge AssetPortfolioVolume = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_amount",
                "Volume of asset by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });
        
        private static readonly Gauge AssetPortfolioOpenPrice = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_open_price",
                "Open price of asset by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });
        
        private static readonly Gauge AssetPortfolioVolumeUsd = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_usd_amount",
                "Volume in USD of asset by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });
        
        private static readonly Gauge AssetPortfolioUnreleasedPnl = Metrics
            .CreateGauge("jet_portfolio_asset_wallet_unrealised_pl",
                "Unreleased pnl asset by wallet.",
                new GaugeConfiguration { LabelNames = new[] { "asset", "wallet"} });

        public void SetMetrics(AssetPortfolio portfolio)
        {
            foreach (var balanceByAsset in portfolio.BalanceByAsset)
            {
                foreach (var balanceByWalletAndAsset in balanceByAsset.WalletBalances)
                {
                    AssetPortfolioVolume
                        .WithLabels(balanceByAsset.Asset, balanceByWalletAndAsset.WalletName)
                        .Set(Convert.ToDouble(balanceByWalletAndAsset.NetVolume));
                    
                    AssetPortfolioOpenPrice
                        .WithLabels(balanceByAsset.Asset, balanceByWalletAndAsset.WalletName)
                        .Set(Convert.ToDouble(balanceByWalletAndAsset.OpenPrice));
                    
                    AssetPortfolioVolumeUsd
                        .WithLabels(balanceByAsset.Asset, balanceByWalletAndAsset.WalletName)
                        .Set(Convert.ToDouble(balanceByWalletAndAsset.NetUsdVolume));
                    
                    AssetPortfolioUnreleasedPnl
                        .WithLabels(balanceByAsset.Asset, balanceByWalletAndAsset.WalletName)
                        .Set(Convert.ToDouble(balanceByWalletAndAsset.UnreleasedPnlUsd));
                }
            }
        }
    }
}
