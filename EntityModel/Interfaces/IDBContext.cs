// -----------------------------------------------------------------------
// <copyright file="IDBContext.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace EntityModel
{
    public interface IDBContext
    {
        IDbSet<Benchmark> Benchmarks { get; set; }
        IDbSet<BenchmarkComponent> BenchmarkComponents { get; set; }
        IDbSet<CashTransaction> CashTransactions { get; set; }
        IDbSet<Currency> Currencies { get; set; }
        IDbSet<DividendAccrual> DividendAccruals { get; set; }
        IDbSet<EquitySummary> EquitySummaries { get; set; }
        IDbSet<Execution> Executions { get; set; }
        IDbSet<FXPosition> FXPositions { get; set; }
        IDbSet<FXRate> FXRates { get; set; }
        IDbSet<FXTransaction> FXTransactions { get; set; }
        IDbSet<Instrument> Instruments { get; set; }
        IDbSet<OpenPosition> OpenPositions { get; set; }
        IDbSet<Order> Orders { get; set; }
        IDbSet<PriorPosition> PriorPositions { get; set; }
        IDbSet<Strategy> Strategies { get; set; }
        IDbSet<Tag> Tags { get; set; }
        IDbSet<Trade> Trades { get; set; }
        IDbSet<DatasourcePreference> DatasourcePreferences { get; set; }
        IDbSet<Account> Accounts { get; set; }

        DbChangeTracker ChangeTracker { get; }
        DbContextConfiguration Configuration { get; }
        Database Database { get; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        DbSet Set(Type entityType);

        DbEntityEntry Entry(object entity);
        DbEntityEntry<T> Entry<T>(T entity) where T : class;
        int SaveChanges();
        void Dispose();
    }
}
