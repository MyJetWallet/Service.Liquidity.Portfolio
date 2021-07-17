using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public class DatabaseContext : DbContext
    {
        private Activity _activity;
        public DbSet<AssetPortfolioTrade> Trades { get; set; }
        public DbSet<ChangeBalanceHistory> ChangeBalanceHistories { get; set; }

        private const string ChangeBalanceHistoryTableName = "changebalancehistory";
        
        public const string Schema = "liquidityportfolio";
        
        public static ILoggerFactory LoggerFactory { get; set; }
        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (LoggerFactory != null)
            {
                optionsBuilder.UseLoggerFactory(LoggerFactory).EnableSensitiveDataLogging();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            SetChangeBalanceHistoryEntity(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void SetChangeBalanceHistoryEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChangeBalanceHistory>().ToTable(ChangeBalanceHistoryTableName);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.Id).UseIdentityColumn();
            modelBuilder.Entity<ChangeBalanceHistory>().HasKey(e => e.Id);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.BrokerId).HasMaxLength(64);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.WalletName).HasMaxLength(64);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.Asset).HasMaxLength(64);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.VolumeDifference);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.UpdateDate);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.Comment).HasMaxLength(256);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.User).HasMaxLength(64);
        }

        public static DatabaseContext Create(DbContextOptionsBuilder<DatabaseContext> options)
        {
            var activity = MyTelemetry.StartActivity($"Database context {Schema}")?.AddTag("db-schema", Schema);
            var ctx = new DatabaseContext(options.Options) {_activity = activity};
            return ctx;
        }
        
        public async Task SaveChangeBalanceHistoryAsync(ChangeBalanceHistory history)
        {
            ChangeBalanceHistories.Add(history);
            await SaveChangesAsync();
        }
 
        public override void Dispose()
        {
            _activity?.Dispose();
            base.Dispose();
        }
    }
}
