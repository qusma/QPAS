// -----------------------------------------------------------------------
// <copyright file="ReportSettings.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EntityModel;

namespace QPAS
{
    public class ReportSettings : INotifyPropertyChanged
    {
        private ReturnType _vaRReturnType;
        private ReturnType _returnsToBenchmark;
        private ReturnType _mcReturnType;

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
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}