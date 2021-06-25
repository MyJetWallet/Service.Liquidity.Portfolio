using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Postgres
{
    public class TradeContext : DbContext
    {
        public DbSet<Trade> Trades { get; set; }
        
        public const string TradeTableName = "trade";
        public const string Schema = "liquidityportfolio";
        
        public static ILoggerFactory LoggerFactory { get; set; }
        public TradeContext(DbContextOptions options) : base(options)
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
            modelBuilder.Entity<Trade>().ToTable(TradeTableName);
            modelBuilder.Entity<Trade>().HasKey(e => e.TradeUId);
            modelBuilder.Entity<Trade>().Property(e => e.Type).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.OrderVolume);
            modelBuilder.Entity<Trade>().Property(e => e.DateTime);
            modelBuilder.Entity<Trade>().Property(e => e.SequenceId);
            modelBuilder.Entity<Trade>().Property(e => e.TradeUId).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.Side);
            modelBuilder.Entity<Trade>().Property(e => e.OrderId).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.QuoteVolume);
            modelBuilder.Entity<Trade>().Property(e => e.Price);
            modelBuilder.Entity<Trade>().Property(e => e.InstrumentSymbol).HasMaxLength(64);
            modelBuilder.Entity<Trade>().Property(e => e.WalletId).HasMaxLength(64);
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
