// -----------------------------------------------------------------------
// <copyright file="PerformanceReportViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MathNet.Numerics.Statistics;
using OxyPlot;
using OxyPlot.Wpf;
using QPAS.DataSets;

namespace QPAS
{
    public class PerformanceReportViewModel : ViewModelBase
    {
        private double _retVsSizeBestFitLineConstant;
        private double _retVsSizeBestFitLineSlope;
        private double _retVsLengthBestFitLineConstant;
        private double _retVsLengthBestFitLineSlope;
        private PlotModel _plByStrategyModel;
        private PlotModel _capitalUsageByStrategyModel;
        private PlotModel _relativeCapitalUsageByStrategyModel;
        private PlotModel _roacByStrategyModel;
        private PlotModel _mdsChartModel;

        public filterReportDS Data { get; set; }

        public ReportSettings Settings { get; set; }

        public ICommand CopyChart { get; private set; }

        public ICommand SaveChart { get; private set; }

        //Plot models
        public PlotModel PLByStrategyModel
        {
            get { return _plByStrategyModel; }
            set { _plByStrategyModel = value; OnPropertyChanged(); }
        }

        public PlotModel CapitalUsageByStrategyModel
        {
            get { return _capitalUsageByStrategyModel; }
            set { _capitalUsageByStrategyModel = value; OnPropertyChanged(); }
        }

        public PlotModel RelativeCapitalUsageByStrategyModel
        {
            get { return _relativeCapitalUsageByStrategyModel; }
            set { _relativeCapitalUsageByStrategyModel = value; OnPropertyChanged(); }
        }

        public PlotModel RoacByStrategyModel
        {
            get { return _roacByStrategyModel; }
            set { _roacByStrategyModel = value; OnPropertyChanged(); }
        }

        public PlotModel MdsChartModel
        {
            get { return _mdsChartModel; }
            set { _mdsChartModel = value; OnPropertyChanged(); }
        }

        //bit of a hack to filter the mae/mfe datatable

        #region maemfeproperties

        public List<Point> WinnerMAEs
        {
            get
            {
                return Enumerable.Select(Data.TradeMAE.Where(x => x.ret > 0), x => new Point(x.mae, x.absReturn)).ToList();
            }
        }

        public List<Point> LoserMAEs
        {
            get
            {
                return Enumerable.Select(Data.TradeMAE.Where(x => x.ret <= 0), x => new Point(x.mae, x.absReturn)).ToList();
            }
        }

        public List<Point> WinnerMFEs
        {
            get
            {
                return Enumerable.Select(Data.TradeMFE.Where(x => x.ret > 0), x => new Point(x.mfe, x.absReturn)).ToList();
            }
        }

        public List<Point> LoserMFEs
        {
            get
            {
                return Enumerable.Select(Data.TradeMFE.Where(x => x.ret <= 0), x => new Point(x.mfe, x.absReturn)).ToList();
            }
        }

        #endregion maemfeproperties

        //return vs X scatter plots
        public double RetVsSizeBestFitLineConstant
        {
            get { return _retVsSizeBestFitLineConstant; }
            set { _retVsSizeBestFitLineConstant = value; OnPropertyChanged(); }
        }

        public double RetVsSizeBestFitLineSlope
        {
            get { return _retVsSizeBestFitLineSlope; }
            set { _retVsSizeBestFitLineSlope = value; OnPropertyChanged(); }
        }

        public double RetVsLengthBestFitLineConstant
        {
            get { return _retVsLengthBestFitLineConstant; }
            set { _retVsLengthBestFitLineConstant = value; OnPropertyChanged(); }
        }

        public double RetVsLengthBestFitLineSlope
        {
            get { return _retVsLengthBestFitLineSlope; }
            set { _retVsLengthBestFitLineSlope = value; OnPropertyChanged(); }
        }


        //Histograms
        public List<Tuple<string, double>> MCSharpeHistogramBuckets { get; set; }
        public List<Tuple<string, double>> MCMARHistogramBuckets { get; set; }
        public List<Tuple<string, double>> MCKRatioHistogramBuckets { get; set; }

