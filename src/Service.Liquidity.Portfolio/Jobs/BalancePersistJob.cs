using System;
using System.Linq;
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
        private readonly BalanceHandler _balanceHandler;
        private readonly IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> _settingsDataWriter;
        private readonly PortfolioMetrics _portfolioMetrics;

        public BalancePersistJob(ILogger<BalancePersistJob> logger,
            BalanceHandler balanceHandler,
            IMyNoSqlServerDataWriter<AssetPortfolioBalanceNoSql> settingsDataWriter,
            PortfolioMetrics portfolioMetrics)
        {
            _logger = logger;
            _balanceHandler = balanceHandler;
            _settingsDataWriter = settingsDataWriter;
            _portfolioMetrics = portfolioMetrics;
            _timer = new MyTaskTimer(nameof(BalancePersistJob), TimeSpan.FromSeconds(Program.Settings.UpdateNoSqlBalancesTimerInSeconds), _logger, DoTime);
            Console.WriteLine($"BalancePersistJob timer: {TimeSpan.FromSeconds(Program.Settings.UpdateNoSqlBalancesTimerInSeconds)}");
        }

        public async Task Start()
        {
            var nosqlBalance = (await _settingsDataWriter.GetAsync()).FirstOrDefault();
            await _balanceHandler.ReloadBalance(nosqlBalance?.Balance);
            _timer.Start();
        }

        private async Task DoTime()
        {
            await SavePortfolio();
        }

        private async Task SavePortfolio()
        {
            var portfolioSnapshot = _balanceHandler.GetPortfolioSnapshot();
            
            _portfolioMetrics.SetPortfolioMetrics(portfolioSnapshot);
            
            await _settingsDataWriter.InsertOrReplaceAsync(AssetPortfolioBalanceNoSql.Create(portfolioSnapshot));
        }

        public async Task Stop()
        {
            _timer.Stop();
            Thread.Sleep(5000);
            await SavePortfolio();
        }
    }
}
