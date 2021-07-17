using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class BalancePersistJob
    {
        private readonly ILogger<BalancePersistJob> _logger;
        private readonly MyTaskTimer _timer;
        private readonly IAssetPortfolioBalanceStorage _assetPortfolioBalance;

        public BalancePersistJob(ILogger<BalancePersistJob> logger,
            IAssetPortfolioBalanceStorage assetPortfolioBalance)
        {
            _logger = logger;
            _assetPortfolioBalance = assetPortfolioBalance;
            _timer = new MyTaskTimer(nameof(BalancePersistJob), TimeSpan.FromSeconds(Program.Settings.UpdateNoSqlBalancesTimerInSeconds), _logger, DoTime);
            Console.WriteLine($"BalancePersistJob timer: {TimeSpan.FromSeconds(Program.Settings.UpdateNoSqlBalancesTimerInSeconds)}");
        }

        public void Start()
        {
            _timer.Start();
        }

        private async Task DoTime()
        {
            await _assetPortfolioBalance.SavePortfolioToNoSql();
        }

        public void Stop()
        {
            _timer.Stop();
            Thread.Sleep(5000);
            _assetPortfolioBalance.SavePortfolioToNoSql();
        }
    }
}
