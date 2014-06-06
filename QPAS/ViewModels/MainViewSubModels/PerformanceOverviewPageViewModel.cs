// -----------------------------------------------------------------------
// <copyright file="PerformanceOverviewPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace QPAS
{
    public class PerformanceOverviewPageViewModel : ViewModelBase
    {
        private List<AreaPoint> _equitySummaryPercentSeries;
        private List<AreaPoint> _equitySummaryPercentDrawdownSeries;

        internal IDBContext Context;
        private List<AreaPoint> _totalEquitySeries;
        private List<AreaPoint> _totalEquityDrawdownSeries;
        private DataTable _stats;

        public List<AreaPoint> EquitySummaryPercentSeries
        {
            get { return _equitySummaryPercentSeries; }
            set
            {
                _equitySummaryPercentSeries = value;
                OnPropertyChanged();
            }
        }

        public List<AreaPoint> EquitySummaryPercentDrawdownSeries
        {
            get { return _equitySummaryPercentDrawdownSeries; }
            set
            {
                _equitySummaryPercentDrawdownSeries = value;
                OnPropertyChanged();
            }
        }

        public List<AreaPoint> TotalEquitySeries
        {
            get { return _totalEquitySeries; }
            set
            {
                _totalEquitySeries = value;
                OnPropertyChanged();
            }
        }

        public List<AreaPoint> TotalEquityDrawdownSeries
        {
            get { return _totalEquityDrawdownSeries; }
            set
            {
                _totalEquityDrawdownSeries = value;
                OnPropertyChanged();
            }
        }

        public DataTable Stats
        {
            get { return _stats; }
            set
            {
                _stats = value;
                OnPropertyChanged();
            }
        }

        public PerformanceOverviewPageViewModel(IDBContext context, IDialogService dialogService)
            : base(dialogService)
        {
            Context = context;
        }

        public override void Refresh()
        {
            Context.EquitySummaries.OrderBy(x => x.Date).Load();

            CreatePctReturnSeries();
            CreateTotalEquitySeries();
            Stats = GeneratePerformanceOverviewStats();
        }

        private void CreateTotalEquitySeries()
        {
            var equityPoints = new List<AreaPoint>();
            var drawdownPoints = new List<AreaPoint>();

            decimal maxEquity = 0;

            foreach (EquitySummary es in Context.EquitySummaries.OrderBy(x => x.Date))
            {
                equityPoints.Add(new AreaPoint(es.Date, 0, (double)es.Total));
                maxEquity = Math.Max(maxEquity, es.Total);
                drawdownPoints.Add(new AreaPoint(es.Date, 0, (double)(es.Total - maxEquity)));
            }

            TotalEquitySeries = equityPoints;
            TotalEquityDrawdownSeries = drawdownPoints;
        }

        private void CreatePctReturnSeries()
        {
            var returnsPoints = new List<AreaPoint>();
            var drawdownPoints = new List<AreaPoint>();

            List<double> ec = new List<double>();
            List<double> rets = new List<double>();
            bool first = true;
            decimal lastTotal = 0;
            double equity = 1;
            double maxEquity = 1;

            //adjust the % returns for deposits/withdrawals as they obviously don't affect performance
            Dictionary<DateTime, decimal> depositsWithdrawals =
                Context
                .CashTransactions
                .Where(x => x.Type == "Deposits & Withdrawals")
                .ToList()
                .GroupBy(x => x.TransactionDate)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));

            foreach (EquitySummary es in Context.EquitySummaries.OrderBy(x => x.Date))
            {
                if (Math.Abs(es.Total - 0) > 0.00001m)
                {
                    if (!first)
                    {
                        decimal externalCashFlow = depositsWithdrawals.ContainsKey(es.Date) ? depositsWithdrawals[es.Date] : 0;

                        if (lastTotal != 0)
                        //make sure we avoid division by zero...equity can't change if we have 0 to start with anyway
                        {
                            rets.Add((double)(((es.Total - externalCashFlow) - lastTotal) / lastTotal));
                            equity *= 1 + rets.Last();
                        }

                        maxEquity = Math.Max(equity, maxEquity);

                        ec.Add(equity);
                        returnsPoints.Add(new AreaPoint(es.Date, equity - 1, 0));
                        drawdownPoints.Add(new AreaPoint(es.Date, equity / maxEquity - 1, 0));
                    }
                    else
                    {
                        ec.Add(1);
                        first = false;
                    }
                }
                lastTotal = es.Total;
            }

            EquitySummaryPercentSeries = returnsPoints;
            EquitySummaryPercentDrawdownSeries = drawdownPoints;
        }

        private DataTable GeneratePerformanceOverviewStats()
        {
            var statsDT = new DataTable();
            statsDT.Columns.Add("Stat", typeof(string));
            statsDT.Columns.Add("Last 30 Days", typeof(string));
            statsDT.Columns.Add("YTD", typeof(string));
            statsDT.Columns.Add("All Time", typeof(string));

            var allEquitySummaries = Context
                                        .EquitySummaries
                                        .OrderBy(x => x.Date)
                                        .ToList();

            Dictionary<DateTime, decimal> depositsWithdrawals =
                Context
                .CashTransactions
                .Where(x => x.Type == "Deposits & Withdrawals")
                .ToList()
                .GroupBy(x => x.TransactionDate)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));

            var last30DaysEC = EcFromEquitySummaries(
                allEquitySummaries.Where(x => x.Date >= DateTime.Now.AddMonths(-1)).ToList(),
                depositsWithdrawals);

            var ytdEC = EcFromEquitySummaries(
                allEquitySummaries.Where(x => x.Date.Year == DateTime.Now.Year).ToList(),
                depositsWithdrawals);

            var allTimeEC = EcFromEquitySummaries(allEquitySummaries, depositsWithdrawals);

            var last30DayStats = PerformanceMeasurement.EquityCurveStats(last30DaysEC, 30);
            var ytdStats = PerformanceMeasurement.EquityCurveStats(ytdEC, DateTime.Now.DayOfYear);
            var allTimeStats = PerformanceMeasurement.EquityCurveStats(allTimeEC,
                (int)(allEquitySummaries.Last().Date - allEquitySummaries.First().Date).TotalDays);

            foreach (var kvp in last30DayStats)
            {
                var dr = statsDT.NewRow();
                dr["Stat"] = kvp.Key;
                dr["Last 30 Days"] = kvp.Value;

                if (ytdStats.ContainsKey(kvp.Key))
                    dr["YTD"] = ytdStats[kvp.Key];

                dr["All Time"] = allTimeStats[kvp.Key];
                statsDT.Rows.Add(dr);
            }
            return statsDT;
        }

        private EquityCurve EcFromEquitySummaries(List<EquitySummary> summaries, Dictionary<DateTime, decimal> depositsWithdrawals)
        {
            if (summaries.Count == 0) return new EquityCurve();

            var ec = new EquityCurve();
            decimal lastTotal = summaries[0].Total;
            for (int i = 1; i < summaries.Count; i++)
            {
                var es = summaries[i];
                decimal externalCashFlow = depositsWithdrawals.ContainsKey(es.Date) ? depositsWithdrawals[es.Date] : 0;

                if (lastTotal != 0)
                //make sure we avoid division by zero...equity can't change if we have 0 to start with anyway
                {
                    double ret = (double)(((es.Total - externalCashFlow) - lastTotal) / lastTotal);
                    ec.AddReturn(ret, es.Date);
                }
                lastTotal = es.Total;
            }
            ec.CalcFinalValues(summaries.Last().Date);
            return ec;
        }
    }
}