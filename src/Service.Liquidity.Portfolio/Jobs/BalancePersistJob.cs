using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Services;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class BalancePersistJob
    {
        private readonly ILogger<BalancePersistJob> _logger;
        private readonly MyTaskTimer _timer;
        private readonly AssetPortfolioManager _assetPortfolioManager;
        private readonly IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> _settingsDataWriter;
        private readonly PortfolioMetricsInterceptor _portfolioMetricsInterceptor;

        public BalancePersistJob(ILogger<BalancePersistJob> logger,
            AssetPortfolioManager assetPortfolioManager,
            IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> settingsDataWriter,
            PortfolioMetricsInterceptor portfolioMetricsInterceptor)
        {
            _logger = logger;
            _assetPortfolioManager = assetPortfolioManager;
            _settingsDataWriter = settingsDataWriter;
            _portfolioMetricsInterceptor = portfolioMetricsInterceptor;
            _timer = new MyTaskTimer(nameof(BalancePersistJob), TimeSpan.FromSeconds(Program.Settings.UpdateNoSqlBalancesTimerInSeconds), _logger, DoTime);
            Console.WriteLine($"BalancePersistJob timer: {TimeSpan.FromSeconds(Program.Settings.UpdateNoSqlBalancesTimerInSeconds)}");
        }

        public async Task Start()
        {
            await _assetPortfolioManager.ReloadBalance(_settingsDataWriter);
            _timer.Start();
        }

        private async Task DoTime()
        {
            await SavePortfolio();
        }

        private async Task SavePortfolio()
        {
            await _assetPortfolioManager.UpdatePortfolio();

            var portfolioSnapshot = _assetPortfolioManager.GetPortfolioSnapshot();
            
            _portfolioMetricsInterceptor.SetPortfolioMetrics(portfolioSnapshot);
            
            await _settingsDataWriter.InsertOrReplaceAsync(AssetPortfolioBalanceNoSql.Create(portfolioSnapshot));
            
            await _assetPortfolioManager.ReloadBalance(_settingsDataWriter);
        }

        public async Task Stop()
        {
            _timer.Stop();
            Thread.Sleep(5000);
            await SavePortfolio();
        }
    }
}
