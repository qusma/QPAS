// -----------------------------------------------------------------------
// <copyright file="DBContext.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using EntityModel.Migrations;

namespace EntityModel
{
    public class DBContext : DbContext, IDBContext
    {
        public DBContext()
            : base("Name=qpasEntities")
        {

        }

        public DBContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public IDbSet<Benchmark> Benchmarks { get; set; }

        public IDbSet<BenchmarkComponent> BenchmarkComponents { get; set; }

        public IDbSet<CashTransaction> CashTransactions { get; set; }

        public IDbSet<Currency> Currencies { get; set; }

        public IDbSet<DividendAccrual> DividendAccruals { get; set; }

        public IDbSet<EquitySummary> EquitySummaries { get; set; }

        public IDbSet<Execution> Executions { get; set; }

        public IDbSet<FXPosition> FXPositions { get; set; }

        public IDbSet<FXRate> FXRates { get; set; }

        public IDbSet<FXTransaction> FXTransactions { get; set; }

        public IDbSet<Instrument> Instruments { get; set; }

        public IDbSet<OpenPosition> OpenPositions { get; set; }

        public IDbSet<Order> Orders { get; set; }

        public IDbSet<PriorPosition> PriorPositions { get; set; }

        public IDbSet<Strategy> Strategies { get; set; }

        public IDbSet<Tag> Tags { get; set; }

        public IDbSet<Trade> Trades { get; set; }

        public IDbSet<DatasourcePreference> DatasourcePreferences { get; set; }

        public IDbSet<Account> Accounts { get; set; }

        public IDbSet<UserScript> UserScripts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Properties<decimal>().Configure(config => config.HasPrecision(20, 10));

            modelBuilder.Entity<Trade>()
                .HasMany(c => c.Tags)
                .WithMany()
                .Map(x =>
            {
                x.MapLeftKey("TradeID");
                x.MapRightKey("TagID");
                x.ToTable("TagMap");
            });

            modelBuilder.Entity<Order>().HasMany(x => x.Executions).WithRequired(x => x.Order).HasForeignKey(x => x.OrderID);
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            //modelBuilder.Properties<string>().Configure(p => p.IsUnicode(true));
            //modelBuilder.Entity<Trade>().Property(p => p.Notes).IsUnicode(true);
            //modelBuilder.Conventions.AddAfter<MaxLengthAttributeConvention>(new UnicodeConvention());

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DBContext, MyDbContextMigrationConfiguration>());
        }
    }
}