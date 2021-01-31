// -----------------------------------------------------------------------
// <copyright file="IDBContext.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityModel
{
    public interface IQpasDbContext : IDisposable
    {
        DatabaseFacade Database { get; }
        EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

        DbSet<Benchmark> Benchmarks { get; set; }
        DbSet<BenchmarkComponent> BenchmarkComponents { get; set; }
        DbSet<CashTransaction> CashTransactions { get; set; }
        DbSet<Currency> Currencies { get; set; }
        DbSet<DividendAccrual> DividendAccruals { get; set; }
        DbSet<EquitySummary> EquitySummaries { get; set; }
        DbSet<Execution> Executions { get; set; }
        DbSet<FXPosition> FXPositions { get; set; }
        DbSet<FXRate> FXRates { get; set; }
        DbSet<FXTransaction> FXTransactions { get; set; }
        DbSet<Instrument> Instruments { get; set; }
        DbSet<OpenPosition> OpenPositions { get; set; }
        DbSet<Order> Orders { get; set; }
        DbSet<PriorPosition> PriorPositions { get; set; }
        DbSet<Strategy> Strategies { get; set; }
        DbSet<Tag> Tags { get; set; }
        DbSet<Trade> Trades { get; set; }
        DbSet<DatasourcePreference> DatasourcePreferences { get; set; }
        DbSet<Account> Accounts { get; set; }
        DbSet<UserScript> UserScripts { get; set; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        int SaveChanges();
        //
        // Summary:
        //     Saves all changes made in this context to the database.
        //     This method will automatically call Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges
        //     to discover any changes to entity instances before saving to the underlying database.
        //     This can be disabled via Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled.
        //     Multiple active operations on the same context instance are not supported. Use
        //     'await' to ensure that any asynchronous operations have completed before calling
        //     another method on this context.
        //
        // Parameters:
        //   acceptAllChangesOnSuccess:
        //     Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges
        //     is called after the changes have been sent successfully to the database.
        //
        //   cancellationToken:
        //     A System.Threading.CancellationToken to observe while waiting for the task to
        //     complete.
        //
        // Returns:
        //     A task that represents the asynchronous save operation. The task result contains
        //     the number of state entries written to the database.
        //
        // Exceptions:
        //   T:Microsoft.EntityFrameworkCore.DbUpdateException:
        //     An error is encountered while saving to the database.
        //
        //   T:Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
        //     A concurrency violation is encountered while saving to the database. A concurrency
        //     violation occurs when an unexpected number of rows are affected during save.
        //     This is usually because the data in the database has been modified since it was
        //     loaded into memory.
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
        //
        // Summary:
        //     Saves all changes made in this context to the database.
        //     This method will automatically call Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges
        //     to discover any changes to entity instances before saving to the underlying database.
        //     This can be disabled via Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled.
        //     Multiple active operations on the same context instance are not supported. Use
        //     'await' to ensure that any asynchronous operations have completed before calling
        //     another method on this context.
        //
        // Parameters:
        //   cancellationToken:
        //     A System.Threading.CancellationToken to observe while waiting for the task to
        //     complete.
        //
        // Returns:
        //     A task that represents the asynchronous save operation. The task result contains
        //     the number of state entries written to the database.
        //
        // Exceptions:
        //   T:Microsoft.EntityFrameworkCore.DbUpdateException:
        //     An error is encountered while saving to the database.
        //
        //   T:Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
        //     A concurrency violation is encountered while saving to the database. A concurrency
        //     violation occurs when an unexpected number of rows are affected during save.
        //     This is usually because the data in the database has been modified since it was
        //     loaded into memory.
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        EntityEntry Entry(object obj);
    }
}
