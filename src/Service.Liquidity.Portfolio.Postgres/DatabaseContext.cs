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
        public DbSet<Trade> Trades { get; set; }
        public DbSet<AssetBalance> Balances { get; set; }
        public DbSet<ChangeBalanceHistory> ChangeBalanceHistories { get; set; }

        private const string TradeTableName = "trade";
        private const string BalanceTableName = "assetbalance";
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

            SetTradeEntity(modelBuilder);
            SetBalanceEntity(modelBuilder);
            SetChangeBalanceHistoryEntity(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void SetChangeBalanceHistoryEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChangeBalanceHistory>().ToTable(ChangeBalanceHistoryTableName);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.Id).UseIdentityColumn();
            modelBuilder.Entity<ChangeBalanceHistory>().HasKey(e => e.Id);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.BrokerId).HasMaxLength(64);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.ClientId).HasMaxLength(64);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.WalletId).HasMaxLength(64);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.Asset).HasMaxLength(64);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.VolumeDifference);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.UpdateDate);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.Comment).HasMaxLength(256);
            modelBuilder.Entity<ChangeBalanceHistory>().Property(e => e.User).HasMaxLength(64);
        }

        private void SetBalanceEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssetBalance>().ToTable(BalanceTableName);
            modelBuilder.Entity<AssetBalance>().HasKey(e => new {e.WalletId, e.Asset});
            modelBuilder.Entity<AssetBalance>().Property(e => e.BrokerId).HasMaxLength(64);
            modelBuilder.Entity<AssetBalance>().Property(e => e.ClientId).HasMaxLength(64);
            modelBuilder.Entity<AssetBalance>().Property(e => e.WalletId).HasMaxLength(64);
            modelBuilder.Entity<AssetBalance>().Property(e => e.Asset).HasMaxLength(64);
            modelBuilder.Entity<AssetBalance>().Property(e => e.Volume);
            modelBuilder.Entity<AssetBalance>().Property(e => e.UpdateDate);
        }

        private void SetTradeEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trade>().ToTable(TradeTableName);
            modelBuilder.Entity<Trade>().Property(e => e.Id).UseIdentityColumn();
            modelBuilder.Entity<Trade>().HasKey(e => e.Id);
            modelBuilder.Entity<Trade>().Property(e => e.TradeId).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.BrokerId).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.ClientId).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.WalletId).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.Symbol).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.Side);
            modelBuilder.Entity<Trade>().Property(e => e.Price);
            modelBuilder.Entity<Trade>().Property(e => e.BaseVolume);
            modelBuilder.Entity<Trade>().Property(e => e.QuoteVolume);
            modelBuilder.Entity<Trade>().Property(e => e.BaseVolumeInUsd);
            modelBuilder.Entity<Trade>().Property(e => e.QuoteVolumeInUsd);
            modelBuilder.Entity<Trade>().Property(e => e.DateTime);
            modelBuilder.Entity<Trade>().Property(e => e.ErrorMessage).HasMaxLength(256);
            modelBuilder.Entity<Trade>().Property(e => e.Source).HasMaxLength(64);
            
            modelBuilder.Entity<Trade>().HasIndex(e => e.TradeId).IsUnique();
        }

        public static DatabaseContext Create(DbContextOptionsBuilder<DatabaseContext> options)
        {
            var activity = MyTelemetry.StartActivity($"Database context {Schema}")?.AddTag("db-schema", Schema);
            var ctx = new DatabaseContext(options.Options) {_activity = activity};
            return ctx;
        }

        public async Task UpdateBalancesAsync(List<AssetBalance> balances)
        {
            await Balances
                .UpsertRange(balances)
                .On(e => new {e.WalletId, e.Asset})
                .RunAsync();
        }
        
        public async Task SaveChangeBalanceHistoryAsync(List<ChangeBalanceHistory> history)
        {
            ChangeBalanceHistories.AddRange(history);
            await SaveChangesAsync();
        }
        
        public async Task SaveTradesAsync(IEnumerable<Trade> trades)
        {
            await Trades
                .UpsertRange(trades)
                .On(e => e.TradeId)
                .RunAsync();
        }
        
        public override void Dispose()
        {
            _activity?.Dispose();
            base.Dispose();
        }
    }
}
