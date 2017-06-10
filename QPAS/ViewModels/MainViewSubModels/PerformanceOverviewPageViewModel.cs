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
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

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
        private Currency _selectedCurrency;

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

        public PerformanceOverviewPageViewModel(IDBContext context, IDialogCoordinator dialogService)
            : base(dialogService)
        {
            Context = context;
            Accounts = new ObservableCollection<Account>();
            Accounts.Add(new Account { ID = -1, AccountId = "All" });

            Currencies = new ObservableCollection<Currency>(context.Currencies.ToList());
            SelectedCurrency = Currencies.FirstOrDefault(x => x.Name == "USD");

            SelectedAccount = Accounts.First();

            this.WhenAnyValue(x => x.SelectedAccount)
                .Subscribe(async _ => await Refresh().ConfigureAwait(true));

            this.WhenAny(x => x.SelectedCurrency, x => x)
                .Subscribe(async _ => await Refresh().ConfigureAwait(true));
        }

        public override async Task Refresh()
        {
            Context.EquitySummaries.OrderBy(x => x.Date).Load();

            //Add any accounts that exist in the db but are missing here
            var tmpAccounts = await Context.Accounts.ToListAsync().ConfigureAwait(true);
            var newAccounts = tmpAccounts.Except(Accounts, new LambdaEqualityComparer<Account>((x, y) => x.ID == y.ID));
            Accounts.AddRange(newAccounts);

            if (SelectedAccount == null) return;
            await CreatePctReturnSeries().ConfigureAwait(true);
            await CreateTotalEquitySeries().ConfigureAwait(true);
            Stats = await GeneratePerformanceOverviewStats().ConfigureAwait(true);
        }

        private async Task CreateTotalEquitySeries()
        {
            var equityPoints = new List<AreaPoint>();
            var drawdownPoints = new List<AreaPoint>();

            decimal maxEquity = 0;

            foreach (var kvp in await GetTotalCapitalSeries().ConfigureAwait(true))
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

        private async Task CreatePctReturnSeries()
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
            Dictionary<DateTime, decimal> depositsWithdrawals = await GetDepositsWithdrawals().ConfigureAwait(true);
            Dictionary<DateTime, decimal> totalCapital = await GetTotalCapitalSeries().ConfigureAwait(true);
            
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

        private async Task<Dictionary<DateTime, decimal>> GetTotalCapitalSeries()
        {
            Dictionary<DateTime, decimal> totalCapital;
            if (SelectedAccount.AccountId == "All")
            {
                totalCapital = await Context
                    .EquitySummaries
                    .GroupBy(x => x.Date)
                    .OrderBy(x => x.Key)
                    .ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.Total)).ConfigureAwait(true);
            }
            else
            {
                totalCapital = await Context
                    .EquitySummaries
                    .Where(x => x.AccountID == SelectedAccount.ID)
                    .OrderBy(x => x.Date)
                    .ToDictionaryAsync(x => x.Date, x => x.Total).ConfigureAwait(true);
            }

            return await PerformCurrencyAdjustment(totalCapital).ConfigureAwait(true);
        }

        private async Task<Dictionary<DateTime, decimal>> PerformCurrencyAdjustment(Dictionary<DateTime, decimal> inputSeries)
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
                    Utils.TimeSeriesFromFXRates(await Context.FXRates.Where(x => x.FromCurrencyID == _selectedCurrency.ID).OrderBy(x => x.Date).ToListAsync().ConfigureAwait(true));

                Dictionary<DateTime, decimal> newSeries = new Dictionary<DateTime, decimal>();

                foreach(var kvp in inputSeries)
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

        private async Task<Dictionary<DateTime, decimal>> GetDepositsWithdrawals()
        {
            Dictionary<DateTime, decimal> depositsWithdrawals;
            if (SelectedAccount.AccountId == "All")
            {
                depositsWithdrawals = (await Context
                        .CashTransactions
                        .Where(x => x.Type == "Deposits & Withdrawals")
                        .ToListAsync().ConfigureAwait(true))
                    .GroupBy(x => x.TransactionDate)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));
            }
            else
            {
                depositsWithdrawals = (await Context
                        .CashTransactions
                        .Where(x => x.AccountID == SelectedAccount.ID)
                        .Where(x => x.Type == "Deposits & Withdrawals")
                        .ToListAsync().ConfigureAwait(true))
                    .GroupBy(x => x.TransactionDate)
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount * y.FXRateToBase));
            }

            return await PerformCurrencyAdjustment(depositsWithdrawals).ConfigureAwait(true);
        }

        private async Task<DataTable> GeneratePerformanceOverviewStats()
        {
            var statsDT = new DataTable();
            statsDT.Columns.Add("Stat", typeof(string));
            statsDT.Columns.Add("Last 30 Days", typeof(string));
            statsDT.Columns.Add("YTD", typeof(string));
            statsDT.Columns.Add("All Time", typeof(string));

            Dictionary<DateTime, decimal> depositsWithdrawals = await GetDepositsWithdrawals().ConfigureAwait(true);

            Dictionary<DateTime, decimal> totalCapital = await GetTotalCapitalSeries().ConfigureAwait(true);
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
            foreach(var kvp in summaries)
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