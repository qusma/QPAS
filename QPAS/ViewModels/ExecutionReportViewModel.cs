// -----------------------------------------------------------------------
// <copyright file="ExecutionReportViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using MahApps.Metro.Controls.Dialogs;
using QPAS.DataSets;

namespace QPAS
{
    public class ExecutionReportViewModel : ViewModelBase
    {
        private double _timeDiffVsSlipBestFitLineSlope;
        private double _timeDiffVsSlipBestFitLineConstant;
        private ExecutionBenchmark _benchmark;
        public int OrderCount { get; set; }
        public ExecutionStatsGenerator StatsGenerator { get; set; }

        public ExecutionBenchmark Benchmark
        {
            get { return _benchmark; }
            set
            {
                if (value == _benchmark) return;
                _benchmark = value;
                OnPropertyChanged();
            }
        }

        public DateTime? ReferenceTime { get; set; }

        public bool UseSessionsTime { get; set; }

        public ICommand RunAnalysis { get; set; }

        public ExecutionReportDS Data { get; set; }

        public ObservableCollection<Point3D> TimeVsSlippagePoints { get; set; }

        public ObservableCollection<KeyValuePair<string, string>> Stats { get; set; }

        public double TimeDiffVsSlipBestFitLineSlope
        {
            get { return _timeDiffVsSlipBestFitLineSlope; }
            set
            {
                if (value.Equals(_timeDiffVsSlipBestFitLineSlope)) return;
                _timeDiffVsSlipBestFitLineSlope = value;
                OnPropertyChanged();
            }
        }

        public double TimeDiffVsSlipBestFitLineConstant
        {
            get { return _timeDiffVsSlipBestFitLineConstant; }
            set
            {
                if (value.Equals(_timeDiffVsSlipBestFitLineConstant)) return;
                _timeDiffVsSlipBestFitLineConstant = value;
                OnPropertyChanged();
            }
        }

        public ExecutionReportViewModel(ExecutionStatsGenerator statsGenerator, IDialogCoordinator dialogService)
            : base(dialogService)
        {
            UseSessionsTime = true;
            ReferenceTime = new DateTime(1, 1, 1, 16, 0, 0);

            StatsGenerator = statsGenerator;

            OrderCount = StatsGenerator.Orders.Count;

            Benchmark = ExecutionBenchmark.Close;

            Stats = new ObservableCollection<KeyValuePair<string, string>>();
            Data = new ExecutionReportDS();
            TimeVsSlippagePoints = new ObservableCollection<Point3D>();

            CreateCommands();
        }

        private void CreateCommands()
        {
            RunAnalysis = new RelayCommand(Run);
        }

        private async void Run()
        {
            string error = "";
            try
            {
                //If the user has selected a fixed reference time, we use tha
                //otherwise just pass null, which uses the QDMS instruments
                TimeSpan? referenceTime = 
                    (UseSessionsTime || !ReferenceTime.HasValue)
                        ? null 
                        : (TimeSpan?)ReferenceTime.Value.TimeOfDay;
                StatsGenerator.GenerateExecutionStats(Benchmark, referenceTime);
                SetStats();
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if(!string.IsNullOrEmpty(error))
            {
                await DialogService.ShowMessageAsync(this, "Error", error);
            }
        }

        private void FormatStats(List<ExecutionStats> stats)
        {
            Stats.Clear();
            Stats.Add(new KeyValuePair<string, string>("Average Commission", stats.Average(x => x.Commission).ToString("c4")));
            Stats.Add(new KeyValuePair<string, string>("Average Commission (%)", stats.Average(x => x.CommissionPct).ToString("p4")));
            Stats.Add(new KeyValuePair<string, string>("Average Slippage", stats.Average(x => x.Slippage).ToString("c4")));
            Stats.Add(new KeyValuePair<string, string>("Average Slippage (%)", stats.Average(x => x.SlippagePct).ToString("p4")));

            Stats.Add(new KeyValuePair<string, string>("Average Commission (W)", 
                stats.WeightedAverage(x => x.Commission, x => Math.Abs(x.Value)).ToString("c4")));
            Stats.Add(new KeyValuePair<string, string>("Average Commission (%) (W)",
                stats.WeightedAverage(x => x.CommissionPct, x => Math.Abs(x.Value)).ToString("p4")));
            Stats.Add(new KeyValuePair<string, string>("Average Slippage (W)",
                stats.WeightedAverage(x => x.Slippage, x => Math.Abs(x.Value)).ToString("c4")));
            Stats.Add(new KeyValuePair<string, string>("Average Slippage (%) (W)",
                stats.WeightedAverage(x => x.SlippagePct, x => Math.Abs((double)x.Value)).ToString("p4")));
        }

        private void SetStats()
        {
            List<ExecutionStats> stats = StatsGenerator.Stats;

            if (stats.Count == 0) return;

            //generate various stats
            FormatStats(stats);

            //then do the charts
            Data.Clear();

            //stats by venue
            var groupedByVenue = stats
                .GroupBy(x => x.Venue)
                .OrderBy(x => x.Average(y => y.SlippagePct));

            foreach(IGrouping<string,ExecutionStats> group in groupedByVenue)
            {
                var dr = Data.SlippageByVenue.NewSlippageByVenueRow();
                dr.Venue = group.Key;
                dr.AvgDollarSlippage = group.Average(x => x.Slippage);
                dr.AvgPctSlippage = group.Average(x => x.SlippagePct);
                dr.AvgDollarSlippageWeighted = group.WeightedAverage(x => x.Slippage, x => Math.Abs(x.Value));
                dr.AvgPctSlippageWeighted = group.WeightedAverage(x => x.SlippagePct, x => Math.Abs((double)x.Value));
                Data.SlippageByVenue.AddSlippageByVenueRow(dr);
            }

            //stats by order type
            var groupedByOrderType = stats
                .GroupBy(x => x.OrderType)
                .OrderBy(x => x.Average(y => y.SlippagePct));

            foreach (IGrouping<string, ExecutionStats> group in groupedByOrderType)
            {
                var dr = Data.SlippageByOrderType.NewSlippageByOrderTypeRow();
                dr.OrderType = group.Key;
                dr.AvgDollarSlippage = group.Average(x => x.Slippage);
                dr.AvgPctSlippage = group.Average(x => x.SlippagePct);
                dr.AvgDollarSlippageWeighted = group.WeightedAverage(x => x.Slippage, x => Math.Abs(x.Value));
                dr.AvgPctSlippageWeighted = group.WeightedAverage(x => x.SlippagePct, x => Math.Abs((double)x.Value));
                Data.SlippageByOrderType.AddSlippageByOrderTypeRow(dr);
            }

            //time vs slippage scatter chart
            decimal averageValue = stats.Average(x => Math.Abs(x.Value));
            TimeVsSlippagePoints.Clear();
            //What we do here: the Z value sets the point size
            //Which is set to 2 times the ratio between the size of this execution and the average size
            TimeVsSlippagePoints.AddRange(stats.Select(x => new Point3D(x.TimeDiff, x.SlippagePct, (double)(2 * (Math.Abs(x.Value) / averageValue)))));

            //then regress time difference vs slippage for the OLS line
            double[] b;
            double rsq;
            MathUtils.MLR(stats.Select(x => x.SlippagePct).ToList(), stats.Select(x => x.TimeDiff).ToList(), out b, out rsq);
            TimeDiffVsSlipBestFitLineConstant = b[0];
            TimeDiffVsSlipBestFitLineSlope = b[1];
        }
    }
}