        //Benchmark stuff
        public string BenchmarkInstrument { get; set; }
        public double BenchmarkAlpha { get; set; }
        public double BenchmarkBeta { get; set; }
        public double BenchmarkRSquare { get; set; }
        public double BenchmarkCorrelation { get; set; }

        //We need to hack around the datatable dbnull issue by using the following two series
        //Since they don't have points across the entire range
        public List<KeyValuePair<int, double>> AvgCumulativeWinnerRets { get; set; }
        public List<KeyValuePair<int, double>> AvgCumulativeLoserRets { get; set; }

        public PerformanceReportViewModel(filterReportDS data, ReportSettings settings, IDialogService dialogService) : base(null)
        {
            Data = data;
            Settings = settings;
            CopyChart = new RelayCommand<PlotView>(x => x.CopyToClipboard());
            SaveChart = new RelayCommand<PlotView>(x =>
                {
                    try
                    {
                        x.SaveAsPNG();
                    }
                    catch (Exception ex)
                    {
                        dialogService.ShowMessageAsync("Error saving image", ex.Message);
                    }
                });

            //Calculate best fit line for the return vs size scatter chart
            double rsq;
            double[] b;
            Utils.MLR(
                Data.positionSizesVsReturns.Select(x => x.size).ToList(),
                Data.positionSizesVsReturns.Select(x => x.ret).ToList(),
                out b,
                out rsq);

            _retVsSizeBestFitLineConstant = b[0];
            _retVsSizeBestFitLineSlope = b[1];

            //Calculate best fit line for the return vs length scatter chart
            Utils.MLR(
                Data.tradeLengthsVsReturns.Select(x => x.ret).ToList(),
                Data.tradeLengthsVsReturns.Select(x => x.length).ToList(),
                out b,
                out rsq);

                _retVsLengthBestFitLineConstant = b[0];
                _retVsLengthBestFitLineSlope = b[1];

            //Histograms
            try
            {
                var sharpeHistogram = new Histogram(Enumerable.Select(Data.MCRatios, x => x.Sharpe), 20);
                MCSharpeHistogramBuckets = sharpeHistogram.GetBuckets();

                var marHistogram = new Histogram(Enumerable.Select(Data.MCRatios, x => x.MAR), 20);
                MCMARHistogramBuckets = marHistogram.GetBuckets();

                var kRatioHistogram = new Histogram(Enumerable.Select(Data.MCRatios, x => x.KRatio), 20);
                MCKRatioHistogramBuckets = kRatioHistogram.GetBuckets("0");
            }
            catch { } //Yeah this is bad, there's a ton of stupid errors that are not easy to check for...re-do this in the future

            //Benchmark stats
            if (Settings.Benchmark != null)
            {
                BenchmarkAlpha = ((filterReportDS.benchmarkStatsRow)Data.benchmarkStats.Rows[0]).alpha;
                BenchmarkBeta = ((filterReportDS.benchmarkStatsRow)Data.benchmarkStats.Rows[0]).beta;
                BenchmarkCorrelation = ((filterReportDS.benchmarkStatsRow)Data.benchmarkStats.Rows[0]).correlation;
                BenchmarkRSquare = ((filterReportDS.benchmarkStatsRow)Data.benchmarkStats.Rows[0]).rsquare;
                BenchmarkInstrument = Settings.Benchmark.Name;
            }

            //avg cumulative winner/loser returns by day in trade
            AvgCumulativeWinnerRets = 
                Enumerable.Select(data
                    .AverageDailyRets
                    .Where(x => !x.IswinnersRetsNull()), x => new KeyValuePair<int, double>(x.day, x.winnersRets))
                .ToList();

            AvgCumulativeLoserRets =
                Enumerable.Select(data
                    .AverageDailyRets
                    .Where(x => !x.IslosersRetsNull()), x => new KeyValuePair<int, double>(x.day, x.losersRets))
                .ToList();

            //build plot models
            CreatePLByStrategyChartModel();
            CreateCapitalUsageByStrategyChartModel();
            CreateRelativeCapitalUsageByStrategyChartModel();
            CreateRoacByStrategyChartModel();
            CreateMdsChartModel();
        }

