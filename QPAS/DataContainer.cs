// -----------------------------------------------------------------------
// <copyright file="AreaPoint.cs" company="">
// Copyright 2020 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System.Collections.ObjectModel;

namespace QPAS
{
    public class DataContainer
    {
        public ObservableCollection<Account> Accounts { get; } = new ObservableCollection<Account>();
        public ObservableCollection<Instrument> Instruments { get; } = new ObservableCollection<Instrument>();
        public ObservableCollection<CashTransaction> CashTransactions { get; } = new ObservableCollection<CashTransaction>();
        public ObservableCollection<Currency> Currencies { get; } = new ObservableCollection<Currency>();
        public ObservableCollection<DividendAccrual> DividendAccruals { get; } = new ObservableCollection<DividendAccrual>();
        public ObservableCollection<EquitySummary> EquitySummaries { get; } = new ObservableCollection<EquitySummary>();
        public ObservableCollection<Execution> Executions { get; } = new ObservableCollection<Execution>();
        public ObservableCollection<FXRate> FXRates { get; } = new ObservableCollection<FXRate>();
        public ObservableCollection<FXTransaction> FXTransactions { get; } = new ObservableCollection<FXTransaction>();
        public ObservableCollection<Order> Orders { get; } = new ObservableCollection<Order>();
        public ObservableCollection<PriorPosition> PriorPositions { get; } = new ObservableCollection<PriorPosition>();
        public ObservableCollection<Strategy> Strategies { get; } = new ObservableCollection<Strategy>();
        public ObservableCollection<Tag> Tags { get; } = new ObservableCollection<Tag>();
        public ObservableCollection<Trade> Trades { get; } = new ObservableCollection<Trade>();
        public ObservableCollection<Benchmark> Benchmarks { get; } = new ObservableCollection<Benchmark>();

        //below do not hold global state
        public ObservableCollection<OpenPosition> OpenPositions { get; } = new ObservableCollection<OpenPosition>();
        public ObservableCollection<FXPosition> FXPositions { get; } = new ObservableCollection<FXPosition>();
        public ObservableCollection<DatasourcePreference> DatasourcePreferences { get; } = new ObservableCollection<DatasourcePreference>();

    }
}
