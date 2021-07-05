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
        private readonly IPortfolioStorage _portfolioStorage;
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<BalancePersistJob> _logger;
        private readonly MyTaskTimer _timer;

        public BalancePersistJob(IPortfolioStorage portfolioStorage,
            ILogger<BalancePersistJob> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _portfolioStorage = portfolioStorage;
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _timer = new MyTaskTimer(nameof(BalancePersistJob), TimeSpan.FromSeconds(5), _logger, DoTime);
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
            _portfolioStorage.UpdateBalances(dbBalances);
        }

        private async Task SaveSnapshotToDb()
        {
            Console.WriteLine("SaveSnapshotToDb");
            await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
            var localBalances = _portfolioStorage.GetBalancesSnapshot();
            await ctx.UpdateBalancesAsync(localBalances);
        }

        public void Stop()
        {
            _timer.Stop();
            Thread.Sleep(5000);
            SaveSnapshotToDb().GetAwaiter().GetResult();
        }
    }
}