        private void CreateMdsChartModel()
        {
            var model = new PlotModel();

            var xAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.None
            };
            model.Axes.Add(xAxis);

            var yAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                MajorGridlineStyle = LineStyle.None
            };
            model.Axes.Add(yAxis);

            var series = new OxyPlot.Series.ScatterSeries
            {
                ItemsSource = (Data.MdsCoords.Rows.Cast<filterReportDS.MdsCoordsRow>()
                    .Select(dr => new DataPoint(dr.X, dr.Y))),
                DataFieldX = "X",
                DataFieldY = "Y",
                MarkerType = MarkerType.Circle,
                MarkerSize = 2,
                MarkerFill = OxyColor.FromRgb(79, 129, 189)
            };

            model.Series.Add(series);

            foreach(filterReportDS.MdsCoordsRow dr in Data.MdsCoords.Rows)
            {
                var annotation = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = dr.StrategyName,
                    TextPosition = new DataPoint(dr.X, dr.Y),
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top,
                    Font = "Segoe UI",
                    TextColor = OxyColor.FromRgb(0, 0, 0),
                    StrokeThickness = 0
                };

                model.Annotations.Add(annotation);
            }

            MdsChartModel = model;
        }

        private void CreateRoacByStrategyChartModel()
        {
            var model = new PlotModel();

            var xAxis = new OxyPlot.Axes.DateTimeAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd"
            };
            model.Axes.Add(xAxis);

            var yAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                StringFormat = "p1",
                MajorGridlineStyle = LineStyle.Dash
            };
            model.Axes.Add(yAxis);

            foreach (DataColumn column in Data.StrategyROAC.Columns)
            {
                if (column.ColumnName == "Date") continue;

                DataColumn column1 = column;
                var series = new OxyPlot.Series.LineSeries
                {
                    ItemsSource = Data.StrategyROAC.Select(x => new { X = x.Date, Y = x.Field<double>(column1.ColumnName) }),
                    Title = column.ColumnName,
                    CanTrackerInterpolatePoints = false,
                    TrackerFormatString = "Strategy: " + column.ColumnName + @" Date: {2:yyyy-MM-dd} P/L: {4:p12",
                    DataFieldX = "X",
                    DataFieldY = "Y",
                    MarkerType = MarkerType.None
                };
                model.Series.Add(series);
            }

            model.LegendPosition = LegendPosition.BottomCenter;
            model.LegendOrientation = LegendOrientation.Horizontal;
            model.LegendPlacement = LegendPlacement.Outside;

            RoacByStrategyModel = model;
        }

        private void CreatePLByStrategyChartModel()
        {
            var model = new PlotModel();

            var xAxis = new OxyPlot.Axes.DateTimeAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd"
            };
            model.Axes.Add(xAxis);

            var yAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                StringFormat = "c0",
                MajorGridlineStyle = LineStyle.Dash
            };
            model.Axes.Add(yAxis);

            foreach (DataColumn column in Data.StrategyPLCurves.Columns)
            {
                if (column.ColumnName == "date") continue;

                DataColumn column1 = column;
                var series = new OxyPlot.Series.LineSeries
                {
                    ItemsSource = Data.StrategyPLCurves.Select(x => new { X = x.date, Y = x.Field<double>(column1.ColumnName) }),
                    Title = column.ColumnName,
                    CanTrackerInterpolatePoints = false,
                    TrackerFormatString = "Strategy: " + column.ColumnName + @" Date: {2:yyyy-MM-dd} P/L: {4:c0}",
                    DataFieldX = "X",
                    DataFieldY = "Y",
                    MarkerType = MarkerType.None
                };
                model.Series.Add(series);
            }

            model.LegendPosition = LegendPosition.BottomCenter;
            model.LegendOrientation = LegendOrientation.Horizontal;
            model.LegendPlacement = LegendPlacement.Outside;

            PLByStrategyModel = model;
        }

        private void CreateCapitalUsageByStrategyChartModel()
        {
            var model = new PlotModel();

            var xAxis = new OxyPlot.Axes.DateTimeAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd"
            };
            model.Axes.Add(xAxis);

            var yAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                StringFormat = "c0",
                MajorGridlineStyle = LineStyle.Dash
            };
            model.Axes.Add(yAxis);

            var capUsageTmpSum = Enumerable.Range(0, Data.CapitalUsageByStrategy.Rows.Count).Select(x => 0.0).ToList();

            foreach (DataColumn column in Data.CapitalUsageByStrategy.Columns)
            {
                if (column.ColumnName == "date") continue;

                DataColumn column1 = column;
                List<double> sum = capUsageTmpSum;
                var series = new OxyPlot.Series.AreaSeries
                {
                    ItemsSource = Data
                    .CapitalUsageByStrategy
                    .Select((x, i) => 
                        new { 
                            X = x.date, 
                            Y = sum[i],
                            Y2 = sum[i] + x.Field<double>(column1.ColumnName),
                        }),

                    Title = column.ColumnName,
                    CanTrackerInterpolatePoints = false,
                    TrackerFormatString = "Strategy: " + column.ColumnName + @" Date: {2:yyyy-MM-dd} Capital Usage: {4:c0}",
                    DataFieldX = "X",
                    DataFieldX2 = "X",
                    DataFieldY = "Y",
                    DataFieldY2 = "Y2",
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };

                capUsageTmpSum = Data.CapitalUsageByStrategy.Select((x, i) => x.Field<double>(column1.ColumnName) + capUsageTmpSum[i]).ToList();

                model.Series.Add(series);
            }

            model.LegendPosition = LegendPosition.BottomCenter;
            model.LegendOrientation = LegendOrientation.Horizontal;
            model.LegendPlacement = LegendPlacement.Outside;

            CapitalUsageByStrategyModel = model;
        }


        private void CreateRelativeCapitalUsageByStrategyChartModel()
        {
            var model = new PlotModel();

            var xAxis = new OxyPlot.Axes.DateTimeAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd"
            };
            model.Axes.Add(xAxis);

            var yAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                StringFormat = "p0",
                MajorGridlineStyle = LineStyle.Dash
            };
            model.Axes.Add(yAxis);

            var capUsageTmpSum = Enumerable.Range(0, Data.RelativeCapitalUsageByStrategy.Rows.Count).Select(x => 0.0).ToList();

            foreach (DataColumn column in Data.RelativeCapitalUsageByStrategy.Columns)
            {
                if (column.ColumnName == "date") continue;

                DataColumn column1 = column;
                List<double> sum = capUsageTmpSum;
                var series = new OxyPlot.Series.AreaSeries
                {
                    ItemsSource = Data
                    .RelativeCapitalUsageByStrategy
                    .Select((x, i) =>
                        new
                        {
                            X = x.date,
                            Y = sum[i],
                            Y2 = sum[i] + x.Field<double>(column1.ColumnName),
                        }),

                    Title = column.ColumnName,
                    CanTrackerInterpolatePoints = false,
                    TrackerFormatString = "Strategy: " + column.ColumnName + @" Date: {2:yyyy-MM-dd} Capital Usage: {4:p1}",
                    DataFieldX = "X",
                    DataFieldX2 = "X",
                    DataFieldY = "Y",
                    DataFieldY2 = "Y2",
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };

                capUsageTmpSum = Data.RelativeCapitalUsageByStrategy.Select((x, i) => x.Field<double>(column1.ColumnName) + capUsageTmpSum[i]).ToList();

                model.Series.Add(series);
            }

            model.LegendPosition = LegendPosition.BottomCenter;
            model.LegendOrientation = LegendOrientation.Horizontal;
            model.LegendPlacement = LegendPlacement.Outside;

            RelativeCapitalUsageByStrategyModel = model;
        }
    }
}