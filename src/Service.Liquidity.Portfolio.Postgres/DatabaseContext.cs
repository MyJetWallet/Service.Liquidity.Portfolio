﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private const string TradeTableName = "trade";
        private const string BalanceTableName = "assetbalance";
        
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

            base.OnModelCreating(modelBuilder);
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
            modelBuilder.Entity<Trade>().Property(e => e.TopicSource).HasMaxLength(64);
        }

        public static DatabaseContext Create(DbContextOptionsBuilder<DatabaseContext> options)
        {
            var activity = MyTelemetry.StartActivity($"Database context {Schema}")?.AddTag("db-schema", Schema);
            var ctx = new DatabaseContext(options.Options) {_activity = activity};
            return ctx;
        }

        public async Task UpdateBalancesAsync(List<AssetBalance> balances)
        {
            Balances
                .ToList()
                .ForEach(dbElem =>
            {
                balances.ForEach(newElem =>
                {
                    if (dbElem.WalletId == newElem.WalletId && dbElem.Asset == newElem.Asset)
                    {
                        newElem.Volume += dbElem.Volume;
                    }
                });
            });
            await Balances.UpsertRange(balances).On(e => new {e.WalletId, e.Asset}).RunAsync();
        }
        
        public async Task SaveTradesAsync(IEnumerable<Trade> trades)
        {
            Trades.AddRange(trades);
            await SaveChangesAsync();
        }
        
        public override void Dispose()
        {
            _activity?.Dispose();
            base.Dispose();
        }
    }
}
