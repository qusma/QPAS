// -----------------------------------------------------------------------
// <copyright file="ReportSettings.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QPAS
{
    public class ReportSettings : INotifyPropertyChanged
    {

        private ReturnType _vaRReturnType;
        private ReturnType _returnsToBenchmark;
        private ReturnType _mcReturnType;
        private ReturnType _autoCorrReturnType;
        private ReturnType _backtestComparisonReturnType;
        private BacktestSource _backtestSource;
        private QDMS.Instrument _backtest;

        public List<int> TradeIDs { get; set; }

        public ReturnType MCReturnType
        {
            get { return _mcReturnType; }
            set { _mcReturnType = value; OnPropertyChanged(); }
        }

        public int MCPeriods { get; set; }

        public int MCRuns { get; set; }

        public int MCClusterSize { get; set; }

        public bool MCWithReplacement { get; set; }

        public Benchmark Benchmark { get; set; }

        /// <summary>
        /// Set an instrument to be used as a backtest series to compare against.
        /// </summary>
        public QDMS.Instrument Backtest
        {
            get { return _backtest; }
            set { _backtest = value; OnPropertyChanged(); }
        }

        public BacktestSource BacktestSource
        {
            get { return _backtestSource; }
            set { _backtestSource = value; OnPropertyChanged(); }
        }

        public ReturnType BacktestComparisonReturnType
        {
            get { return _backtestComparisonReturnType; }
            set { _backtestComparisonReturnType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// If backtest results are loaded from a file, they're put in here.
        /// </summary>
        public EquityCurve BacktestData { get; set; }

        public ReturnType ReturnsToBenchmark
        {
            get { return _returnsToBenchmark; }
            set { _returnsToBenchmark = value; OnPropertyChanged(); }
        }

        public ReturnType VaRReturnType
        {
            get { return _vaRReturnType; }
            set { _vaRReturnType = value; OnPropertyChanged(); }
        }

        public ReturnType AutoCorrReturnType
        {
            get { return _autoCorrReturnType; }
            set { _autoCorrReturnType = value; OnPropertyChanged(); }
        }

        public int VaRDays { get; set; }

        public ReportSettings()
        {
            MCPeriods = 504;
            MCRuns = 2000;
            MCClusterSize = 5;
            MCWithReplacement = true;
            MCReturnType = ReturnType.ROTC;
            ReturnsToBenchmark = ReturnType.ROTC;

            VaRReturnType = ReturnType.ROTC;
            VaRDays = 5;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}