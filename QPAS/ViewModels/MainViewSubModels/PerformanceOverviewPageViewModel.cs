// -----------------------------------------------------------------------
// <copyright file="PerformanceOverviewPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QPAS
{
    public class PerformanceOverviewPageViewModel : ViewModelBase
    {
        private List<AreaPoint> _equitySummaryPercentSeries;
        private List<AreaPoint> _equitySummaryPercentDrawdownSeries;
        private List<AreaPoint> _totalEquitySeries;
        private List<AreaPoint> _totalEquityDrawdownSeries;
        private DataTable _stats;
        private Account _selectedAccount;
        private Currency _selectedCurrency;
        private readonly IContextFactory _contextFactory;
        private readonly IAppSettings _settings;
        private readonly DataContainer _data;

        public List<AreaPoint> EquitySummaryPercentSeries
        {
            get => _equitySummaryPercentSeries;
            set => this.RaiseAndSetIfChanged(ref _equitySummaryPercentSeries, value);
        }

        public List<AreaPoint> EquitySummaryPercentDrawdownSeries
        {
            get => _equitySummaryPercentDrawdownSeries;
            set => this.RaiseAndSetIfChanged(ref _equitySummaryPercentDrawdownSeries, value);
        }

        public List<AreaPoint> TotalEquitySeries
        {
            get => _totalEquitySeries;
            set => this.RaiseAndSetIfChanged(ref _totalEquitySeries, value);
        }

        public List<AreaPoint> TotalEquityDrawdownSeries
        {
            get => _totalEquityDrawdownSeries;
            set => this.RaiseAndSetIfChanged(ref _totalEquityDrawdownSeries, value);
        }

        public DataTable Stats
        {
            get => _stats;
            set => this.RaiseAndSetIfChanged(ref _stats, value);
        }

        public Account SelectedAccount
        {
            get => _selectedAccount;
            set => this.RaiseAndSetIfChanged(ref _selectedAccount, value);
        }

        public Currency SelectedCurrency
        {
            get => _selectedCurrency;
            set => this.RaiseAndSetIfChanged(ref _selectedCurrency, value);
        }

        public ObservableCollection<Currency> Currencies { get; set; }

        public ObservableCollection<Account> Accounts { get; set; }

        public PerformanceOverviewPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, IAppSettings settings, DataContainer data)
            : base(dialogService)
        {
            _contextFactory = contextFactory;
            _settings = settings;
            _data = data;

            Accounts = new ObservableCollection<Account>();
            Accounts.Add(new Account { ID = -1, AccountId = "All" });
            Accounts.AddRange(_data.Accounts);

            Currencies = _data.Currencies;
            SelectedCurrency = Currencies.FirstOrDefault(x => x.Name == "USD");

            SelectedAccount = Accounts.First();

            this.WhenAnyValue(x => x.SelectedAccount)
                .Subscribe(async _ => await Refresh());

            this.WhenAny(x => x.SelectedCurrency, x => x)
                .Subscribe(async _ => await Refresh());
        }

        public override async Task Refresh()
        {
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
                            double todaysRet = (double)(((total - externalCashFlow) - lastTotal) / lastTotal);
                            rets.Add(todaysRet);
                            equity *= 1 + todaysRet;
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
            Dictionary<DateTime, decimal> totalCapital;
            if (SelectedAccount.AccountId == "All")
            {
                totalCapital = _data
                    .EquitySummaries
                    .GroupBy(x => x.Date)
                    .OrderBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Total));
            }
            else
            {
                totalCapital = _data
                    .EquitySummaries
                    .Where(x => x.AccountID == SelectedAccount.ID)
                    .OrderBy(x => x.Date)
                    .ToDictionary(x => x.Date, x => x.Total);
            }

            return PerformCurrencyAdjustment(totalCapital);
        }

        private Dictionary<DateTime, decimal> PerformCurrencyAdjustment(Dictionary<DateTime, decimal> inputSeries)
        {
            if (SelectedCurrency == null || SelectedCurrency.Name == "USD")
            {
                //no fx adjustment needed, return
                return inputSeries;
            }
            else
            {
                //Grab fx data and move the timeseries to the start of the equity data
                TimeSeries fxRates =
                    Utils.TimeSeriesFromFXRates(_data.FXRates.Where(x => x.FromCurrencyID == _selectedCurrency.ID).OrderBy(x => x.Date).ToList());

                Dictionary<DateTime, decimal> newSeries = new Dictionary<DateTime, decimal>();

                foreach (var kvp in inputSeries)
                {
                    //move fx rate series to the time of the current item from the input series
                    fxRates.ProgressTo(kvp.Key);

                    //precaution to make sure we actually have data for this point
                    decimal fxRate = fxRates.CurrentBar >= 0 ? fxRates[0].Close : 1;

                    newSeries.Add(kvp.Key, kvp.Value / fxRate);
                }

                return newSeries;
            }
        }

        private Dictionary<DateTime, decimal> GetDepositsWithdrawals()
        {
            Dictionary<DateTime, decimal> depositsWithdrawals;
            if (SelectedAccount.AccountId == "All")
            {
                depositsWithdrawals = (_data
                        .CashTransactions
                        .Where(x => x.Type == "Deposits & Withdrawals")
                        .ToList())
                    .GroupBy(x => x.TransactionDate)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));
            }
            else
            {
                depositsWithdrawals = (_data
                        .CashTransactions
                        .Where(x => x.AccountID == SelectedAccount.ID)
                        .Where(x => x.Type == "Deposits & Withdrawals")
                        .ToList())
                    .GroupBy(x => x.TransactionDate)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));
            }

            return PerformCurrencyAdjustment(depositsWithdrawals);
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

            var last30DayStats = PerformanceMeasurement.EquityCurveStats(last30DaysEC, 30, _settings.AssumedInterestRate);
            var ytdStats = PerformanceMeasurement.EquityCurveStats(ytdEC, DateTime.Now.DayOfYear, _settings.AssumedInterestRate);
            var allTimeStats = PerformanceMeasurement.EquityCurveStats(allTimeEC,
                (int)(totalCapital.Last().Key - totalCapital.First().Key).TotalDays, _settings.AssumedInterestRate);

            foreach (var kvp in allTimeStats)
            {
                var dr = statsDT.NewRow();
                dr["Stat"] = kvp.Key;

                if (last30DayStats.ContainsKey(kvp.Key))
                    dr["Last 30 Days"] = last30DayStats[kvp.Key];

                if (ytdStats.ContainsKey(kvp.Key))
                    dr["YTD"] = ytdStats[kvp.Key];

                dr["All Time"] = allTimeStats[kvp.Key];
                statsDT.Rows.Add(dr);
            }
            return statsDT;
        }

        private EquityCurve EcFromEquitySummaries(Dictionary<DateTime, decimal> summaries, Dictionary<DateTime, decimal> depositsWithdrawals)
        {
            if (summaries.Count == 0) return new EquityCurve(100, DateTime.Now);

            var ec = new EquityCurve(100, summaries.First().Key);
            decimal lastTotal = summaries.First().Value;
            bool first = true;
            foreach (var kvp in summaries)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
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