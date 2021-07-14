using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class BalancePersistJob
    {
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<BalancePersistJob> _logger;
        private readonly MyTaskTimer _timer;

        public BalancePersistJob(IPortfolioHandler portfolioHandler,
            ILogger<BalancePersistJob> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _portfolioHandler = portfolioHandler;
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _timer = new MyTaskTimer(nameof(BalancePersistJob), TimeSpan.FromSeconds(Program.Settings.UpdateDbBalancesTimerInSeconds), _logger, DoTime);
            Console.WriteLine($"BalancePersistJob timer: {TimeSpan.FromSeconds(Program.Settings.UpdateDbBalancesTimerInSeconds)}");
        }

        public void Start()
        {
            GetSnapshotFromDb().GetAwaiter().GetResult();
            _timer.Start();
        }

        private async Task DoTime()
        {
            await SaveSnapshotToDb();
        }

        private async Task GetSnapshotFromDb()
        {
            Console.WriteLine("GetSnapshotFromDb");
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            var dbBalances = await ctx.Balances.ToListAsync();
            _portfolioHandler.UpdateBalance(dbBalances);
        }

        private async Task SaveSnapshotToDb()
        {
            Console.WriteLine("SaveSnapshotToDb");
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            var localBalances = _portfolioHandler.GetBalancesSnapshot();
            await ctx.UpdateBalancesAsync(localBalances);

            _logger.LogInformation("Save Snapshot to db");
        }

        public void Stop()
        {
            _timer.Stop();
            Thread.Sleep(5000);
            SaveSnapshotToDb().GetAwaiter().GetResult();
        }
    }
}
