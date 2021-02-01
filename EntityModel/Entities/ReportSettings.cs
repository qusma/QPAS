// -----------------------------------------------------------------------
// <copyright file="ReportSettings.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EntityModel
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
        private int _vaRDays;
        private bool _mCWithReplacement;
        private int _mCClusterSize;
        private int _mCRuns;
        private int _mCPeriods;
        private Benchmark benchmark;

        public int Id { get; set; }

        public string Name { get; set; }

        public ReturnType MCReturnType
        {
            get { return _mcReturnType; }
            set { _mcReturnType = value; OnPropertyChanged(); }
        }

        public int MCPeriods
        {
            get { return _mCPeriods; }
            set { _mCPeriods = value; OnPropertyChanged(); }
        }

        public int MCRuns
        {
            get { return _mCRuns; }
            set { _mCRuns = value; OnPropertyChanged(); }
        }

        public int MCClusterSize
        {
            get { return _mCClusterSize; }
            set { _mCClusterSize = value; OnPropertyChanged(); }
        }

        public bool MCWithReplacement
        {
            get { return _mCWithReplacement; }
            set { _mCWithReplacement = value; OnPropertyChanged(); }
        }

        public Benchmark Benchmark
        {
            get { return benchmark; }
            set { benchmark = value; OnPropertyChanged(); }
        }

        public int? BenchmarkId { get; set; }

        /// <summary>
        /// Set an instrument to be used as a backtest series to compare against.
        /// </summary>
        [NotMapped]
        public QDMS.Instrument Backtest
        {
            get { return _backtest; }
            set { 
                _backtest = value; 
                OnPropertyChanged();
                BacktestExternalInstrumentId = value?.ID;
            }
        }

        public int? BacktestExternalInstrumentId { get; set; }

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

        public int VaRDays
        {
            get { return _vaRDays; }
            set { _vaRDays = value; OnPropertyChanged(); }
        }

        public List<int> SelectedTags { get; set; } = new List<int>();

        public List<int> SelectedStrategies { get; set; } = new List<int>();

        public List<int> SelectedInstruments { get; set; } = new List<int>();

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