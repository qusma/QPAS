// -----------------------------------------------------------------------
// <copyright file="PerformanceOverviewPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
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
        private Account _selectedAccount;

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

        public Account SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                if (Equals(value, _selectedAccount)) return;
                _selectedAccount = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        public ObservableCollection<Account> Accounts { get; set; }

        public PerformanceOverviewPageViewModel(IDBContext context, IDialogService dialogService)
            : base(dialogService)
        {
            Context = context;
            Accounts = new ObservableCollection<Account>();
            Accounts.Add(new Account { ID = -1, AccountId = "All" });

            SelectedAccount = Accounts.First();
        }

        public override void Refresh()
        {
            Context.EquitySummaries.OrderBy(x => x.Date).Load();

            //Add any accounts that exist in the db but are missing here
            var tmpAccounts = Context.Accounts.ToList();
            var newAccounts = tmpAccounts.Except(Accounts, new LambdaEqualityComparer<Account>((x, y) => x.ID == y.ID));
            Accounts.AddRange(newAccounts);

            if (SelectedAccount == null) return;
            CreatePctReturnSeries();
            CreateTotalEquitySeries();
            Stats = GeneratePerformanceOverviewStats();
        }

        private void CreateTotalEquitySeries()
        {
            var equityPoints = new List<AreaPoint>();
            var drawdownPoints = new List<AreaPoint>();

            decimal maxEquity = 0;

            foreach (var kvp in GetTotalCapitalSeries())
            {
                DateTime date = kvp.Key;
                decimal total = kvp.Value;
                equityPoints.Add(new AreaPoint(date, 0, (double)total));
                maxEquity = Math.Max(maxEquity, total);
                drawdownPoints.Add(new AreaPoint(date, 0, (double)(total - maxEquity)));
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
            Dictionary<DateTime, decimal> depositsWithdrawals = GetDepositsWithdrawals();

            Dictionary<DateTime, decimal> totalCapital = GetTotalCapitalSeries();

            foreach (var es in totalCapital)
            {
                decimal total = es.Value;
                DateTime date = es.Key;

                if (Math.Abs(total - 0) > 0.00001m)
                {
                    if (!first)
                    {
                        decimal externalCashFlow = depositsWithdrawals.ContainsKey(date) ? depositsWithdrawals[date] : 0;

                        if (lastTotal != 0)
                        //make sure we avoid division by zero...equity can't change if we have 0 to start with anyway
                        {
                            rets.Add((double)(((total - externalCashFlow) - lastTotal) / lastTotal));
                            equity *= 1 + rets.Last();
                        }

                        maxEquity = Math.Max(equity, maxEquity);

                        ec.Add(equity);
                        returnsPoints.Add(new AreaPoint(date, equity - 1, 0));
                        drawdownPoints.Add(new AreaPoint(date, equity / maxEquity - 1, 0));
                    }
                    else
                    {
                        ec.Add(1);
                        first = false;
                    }
                }
                lastTotal = total;
            }

            EquitySummaryPercentSeries = returnsPoints;
            EquitySummaryPercentDrawdownSeries = drawdownPoints;
        }

        private Dictionary<DateTime, decimal> GetTotalCapitalSeries()
        {
            if (SelectedAccount.AccountId == "All")
            {
                return Context
                    .EquitySummaries
                    .GroupBy(x => x.Date)
                    .OrderBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
            }
            else
            {
                return Context
                    .EquitySummaries
                    .Where(x => x.AccountID == SelectedAccount.ID)
                    .OrderBy(x => x.Date)
                    .ToDictionary(x => x.Date, x => x.Total);
            }
        }

        private Dictionary<DateTime, decimal> GetDepositsWithdrawals()
        {
            if (SelectedAccount.AccountId == "All")
            {
                return Context
                    .CashTransactions
                    .Where(x => x.Type == "Deposits & Withdrawals")
                    .ToList()
                    .GroupBy(x => x.TransactionDate)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));
            }
            else
            {
                return Context
                    .CashTransactions
                    .Where(x => x.AccountID == SelectedAccount.ID)
                    .Where(x => x.Type == "Deposits & Withdrawals")
                    .ToList()
                    .GroupBy(x => x.TransactionDate)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));
            }
        }

        private DataTable GeneratePerformanceOverviewStats()
        {
            var statsDT = new DataTable();
            statsDT.Columns.Add("Stat", typeof(string));
            statsDT.Columns.Add("Last 30 Days", typeof(string));
            statsDT.Columns.Add("YTD", typeof(string));
            statsDT.Columns.Add("All Time", typeof(string));

            Dictionary<DateTime, decimal> depositsWithdrawals = GetDepositsWithdrawals();

            Dictionary<DateTime, decimal> totalCapital = GetTotalCapitalSeries();
            if (totalCapital.Count == 0) return statsDT;

            var last30DaysEC = EcFromEquitySummaries(
                totalCapital.Where(x => x.Key >= DateTime.Now.AddMonths(-1)).ToDictionary(x => x.Key, x => x.Value),
                depositsWithdrawals);

            var ytdEC = EcFromEquitySummaries(
                totalCapital.Where(x => x.Key.Year == DateTime.Now.Year).ToList().ToDictionary(x => x.Key, x => x.Value),
                depositsWithdrawals);

            var allTimeEC = EcFromEquitySummaries(totalCapital, depositsWithdrawals);

            var last30DayStats = PerformanceMeasurement.EquityCurveStats(last30DaysEC, 30);
            var ytdStats = PerformanceMeasurement.EquityCurveStats(ytdEC, DateTime.Now.DayOfYear);
            var allTimeStats = PerformanceMeasurement.EquityCurveStats(allTimeEC,
                (int)(totalCapital.Last().Key - totalCapital.First().Key).TotalDays);

            foreach (var kvp in allTimeStats)
            {
                var dr = statsDT.NewRow();
                dr["Stat"] = kvp.Key;

                if(last30DayStats.ContainsKey(kvp.Key))
                    dr["Last 30 Days"] = kvp.Value;

                if (ytdStats.ContainsKey(kvp.Key))
                    dr["YTD"] = ytdStats[kvp.Key];

                dr["All Time"] = allTimeStats[kvp.Key];
                statsDT.Rows.Add(dr);
            }
            return statsDT;
        }

        private EquityCurve EcFromEquitySummaries(Dictionary<DateTime, decimal> summaries, Dictionary<DateTime, decimal> depositsWithdrawals)
        {
            if (summaries.Count == 0) return new EquityCurve();

            var ec = new EquityCurve();
            decimal lastTotal = summaries.First().Value;
            foreach(var kvp in summaries)
            {
                DateTime date = kvp.Key;
                decimal total = kvp.Value;
                decimal externalCashFlow = depositsWithdrawals.ContainsKey(date) ? depositsWithdrawals[date] : 0;

                if (lastTotal != 0)
                //make sure we avoid division by zero...equity can't change if we have 0 to start with anyway
                {
                    double ret = (double)(((total - externalCashFlow) - lastTotal) / lastTotal);
                    ec.AddReturn(ret, date);
                }
                lastTotal = total;
            }
            ec.CalcFinalValues(summaries.Last().Key);
            return ec;
        }
    }
}