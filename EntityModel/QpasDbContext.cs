﻿// -----------------------------------------------------------------------
// <copyright file="DBContext.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace EntityModel
{
    public class QpasDbContext : DbContext, IQpasDbContext
    {
        public DbSet<Benchmark> Benchmarks { get; set; }

        public DbSet<BenchmarkComponent> BenchmarkComponents { get; set; }

        public DbSet<CashTransaction> CashTransactions { get; set; }

        public DbSet<Currency> Currencies { get; set; }

        public DbSet<DividendAccrual> DividendAccruals { get; set; }

        public DbSet<EquitySummary> EquitySummaries { get; set; }

        public DbSet<Execution> Executions { get; set; }

        public DbSet<FXPosition> FXPositions { get; set; }

        public DbSet<FXRate> FXRates { get; set; }

        public DbSet<FXTransaction> FXTransactions { get; set; }

        public DbSet<Instrument> Instruments { get; set; }

        public DbSet<OpenPosition> OpenPositions { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<PriorPosition> PriorPositions { get; set; }

        public DbSet<Strategy> Strategies { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<Trade> Trades { get; set; }

        public DbSet<DatasourcePreference> DatasourcePreferences { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<UserScript> UserScripts { get; set; }

        public DbSet<ReportSettings> ReportSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(20);
                property.SetScale(10);
            }

            foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string)))
            {
                property.SetIsUnicode(true);
            }

            //Indexes
            modelBuilder.Entity<Account>()
                .HasIndex(x => x.AccountId)
                .IsUnique();

            modelBuilder.Entity<CashTransaction>()
                .HasIndex(x => x.TransactionDate);

            modelBuilder.Entity<CashTransaction>()
                .HasIndex(x => x.Type);

            modelBuilder.Entity<FXRate>()
                .HasIndex(x => x.Date);

            modelBuilder.Entity<Order>()
                .HasIndex(x => x.TradeDate);

            modelBuilder.Entity<Order>()
                .HasIndex(x => x.IBOrderID)
                .IsUnique();

            modelBuilder.Entity<Instrument>()
                .HasIndex(x => x.ConID);

            modelBuilder.Entity<PriorPosition>()
                .HasIndex(x => x.Date);

            modelBuilder.Entity<Trade>()
                .HasIndex(x => x.DateOpened);

            modelBuilder.Entity<ReportSettings>()
                .Property(x => x.SelectedTags)
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    serialized => JsonConvert.DeserializeObject<List<int>>(serialized));

            modelBuilder.Entity<ReportSettings>()
                .Property(x => x.SelectedStrategies)
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    serialized => JsonConvert.DeserializeObject<List<int>>(serialized));

            modelBuilder.Entity<ReportSettings>()
                .Property(x => x.SelectedInstruments)
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    serialized => JsonConvert.DeserializeObject<List<int>>(serialized));

            //relations
            modelBuilder.Entity<Trade>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Trades)
                .UsingEntity(x => x.ToTable("TagMap"));

            modelBuilder.Entity<Order>()
                .HasMany(x => x.Executions)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderID)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public readonly ILoggerFactory MyLoggerFactory;

        public QpasDbContext()
        {
        }

        public QpasDbContext(DbContextOptions<QpasDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=qpas.db;");
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            }

#if DEBUG
            //optionsBuilder.LogTo(message => System.Diagnostics.Debug.WriteLine(message), LogLevel.Trace);
            //optionsBuilder.EnableSensitiveDataLogging();
#endif
        }
    }
}