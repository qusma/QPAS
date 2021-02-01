// -----------------------------------------------------------------------
// <copyright file="ReportGenerator.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using NLog;
using QDMS;
using QPAS.DataSets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Instrument = EntityModel.Instrument;
using Tag = EntityModel.Tag;
using ReportSettings = EntityModel.ReportSettings;

namespace QPAS
{
    public class ReportGenerator : IDisposable
    {
        private PortfolioTracker _totalPortfolioTracker;

        private filterReportDS ds;
        private List<Trade> _trades;
        private DateTime _todaysDate;

        private List<DateTime> _datesInPeriod;
        private List<decimal> _capitalInPeriod;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Key: strategy name
        /// </summary>
        private Dictionary<string, PortfolioTracker> _strategyPfolioTrackers;

        //Benchmark
        private EquityCurve _benchmarkEC;

        private Dictionary<DateTime, double> _benchmarkSeries;

        private List<double> _benchmarkReturns;

        //Backtest
        private EquityCurve _backtestEC;


        private DateTime _minDate;
        private DateTime _maxDate;

        private ProgressDialogController _progressDialog;
        private ReportSettings _settings;

        public IQpasDbContext Context { get; private set; }

        public void Dispose()
        {
            if (ds != null)
            {
                ds.Dispose();
                ds = null;
            }
        }

        /// <summary>
        /// Set the progress dialog stuff.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="progress">Progress: from 0 to 100</param>
        private void SetProgress(string message, double progress)
        {
            if (_progressDialog != null)
            {
                _progressDialog.SetMessage(message);
                _progressDialog.SetProgress(progress / 100);
            }
        }

        private IEnumerable<Tuple<DateTime, decimal>> GetEquity(IAppSettings settings)
        {
            IEnumerable<Tuple<DateTime, decimal>> equitySums;
            if (settings.TotalCapitalAlwaysUsesAllAccounts)
            {
                //total equity is the equity across all accounts, no matter if they were used in the selected trades or not
                equitySums =
                    Context
                    .EquitySummaries
                    .Where(x => x.Date >= _minDate.Date && x.Date <= _maxDate.Date)
                    .OrderBy(x => x.Date)
                    .AsEnumerable() //can't exeute groupby on the db for some reason, this makes it client-side
                    .GroupBy(x => x.Date)
                    .ToList()
                    .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.Total)));
            }
            else
            {
                //total equity figure only includes accounts that these trades belong to.
                //Here we figure out which accounts the selected items actually belong to
                List<int> accountIDs =
                    _trades
                    .Where(x => x.Orders != null)
                    .SelectMany(x => x.Orders)
                    .Where(x => x.AccountID.HasValue)
                    .Select(x => x.AccountID.Value)
                    .ToList();

                accountIDs.AddRange(
                    _trades
                        .Where(x => x.CashTransactions != null)
                        .SelectMany(x => x.Orders)
                        .Where(x => x.AccountID.HasValue)
                        .Select(x => x.AccountID.Value));

                accountIDs.AddRange(
                    _trades
                        .Where(x => x.FXTransactions != null)
                        .SelectMany(x => x.Orders)
                        .Where(x => x.AccountID.HasValue)
                        .Select(x => x.AccountID.Value));

                accountIDs = accountIDs.Distinct().ToList();

                //grab dates in period and total capital at each of them           
                //What we do here is the following: grab the equity summary for all accounts for the specified dates
                //then filter the selected accounts
                //and finally determine total capital for each day by summing up all the equitysummaries' Total field at that date
                equitySums =
                    Context
                    .EquitySummaries
                    .Where(x => x.Date >= _minDate.Date && x.Date <= _maxDate.Date && x.AccountID.HasValue)
                    .OrderBy(x => x.Date)
                    .Select(x => new { x.AccountID, x.Date, x.Total })
                    .ToList()
                    .Where(x => accountIDs.Contains(x.AccountID.Value))
                    .GroupBy(x => x.Date)
                    .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.Total)));
            }

            return equitySums;
        }

        /// <summary>
        /// Fills a filterReportDS DataSet with trading statistics.
        /// </summary>
        /// <param name="tradeIDs">The IDs of the trades that are to be included.</param>
        /// <param name="settings"></param>
        /// <param name="datasourcer"></param>
        /// <param name="progressDialog"></param>
        public async Task<filterReportDS> TradeStats(List<Trade> trades, ReportSettings settings, IAppSettings appSettings, IDataSourcer datasourcer, IContextFactory contextFactory, EquityCurve backtestData = null, ProgressDialogController progressDialog = null)
        {
            _settings = settings;
            _progressDialog = progressDialog;
            ds = new filterReportDS();

            SetProgress("Loading Data", 0);
            Context = contextFactory.Get();


            _trades = trades;
            if (_trades.Count == 0) return ds;

            _trades = _trades.OrderBy(x => x.DateOpened).ToList();

            //first + last relevant date
            _minDate = _trades.Min(x => x.DateOpened);
            //if any trades are still open, last date is the last date with an equity summary
            _maxDate = _trades.Any(x => x.Open)
                ? Context.EquitySummaries.Max(x => x.Date)
                : _trades.Max(x => x.DateClosed ?? x.DateOpened);

            //Get the dates and total equity figures
            var equitySums = GetEquity(appSettings);

            _capitalInPeriod = equitySums.Select(x => x.Item2).ToList();
            _datesInPeriod = equitySums.Select(x => x.Item1).ToList();

            if (_datesInPeriod.Count == 0)
            {
                throw new Exception("No equity summaries found for the selected period");
            }

            //grab the relevant instrument data...
            Dictionary<int, TimeSeries> data = await AcquireInstrumentData(datasourcer);

            //Grab FX data
            var fxData = AcquireFXData();

            //Grab backtest data
            await AcquireBacktestData(datasourcer, backtestData);

            //also get the benchmark values for the period
            if (settings.Benchmark != null)
            {
                (_benchmarkEC, _benchmarkSeries, _benchmarkReturns) =
                    await BenchmarkBuilder.GetBenchmarkReturns(settings.Benchmark.ID, contextFactory, _datesInPeriod, datasourcer).ConfigureAwait(false);
            }

            //start up the portfolio trackers
            _totalPortfolioTracker = new PortfolioTracker(data, fxData, _trades, "Total Pfolio", _datesInPeriod.First(), appSettings.OptionsCapitalUsageMultiplier);

            //tracker per-strategy
            var distinctStrats = _trades.Select(x => x.Strategy).Distinct().ToList();
            _strategyPfolioTrackers = distinctStrats
                .ToDictionary(
                    x => x.Name,
                    x => new PortfolioTracker(data, fxData, _trades.Where(t => t.StrategyID == x.ID).ToList(), x.Name, _datesInPeriod.First(), appSettings.OptionsCapitalUsageMultiplier));


            //then we do the calcs
            //the capital in use in one day is calculated as the capital that was in use at the end of the previous day
            //plus any positions opened today before an arbitrary cut-off point (for now I'll use 15:40:00 ET)
            //anything before that is considered to be an intraday position and should have this day's returns included in the calculations
            //anything after that is a position considered to be initiated "at the close" and thus not included in today's returns calculations
            TimeSpan cutoffTime = new TimeSpan(15, 55, 0);

            //capture data to be stored here
            var instrumentUpGross = new Dictionary<string, double>();
            var instrumentDnGross = new Dictionary<string, double>();
            var instrumentUpCaptured = new Dictionary<string, double>();
            var instrumentDnCaptured = new Dictionary<string, double>();
            var instrumentUpLost = new Dictionary<string, double>();
            var instrumentDnLost = new Dictionary<string, double>();


            //then we start doing the actual work..
            for (int i = 0; i < _datesInPeriod.Count; i++)
            {
                if (i % 10 == 0)
                {
                    SetProgress("Simulating Trades", 20 + ((double)i / _datesInPeriod.Count) * 75);
                }

                _todaysDate = _datesInPeriod[i];
                decimal todaysTotalCapital = _capitalInPeriod[Math.Max(0, i - 1)];

                //send all the timeseries to the right date
                foreach (TimeSeries t in data.Values)
                {
                    t.ProgressTo(_todaysDate);
                }

                foreach (TimeSeries t in fxData.Values)
                {
                    t.ProgressTo(_todaysDate);
                }

                //then add all the day's orders, cash transactions, etc.
                _totalPortfolioTracker.ProcessItemsAt(_todaysDate);
                foreach (PortfolioTracker pt in _strategyPfolioTrackers.Values)
                {
                    pt.ProcessItemsAt(_todaysDate);
                }

                //update portfolio trackers at the end of the day, calculating curves, etc.
                _totalPortfolioTracker.OnDayClose(_todaysDate, todaysTotalCapital);
                foreach (PortfolioTracker pt in _strategyPfolioTrackers.Values)
                {
                    pt.OnDayClose(_todaysDate, todaysTotalCapital);
                }

                AddDollarEquityCurveRow();

                //daily return on total capital equity curve
                AddDailyEquityCurvePctOnTotalCapitalRow();

                //daily return on allocated capital equity curve
                AddROACEquityCurveRow();
            } ///////////////////END OF MAIN LOOP!

            SetProgress("Calculating Statistics", 95);

            //generate stats and stuff
            ProcessResults(settings, appSettings);

            Context.Dispose();

            return ds;
        }

        private async Task AcquireBacktestData(IDataSourcer datasourcer, EquityCurve backtestData)
        {
            if (_settings.BacktestSource == BacktestSource.None)
            {
                return;
            }
            else if (_settings.BacktestSource == BacktestSource.External && _settings.BacktestExternalInstrumentId != null)
            {
                var data = await datasourcer.ExternalDataSource.GetData(_settings.BacktestExternalInstrumentId.Value, new DateTime(1950, 1, 1), DateTime.Now, BarSize.OneDay);
                if (data == null || data.Count == 0)
                {
                    _logger.Log(LogLevel.Error, "Could not retrieve backtest data.");
                    return;
                }

                _backtestEC = new EquityCurve(100, data[0].Date.ToDateTime());
                for (int i = 1; i < data.Count; i++)
                {
                    _backtestEC.AddReturn((double)(data[i].Close / data[i - 1].Close - 1), data[i].Date.ToDateTime());
                }
            }
            else if (_settings.BacktestSource == BacktestSource.File && backtestData != null)
            {
                _backtestEC = backtestData;
            }
        }

        private Dictionary<int, TimeSeries> AcquireFXData()
        {
            List<int> fxIDs = GetNeededFxIDs().ToList();

            var fxData = new Dictionary<int, TimeSeries>();
            int counter = 0;
            foreach (int id in fxIDs)
            {
                if (id == 1) continue;

                int id1 = id;
                fxData.Add(id, Utils.TimeSeriesFromFXRates(Context.FXRates.Where(x => x.FromCurrencyID == id1).OrderBy(x => x.Date)));

                SetProgress(string.Format("Loading FX Data ({0}/{1})", counter, fxIDs.Count),
                    15 + 5 * ((double)counter / (fxIDs.Count)));
                counter++;
            }

            return fxData;
        }

        private async Task<Dictionary<int, TimeSeries>> AcquireInstrumentData(IDataSourcer datasourcer)
        {
            //determine what dates we need each instrument for
            //Instrument id - from date/to date
            Dictionary<Instrument, KeyValuePair<DateTime, DateTime>> neededDates = GetNeededDates();

            var data = new Dictionary<int, TimeSeries>();
            int counter = 0;
            foreach (var kvp in neededDates)
            {
                Instrument instrument = kvp.Key;
                DateTime startingDate = kvp.Value.Key;
                DateTime endingDate = kvp.Value.Value;

                var series = await datasourcer.GetData(instrument, startingDate, endingDate);
                if (series == null || series.Count == 0)
                {
                    //couldn't find data at qdms, use prior period positions
                    var pos = Context.PriorPositions.Where(x => x.InstrumentID == kvp.Key.ID).OrderBy(x => x.Date).ToList();
                    data.Add(kvp.Key.ID, TimeSeriesFromPriorPositions(pos));
                    _logger.Log(LogLevel.Warn, string.Format("Data for instrument {0} ({1}) not found from QDMS. Using prior positions instead.", kvp.Key.Symbol, kvp.Key.AssetCategory));
                }
                else
                {
                    data.Add(kvp.Key.ID, new TimeSeries(series));
                }

                if (counter % 5 == 0)
                {
                    SetProgress(string.Format("Loading Data ({0}/{1})", counter, neededDates.Count),
                        15 * ((double)counter / (neededDates.Count)));
                }
                counter++;
            }

            return data;
        }

        /// <summary>
        /// Takes the data generated, possibly transoforms it, and sticks it into various datatables.
        /// </summary>
        private void ProcessResults(ReportSettings settings, IAppSettings appSettings)
        {
            //capital usage
            DoCapitalUsage();

            //add the monthly returns to the datatables and also calculate the total annual returns
            AddMonthlyPnLRows();
            AddMonthlyROACRows();
            AddMonthlyROTCRowS();

            //close to close pnl curve
            DoCloseToClosePnLCurve();

            //cash transaction type PnL
            DoCashTransactionByTypeRows();

            //Trade stats
            DoTradeStats();
            DoTradeStatsByStrategy();

            //MAE/MFE
            DoMAEMFE();

            //generate the daily ROAC and ROTC stats, also benchmark stats
            DoPortfolioStats(appSettings);

            //PnLByInstrument
            DoPnLByInstrument(_totalPortfolioTracker.Positions.Values);

            //instrument ROAC
            AddInstrumentROACRows();

            //Profit/loss by strategy
            AddStrategyPLCurves();

            //Capital Usage by strategy
            AddCapitalUsageByStrategy();

            //ROAC by strategy
            DoStrategyROACCurves();

            //strategy ROAC covariance matrix
            DoStrategyROACCovMatrix();

            //Strategy ROAC MDS
            DoStrategyROACMds();

            //up/dn capture stats

            //var captureDR = ds.captureStats.NewcaptureStatsRow();
            //captureDR.ticker = symbol;
            //captureDR.upsideCaptured = instrumentUpCaptured[symbol] / grossUp;
            //captureDR.upsideLost = instrumentUpLost[symbol] / grossUp;
            //captureDR.grossUpside = grossUp;
            //captureDR.upsideMissed = (grossUp - instrumentUpCaptured[symbol] - instrumentUpLost[symbol]) / grossUp;

            //captureDR.downsideCaptured = instrumentDnCaptured[symbol] / grossDn;
            //captureDR.downsideLost = instrumentDnLost[symbol] / grossDn;
            //captureDR.grossDownside = grossDn;
            //captureDR.downsideMissed = (grossDn - instrumentDnCaptured[symbol] - instrumentDnLost[symbol]) / grossDn;

            //ds.captureStats.AddcaptureStatsRow(captureDR);

            //trade result histograms
            AddTradeResultHistogramRows();

            //holding period histogram
            AddHoldingPeriodHistogramRows();

            //daily return histograms
            AddDailyReturnHistogramRows();

            //trade size vs % return scatter plot
            AddTradeSizeVsRetPlotrows();

            //trade length vs % return scatter plot
            AddTradeLengthVsRetPlotRows();

            //benchmarking stats
            if (_benchmarkReturns != null)
            {
                DoBenchmarkStats(settings.ReturnsToBenchmark, appSettings.AssumedInterestRate);
            }

            //rolling alpha/beta
            if (_benchmarkReturns != null)
            {
                DoRollingAlphaBeta(settings.ReturnsToBenchmark, appSettings.AssumedInterestRate);
            }

            //Profit/loss by tag
            DoPLByTag();

            //Monte Carlo
            DoMonteCarlo(appSettings);

            //Value at Risk
            DoValueAtRisk(settings);
            DoComponentValueAtRisk();

            //Expected shortfall
            DoExpectedShortfall(settings);

            //average cumulative daily trade rets
            DoAverageCumulativeTradeReturns();

            //acf and pacf
            DoAcfPacf(settings);

            //Backtest stuff
            DoBacktestEquityCurves();
            DoBacktestStats(appSettings);
            DoBacktestMonteCarlo();

            //trade stats by opening day/time
            DoTradeStatsByOpeningDayTime();
        }

        private void DoTradeStatsByOpeningDayTime()
        {
            var byDay = _trades.GroupBy(x => x.DateOpened.DayOfWeek);
            var byDayAvgRet = byDay.ToDictionary(x => x.Key, x => x.Any() ? (double?)x.Average(y => y.TotalResultPct) : null);
            var byDayError = byDay.ToDictionary(x => x.Key, x => x.Any() ? GetError(x) : 0);

            foreach (var kv in byDayAvgRet.OrderBy(x => x.Key))
            {
                var row = ds.tradeRetsByDay.NewtradeRetsByDayRow();
                row.weekDay = kv.Key;
                if (kv.Value.HasValue)
                {
                    row.avgRet = kv.Value.Value;
                }
                row.error = byDayError[kv.Key];
                ds.tradeRetsByDay.AddtradeRetsByDayRow(row);
            }

            var byHour = _trades.GroupBy(x => x.DateOpened.Hour);
            var byHourAvgRet = byHour.ToDictionary(x => x.Key, x => x.Any() ? (double?)x.Average(y => y.TotalResultPct) : null);
            var byHourError = byHour.ToDictionary(x => x.Key, x => x.Any() ? GetError(x) : 0);

            foreach (var kv in byHourAvgRet.OrderBy(x => x.Key))
            {
                var row = ds.tradeRetsByHour.NewtradeRetsByHourRow();
                row.hour = kv.Key;
                if (kv.Value.HasValue)
                {
                    row.avgRet = kv.Value.Value;
                }
                row.error = byHourError[kv.Key];
                ds.tradeRetsByHour.AddtradeRetsByHourRow(row);
            }

            var byDayAndHour = _trades.GroupBy(x => new { x.DateOpened.DayOfWeek, x.DateOpened.Hour });
            var byDayAndHourAvgRet = byDayAndHour.ToDictionary(x => x.Key, x => x.Any() ? (double?)x.Average(y => y.TotalResultPct) : null);
            var byDayAndHourError = byDayAndHour.ToDictionary(x => x.Key, x => x.Any() ? GetError(x) : 0);

            //if one hour has zero entries across all days, do not add it
            var hourHasTrades = byDayAndHourAvgRet.GroupBy(x => x.Key.Hour).Where(x => x.Any(y => y.Value.HasValue)).Select(x => x.Key);
            foreach (var kv in byDayAndHourAvgRet.OrderBy(x => x.Key.DayOfWeek).ThenBy(x => x.Key.Hour))
            {
                if (!hourHasTrades.Contains(kv.Key.Hour)) continue;

                var row = ds.tradeRetsByDayAndHour.NewtradeRetsByDayAndHourRow();
                row.weekDay = kv.Key.DayOfWeek;
                row.hour = kv.Key.Hour;
                if (kv.Value.HasValue)
                {
                    row.avgRet = kv.Value.Value;
                }
                row.error = byDayAndHourError[kv.Key];
                ds.tradeRetsByDayAndHour.AddtradeRetsByDayAndHourRow(row);
            }
        }

        private double GetError<T>(IGrouping<T, Trade> trades)
        {
            return 2 * trades.Select(y => y.TotalResultPct).StandardDeviation() / Math.Sqrt(trades.Count());
        }

        private void DoAcfPacf(ReportSettings settings)
        {
            var rets = settings.AutoCorrReturnType == ReturnType.ROAC
                ? _totalPortfolioTracker.RoacEquityCurve.Returns
                : _totalPortfolioTracker.RotcEquityCurve.Returns;

            if (rets.Count < 21) return;
            var acf = MathUtils.AutoCorr(rets, 10);
            var pacf = MathUtils.PartialAutoCorr(rets, 10);

            for (int i = 0; i < 10; i++)
            {
                var dr = ds.ACF.NewACFRow();
                dr.lag = i;
                dr.acf = acf[i];
                ds.ACF.AddACFRow(dr);
            }

            for (int i = 0; i < 10; i++)
            {
                var dr = ds.PACF.NewPACFRow();
                dr.lag = i;
                dr.pacf = pacf[i];
                ds.PACF.AddPACFRow(dr);
            }
        }

        private void DoStrategyROACCurves()
        {
            Dictionary<string, EquityCurve> strategyRoacECs =
                _strategyPfolioTrackers.ToDictionary(x => x.Key, x => x.Value.RoacEquityCurve);

            if (strategyRoacECs.Count == 0) return;

            foreach (string stratName in strategyRoacECs.Keys)
            {
                ds.StrategyROAC.Columns.Add(stratName, typeof(double));
            }

            for (int i = 0; i < _datesInPeriod.Count; i++)
            {
                var dr = ds.StrategyROAC.NewRow();

                dr["Date"] = _datesInPeriod[i]; foreach (var kvp in strategyRoacECs)
                {
                    dr[kvp.Key] = kvp.Value.Equity[i + 1] - 1;
                }

                ds.StrategyROAC.Rows.Add(dr);
            }
        }

        private void DoStrategyROACCovMatrix()
        {
            Dictionary<string, EquityCurve> strategyRoacECs =
                _strategyPfolioTrackers.ToDictionary(x => x.Key, x => x.Value.RoacEquityCurve);

            foreach (string stratName in strategyRoacECs.Keys)
            {
                ds.StrategyCovMatrix.Columns.Add(stratName, typeof(double));
            }

            var correlations = new Dictionary<KeyValuePair<string, string>, double>();

            foreach (string stratName in strategyRoacECs.Keys)
            {
                foreach (string stratName2 in strategyRoacECs.Keys)
                {
                    var key1 = new KeyValuePair<string, string>(stratName, stratName2);
                    if (correlations.ContainsKey(key1)) continue;

                    var key2 = new KeyValuePair<string, string>(stratName2, stratName);

                    double corr = Correlation.Pearson(strategyRoacECs[stratName].Returns, strategyRoacECs[stratName2].Returns);
                    correlations.Add(key1, corr);

                    if (stratName != stratName2)
                        correlations.Add(key2, corr);
                }
            }

            var keys = new List<string>(strategyRoacECs.Keys);

            //If there is a benchmark, add it to the correlation matrix
            if (_benchmarkEC != null && _benchmarkEC.Returns.Count == strategyRoacECs.First().Value.Returns.Count)
            {
                ds.StrategyCovMatrix.Columns.Add("Benchmark", typeof(double));
                keys.Add("Benchmark");

                foreach (string stratName in strategyRoacECs.Keys)
                {
                    double corr = Correlation.Pearson(strategyRoacECs[stratName].Returns, _benchmarkEC.Returns);
                    var key1 = new KeyValuePair<string, string>(stratName, "Benchmark");
                    var key2 = new KeyValuePair<string, string>("Benchmark", stratName);
                    correlations.Add(key1, corr);
                    correlations.Add(key2, corr);
                }

                correlations.Add(new KeyValuePair<string, string>("Benchmark", "Benchmark"), 1);
            }

            foreach (string stratName in keys)
            {
                var dr = ds.StrategyCovMatrix.NewRow();
                dr["Name"] = stratName;
                foreach (string stratName2 in keys)
                {
                    dr[stratName2] = correlations[new KeyValuePair<string, string>(stratName, stratName2)];
                }

                ds.StrategyCovMatrix.Rows.Add(dr);
            }
        }

        private void DoStrategyROACMds()
        {
            if (_strategyPfolioTrackers.Count <= 2) return;

            try
            {
                Dictionary<string, EquityCurve> strategyRoacECs =
                    _strategyPfolioTrackers.ToDictionary(x => x.Key, x => x.Value.RoacEquityCurve);
                List<string> strategyNames = strategyRoacECs.Keys.ToList();

                if (_benchmarkEC != null && _benchmarkEC.Returns.Count == strategyRoacECs.First().Value.Returns.Count)
                {
                    strategyRoacECs.Add("Benchmark", _benchmarkEC);
                    strategyNames.Add("Benchmark");
                }

                Matrix<double> corr = MathUtils.CorrelationMatrix(strategyRoacECs.Select(x => x.Value.Returns).ToList());
                foreach (var x in corr.EnumerateColumnsIndexed())
                {
                    corr.SetColumn(x.Item1, x.Item2.Add(1).SubtractFrom(2));
                }
                Matrix<double> coords = MultiDimensionalScaling.Scale(corr);

                for (int i = 0; i < coords.RowCount; i++)
                {
                    var dr = ds.MdsCoords.NewMdsCoordsRow();
                    dr.StrategyName = strategyNames[i];
                    dr.X = coords[i, 0];
                    dr.Y = coords[i, 1];
                    ds.MdsCoords.AddMdsCoordsRow(dr);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Error during mds: {0}", ex.Message));
            }
        }

        private void DoBacktestStats(IAppSettings settings)
        {
            if (_backtestEC == null || _backtestEC.Dates.Count <= 1)
            {
                return;
            }

            EquityCurve selectedData =
                _settings.BacktestComparisonReturnType == ReturnType.ROAC
                ? _totalPortfolioTracker.RoacEquityCurve
                : _totalPortfolioTracker.RotcEquityCurve;

            DateTime liveTradingStartDate = selectedData.Dates.First().Value.Date;

            EquityCurve backtestBefore = new EquityCurve(100, _backtestEC.Dates.First().Value);
            EquityCurve backtestDuring = null;

            //Here we divide the backtest data into two sub-samples
            //One before the live trading starts, and the other contemporaneously
            for (int i = 1; i < _backtestEC.Dates.Count; i++)
            {
                if (_backtestEC.Dates[i].Value.Date >= liveTradingStartDate)
                {
                    if (backtestDuring == null)
                    {
                        backtestDuring = new EquityCurve(100, _backtestEC.Dates[i].Value);
                    }
                    backtestDuring.AddReturn(_backtestEC.Returns[i], _backtestEC.Dates[i].Value);
                }
                else
                {
                    backtestBefore.AddReturn(_backtestEC.Returns[i], _backtestEC.Dates[i].Value);
                }
            }

            int backtestDays = (int)(backtestBefore.Dates.Last().Value.Date - backtestBefore.Dates.First().Value.Date).TotalDays;
            int backtestLiveDays = (int)(backtestDuring.Dates.Last().Value.Date - backtestDuring.Dates.First().Value.Date).TotalDays;
            int liveDays = (int)(selectedData.Dates.Last().Value.Date - selectedData.Dates.First().Value.Date).TotalDays;

            try
            {
                Dictionary<string, string> backtestStats = PerformanceMeasurement.EquityCurveStats(backtestBefore, backtestDays, settings.AssumedInterestRate);
                Dictionary<string, string> resultsStats = PerformanceMeasurement.EquityCurveStats(selectedData, liveDays, settings.AssumedInterestRate);
                Dictionary<string, string> backtestLiveStats = PerformanceMeasurement.EquityCurveStats(backtestDuring, backtestLiveDays, settings.AssumedInterestRate);


                foreach (var kvp in resultsStats)
                {
                    var dr = ds.BacktestStats.NewBacktestStatsRow();
                    dr.Stat = kvp.Key;
                    dr.Backtest = backtestStats[kvp.Key];
                    dr.BacktestDuringLive = backtestLiveStats[kvp.Key];
                    dr.Result = kvp.Value;
                    ds.BacktestStats.AddBacktestStatsRow(dr);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex);
            }
        }

        private void DoBacktestEquityCurves()
        {
            if (_backtestEC == null || _backtestEC.Dates.Count <= 1)
            {
                return;
            }

            EquityCurve selectedData =
                _settings.BacktestComparisonReturnType == ReturnType.ROAC
                ? _totalPortfolioTracker.RoacEquityCurve
                : _totalPortfolioTracker.RotcEquityCurve;

            int resultsCounter = -1;
            //make sure the backtest data doesn't start after the live returns
            if (selectedData.Dates[0].HasValue && selectedData.Dates[0].Value.Date < _backtestEC.Dates[0].Value.Date)
            {
                _logger.Log(LogLevel.Warn, "Backtest data starts after live data. Backtesting analysis not available.");
                return;
            }


            double mult = 1;
            double cumulativeDiff = 0;
            for (int i = 0; i < _backtestEC.Dates.Count; i++)
            {
                var dr = ds.BacktestEquityCurves.NewBacktestEquityCurvesRow();
                var dateTime = _backtestEC.Dates[i].Value;

                if (dateTime == null) continue;

                //generate cumulative difference in returns
                if (resultsCounter < 0 && dateTime.Date >= selectedData.Dates[0].Value.Date)
                {
                    resultsCounter = 0;
                    mult = _backtestEC.Equity[i] / selectedData.Equity[resultsCounter];
                }

                if (resultsCounter >= 0)
                {
                    cumulativeDiff += selectedData.Returns[resultsCounter] - _backtestEC.Returns[i];
                    dr.CumulativeDifference = cumulativeDiff;

                    dr.Result = selectedData.Equity[resultsCounter] * mult;
                    resultsCounter++;
                }
                else
                {
                    dr.Result = double.NaN;
                    dr.CumulativeDifference = double.NaN;
                }


                dr.Date = dateTime.Date;
                dr.Backtest = _backtestEC.Equity[i];
                ds.BacktestEquityCurves.AddBacktestEquityCurvesRow(dr);

                //The live results data might be shorter than the backtest data
                //in which case we just get out of the loop
                if (resultsCounter >= selectedData.Equity.Count)
                {
                    break;
                }
            }
        }

        private void DoPortfolioStats(IAppSettings settings)
        {
            int daysInPeriod = (int)(_datesInPeriod.Last() - _datesInPeriod.First()).TotalDays;
            Dictionary<string, string> roacStats = PerformanceMeasurement.EquityCurveStats(_totalPortfolioTracker.RoacEquityCurve, daysInPeriod, settings.AssumedInterestRate);
            Dictionary<string, string> rotcStats = PerformanceMeasurement.EquityCurveStats(_totalPortfolioTracker.RotcEquityCurve, daysInPeriod, settings.AssumedInterestRate);

            Dictionary<string, string> benchmarkStats = null;
            if (_benchmarkEC != null)
            {
                benchmarkStats = PerformanceMeasurement.EquityCurveStats(_benchmarkEC, daysInPeriod, settings.AssumedInterestRate);
            }

            foreach (var kvp in roacStats)
            {
                var dr = ds.PortfolioStats.NewPortfolioStatsRow();
                dr.stat = kvp.Key;
                dr.roac = kvp.Value;
                dr.rotc = rotcStats[kvp.Key];
                if (benchmarkStats != null)
                {
                    dr.benchmark = benchmarkStats[kvp.Key];
                }
                ds.PortfolioStats.AddPortfolioStatsRow(dr);
            }
        }

        private void AddCapitalUsageByStrategy()
        {
            Dictionary<string, List<decimal>> strategyCapitalUsage =
                _strategyPfolioTrackers.ToDictionary(x => x.Key, x => x.Value.Capital.Gross);

            foreach (var kvp in strategyCapitalUsage)
            {
                ds.CapitalUsageByStrategy.Columns.Add(kvp.Key, typeof(double));
                ds.RelativeCapitalUsageByStrategy.Columns.Add(kvp.Key, typeof(double));
            }

            var totalAllocation = new List<decimal>();

            for (int i = 0; i < _datesInPeriod.Count; i++)
            {
                totalAllocation.Add(0);

                var dr = ds.CapitalUsageByStrategy.NewRow();
                dr["date"] = _datesInPeriod[i];
                foreach (var kvp in strategyCapitalUsage)
                {
                    dr[kvp.Key] = (double)kvp.Value[i];
                    totalAllocation[i] += kvp.Value[i];
                }
                ds.CapitalUsageByStrategy.Rows.Add(dr);
            }

            for (int i = 0; i < _datesInPeriod.Count; i++)
            {
                var dr = ds.RelativeCapitalUsageByStrategy.NewRow();
                dr["date"] = _datesInPeriod[i];
                foreach (var kvp in strategyCapitalUsage)
                {
                    if (totalAllocation[i] == 0)
                    {
                        dr[kvp.Key] = 0;
                    }
                    else
                    {
                        dr[kvp.Key] = (double)(kvp.Value[i] / totalAllocation[i]);
                    }
                }
                ds.RelativeCapitalUsageByStrategy.Rows.Add(dr);
            }
        }

        private void AddStrategyPLCurves()
        {
            foreach (var kvp in _strategyPfolioTrackers)
            {
                ds.StrategyPLCurves.Columns.Add(kvp.Key, typeof(double));
            }

            for (int i = 0; i < _datesInPeriod.Count; i++)
            {
                var dr = ds.StrategyPLCurves.NewRow();
                dr["date"] = _datesInPeriod[i];
                foreach (KeyValuePair<string, PortfolioTracker> kvp in _strategyPfolioTrackers)
                {
                    dr[kvp.Key] = kvp.Value.ProfitLossEquityCurve.Equity[i + 1];
                }
                ds.StrategyPLCurves.Rows.Add(dr);
            }
        }

        private void DoPLByTag()
        {
            var distinctTags = _trades.Where(x => x.Tags != null).SelectMany(x => x.Tags).Distinct().OrderBy(x => x.Name).ToList();

            //per tag
            foreach (Tag tag in distinctTags)
            {
                var tagTrades = _trades.Where(x => x.Tags != null && x.Tags.Contains(tag)).ToList();
                var dr = ds.PLByTag.NewPLByTagRow();
                dr.tag = tag.Name;
                dr.avgPL = (double)tagTrades.Average(x => x.ResultDollars + x.UnrealizedResultDollars);
                dr.totalPL = (double)tagTrades.Sum(x => x.ResultDollars + x.UnrealizedResultDollars);
                dr.count = tagTrades.Count();
                ds.PLByTag.AddPLByTagRow(dr);
            }

            //tag combos -- we do them up to combinations of 3 tags
            var combinations = new List<List<Tag>>();
            combinations.AddRange(distinctTags.Combinations(2));
            combinations.AddRange(distinctTags.Combinations(3));

            foreach (List<Tag> combo in combinations)
            {
                var tagTrades = _trades.Where(x => x.Tags != null && x.Tags.Intersect(combo).Count() == combo.Count).ToList();

                if (tagTrades.Count == 0) continue;

                var dr = ds.PLByTagCombo.NewPLByTagComboRow();
                dr.tags = string.Join(", ", combo.Select(x => x.Name));
                dr.averagePL = (double)tagTrades.Average(x => x.ResultDollars + x.UnrealizedResultDollars);
                dr.totalPL = (double)tagTrades.Sum(x => x.ResultDollars + x.UnrealizedResultDollars);
                dr.count = tagTrades.Count();
                ds.PLByTagCombo.AddPLByTagComboRow(dr);
            }
        }

        private void DoAverageCumulativeTradeReturns()
        {
            var trackers = _totalPortfolioTracker.TradeTrackers.Values.ToList();

            int maxLength = trackers.Max(x => x.CumulativeReturns.Count);
            var cumRets = trackers.Select(x => x.CumulativeReturns.Values);
            var winnerCumRets = trackers.Where(x => x.Trade.ResultDollars + x.Trade.UnrealizedResultDollars > 0).Select(x => x.CumulativeReturns.Values);
            var loserCumRets = trackers.Where(x => x.Trade.ResultDollars + x.Trade.UnrealizedResultDollars <= 0).Select(x => x.CumulativeReturns.Values);

            double allValue = 1, winnerValue = 1, loserValue = 1;

            for (int i = 0; i < maxLength; i++)
            {
                int allCount = cumRets.Count(x => x.Count > i);

                double allRet = cumRets.Where(x => x.Count > i).Average(x => x[i] / (i == 0 ? 1 : x[i - 1]));
                allValue *= allRet;

                int winnersCount = winnerCumRets.Count(x => x.Count > i);

                int losersCount = loserCumRets.Count(x => x.Count > i);

                var dr = ds.AverageDailyRets.NewAverageDailyRetsRow();
                dr.day = i;
                dr.allRets = allValue - 1;
                dr.allCount = allCount;
                dr.winnersCount = winnersCount;
                if (winnerCumRets.Count(x => x.Count > i) > 0)
                {
                    double ret = winnerCumRets.Where(x => x.Count > i).Average(x => x[i] / (i == 0 ? 1 : x[i - 1]));
                    winnerValue *= ret;
                    dr.winnersRets = winnerValue - 1;
                }
                else
                {
                    dr["winnersRets"] = DBNull.Value;
                }
                dr.losersCount = losersCount;
                if (loserCumRets.Count(x => x.Count > i) > 0)
                {
                    double ret = loserCumRets.Where(x => x.Count > i).Average(x => x[i] / (i == 0 ? 1 : x[i - 1]));
                    loserValue *= ret;
                    dr.losersRets = loserValue - 1;
                }
                else
                {
                    dr["losersRets"] = DBNull.Value;
                }
                ds.AverageDailyRets.AddAverageDailyRetsRow(dr);
            }
        }

        private void DoMAEMFE()
        {
            foreach (TradeTracker t in _totalPortfolioTracker.TradeTrackers.Values)
            {
                var maeRow = ds.TradeMAE.NewTradeMAERow();
                maeRow.ret = (t.Trade.ResultPct + t.Trade.UnrealizedResultPct);
                maeRow.absReturn = Math.Abs(maeRow.ret);
                maeRow.mae = Math.Abs(t.MaxAdverseExcursion);
                ds.TradeMAE.AddTradeMAERow(maeRow);

                var mfeRow = ds.TradeMFE.NewTradeMFERow();
                mfeRow.ret = (t.Trade.ResultPct + t.Trade.UnrealizedResultPct);
                mfeRow.absReturn = Math.Abs(mfeRow.ret);
                mfeRow.mfe = t.MaxFavorableExcursion;
                ds.TradeMFE.AddTradeMFERow(mfeRow);
            }
        }



        private IEnumerable<int> GetNeededFxIDs()
        {
            var currencyIDs =
                _trades
                .Where(x => x.Orders != null)
                .SelectMany(x => x.Orders)
                .Select(x => x.CurrencyID)
                .ToList();

            currencyIDs
                .AddRange(
                    _trades
                    .Where(x => x.CashTransactions != null)
                    .SelectMany(x => x.CashTransactions)
                    .Select(x => x.CurrencyID));

            currencyIDs
                .AddRange(
                    _trades.Where(x => x.FXTransactions != null)
                    .SelectMany(x => x.FXTransactions)
                    .Select(x => x.FXCurrencyID));

            return currencyIDs.Distinct().ToList();
        }

        private void DoValueAtRisk(ReportSettings settings)
        {
            QLNet.RiskStatistics riskStats = new QLNet.RiskStatistics();
            var weights = new List<double>();
            var varReturns = new List<double>(); //transform the returns...
            var inputRets =
                settings.VaRReturnType == ReturnType.ROAC
                    ? _totalPortfolioTracker.RoacEquityCurve.Returns
                    : _totalPortfolioTracker.RotcEquityCurve.Returns;

            for (int i = 0; i < inputRets.Count - settings.VaRDays; i++)
            {
                varReturns.Add(inputRets.GetRange(i, settings.VaRDays).Aggregate(1.0, (x, y) => x * (1 + y)));
            }
            if (varReturns.Count < 10) return;

            varReturns = varReturns.OrderBy(x => x).ToList();

            for (int i = 0; i < varReturns.Count; i++)
            {
                weights.Add(1);
            }
            riskStats.addSequence(varReturns.Select(x => x - 1).ToList(), weights);

            List<double> varLevels = new List<double> { 0.1, 0.05, 0.01, 0.004 };
            foreach (double varLevel in varLevels)
            {
                var varDR = ds.ValueAtRisk.NewValueAtRiskRow();
                varDR.level = varLevel;
                varDR.var = -riskStats.valueAtRisk(1 - varLevel);
                varDR.bootstrapVaR = varReturns.Percentile(varLevel, false) - 1;
                ds.ValueAtRisk.AddValueAtRiskRow(varDR);
            }
        }

        private void DoComponentValueAtRisk()
        {
            //get strategy betas to the total portfolio

            //get marginal VaR = alpha * beta * stdev(pfolio), where alpha = 1.65 for 95% ci

            //component VaR = marginal VaR * $ position
        }

        private void DoExpectedShortfall(ReportSettings settings)
        {
            QLNet.RiskStatistics riskStats = new QLNet.RiskStatistics();
            var weights = new List<double>();
            var esReturns = new List<double>(); //transform the returns...
            var inputRets =
                settings.VaRReturnType == ReturnType.ROAC
                    ? _totalPortfolioTracker.RoacEquityCurve.Returns
                    : _totalPortfolioTracker.RotcEquityCurve.Returns;

            for (int i = 0; i < inputRets.Count - settings.VaRDays; i++)
            {
                esReturns.Add(inputRets.GetRange(i, settings.VaRDays).Aggregate(1.0, (x, y) => x * (1 + y)));
            }
            esReturns = esReturns.OrderBy(x => x).ToList();

            for (int i = 0; i < esReturns.Count; i++)
            {
                weights.Add(1);
            }
            riskStats.addSequence(esReturns.Select(x => x - 1).ToList(), weights);

            List<double> varLevels = new List<double> { 0.1, 0.05, 0.01, 0.004 };
            foreach (double varLevel in varLevels)
            {
                var dr = ds.ExpectedShortfall.NewExpectedShortfallRow();
                dr.level = varLevel;
                try
                {
                    dr.es = -riskStats.expectedShortfall(1 - varLevel);
                    double cutoff = esReturns.Percentile(varLevel, false);
                    dr.bootstrapES = esReturns.Where(x => x < cutoff).Average() - 1;
                }
                catch (Exception ex)
                {
                    dr.es = 0;
                    dr.bootstrapES = 0;
                    _logger.Log(LogLevel.Error, "Could not calculate ES, error: " + ex.Message);
                }
                ds.ExpectedShortfall.AddExpectedShortfallRow(dr);
            }
        }

        private void DoBacktestMonteCarlo()
        {
            if (_backtestEC == null || _backtestEC.Dates.Count <= 1) return;

            List<List<double>> equityCurves;
            List<List<double>> drawdownCurves;

            EquityCurve selectedData =
                _settings.BacktestComparisonReturnType == ReturnType.ROAC
                    ? _totalPortfolioTracker.RoacEquityCurve
                    : _totalPortfolioTracker.RotcEquityCurve;

            DateTime liveResultsStartDate = selectedData.Dates.First().Value;
            List<double> backtestReturns = new List<double>();
            for (int i = 0; i < _backtestEC.Returns.Count; i++)
            {
                if (_backtestEC.Dates[i] >= liveResultsStartDate)
                {
                    break;
                }
                backtestReturns.Add(_backtestEC.Returns[i]);
            }

            if (backtestReturns.Count < 10)
            {
                _logger.Log(LogLevel.Info, "Not enough data for backtest MC.");
                return;
            }

            int periods = selectedData.Dates.Count;



            MonteCarlo.Bootstrap(
                periods,
                10000,
                5,
                true,
                backtestReturns,
                out equityCurves,
                out drawdownCurves);

            double avgBacktestRet = 1 + backtestReturns.Average();

            if (equityCurves.Count == 0) return;

            double avgEquity = 100;

            //do the equity curves...
            for (int i = 0; i < periods; i++)
            {
                int i1 = i;
                var dr = ds.BacktestMonteCarlo.NewBacktestMonteCarloRow();
                var equityCurveValues = equityCurves.Select(x => x[i1]).ToList();
                dr.Period = i;

                equityCurveValues = equityCurveValues.OrderBy(x => x).ToList();

                dr._99PctileUpper = equityCurveValues[(int)Math.Round(.995 * equityCurveValues.Count())];
                dr._99PctileLower = equityCurveValues[(int)Math.Round(.005 * equityCurveValues.Count())];
                dr._95PctileUpper = equityCurveValues[(int)Math.Round(.975 * equityCurveValues.Count())];
                dr._95PctileLower = equityCurveValues[(int)Math.Round(.025 * equityCurveValues.Count())];
                dr._90PctileUpper = equityCurveValues[(int)Math.Round(.95 * equityCurveValues.Count())];
                dr._90PctileLower = equityCurveValues[(int)Math.Round(.05 * equityCurveValues.Count())];
                dr._50PctileUpper = equityCurveValues[(int)Math.Round(.75 * equityCurveValues.Count())];
                dr._50PctileLower = equityCurveValues[(int)Math.Round(.25 * equityCurveValues.Count())];

                dr.Average = avgEquity;
                avgEquity *= avgBacktestRet;
                dr.Result = selectedData.Equity[i] * 100;

                ds.BacktestMonteCarlo.AddBacktestMonteCarloRow(dr);
            }
        }

        private void DoMonteCarlo(IAppSettings appSettings)
        {
            List<List<double>> equityCurves;
            List<List<double>> drawdownCurves;

            MonteCarlo.Bootstrap(
                _settings.MCPeriods,
                _settings.MCRuns,
                _settings.MCClusterSize,
                _settings.MCWithReplacement,
                _settings.MCReturnType == ReturnType.ROAC
                    ? _totalPortfolioTracker.RoacEquityCurve.Returns
                    : _totalPortfolioTracker.RotcEquityCurve.Returns,
                out equityCurves,
                out drawdownCurves);

            if (equityCurves.Count == 0) return;

            //do the equity curves...
            for (int i = 0; i < _settings.MCPeriods; i++)
            {
                int i1 = i;
                var mceDR = ds.MCEquity.NewMCEquityRow();
                var dds = equityCurves.Select(x => x[i1]).ToList();
                mceDR.count = i;

                dds = dds.OrderBy(x => x).ToList();

                mceDR.wideHigh = dds[(int)Math.Round(.95 * dds.Count())];
                mceDR.wideLow = dds[(int)Math.Round(.05 * dds.Count())];
                mceDR.narrowHigh = dds[(int)Math.Round(.75 * dds.Count())];
                mceDR.narrowLow = dds[(int)Math.Round(.25 * dds.Count())];

                ds.MCEquity.Rows.Add(mceDR);
            }

            //and now the drawdowns...
            var maxDDs = drawdownCurves.Select(x => x.Min()).ToList();

            //we want a cumulative line and a histogram...to it per % point
            double startingPct = 0;
            double endingPct = 0;
            if (maxDDs.Count > 0)
            {
                startingPct = Math.Round(maxDDs.Max(), 3);
                endingPct = Math.Round(maxDDs.Min(), 3);
            }
            int totalCount = 0;
            for (double i = startingPct; i >= endingPct; i -= 0.0025)
            {
                var mcddDR = ds.MCDrawdowns.NewMCDrawdownsRow();

                int pointCount = maxDDs.Count(x => x >= i && x < i + 0.0025);
                totalCount += pointCount;
                mcddDR.ddLevel = i;
                mcddDR.point = pointCount;
                mcddDR.cumulative = (double)totalCount / _settings.MCRuns;
                ds.MCDrawdowns.Rows.Add(mcddDR);
            }

            var sharpes = new List<double>();
            var mars = new List<double>();
            var kRatios = new List<double>();

            //now the ratios
            for (int i = 0; i < equityCurves.Count; i++)
            {
                double sharpe, mar, kRatio;
                PerformanceMeasurement.GetRatios(equityCurves[i], drawdownCurves[i], (int)(_settings.MCPeriods * (365.0 / 252)), appSettings.AssumedInterestRate, out sharpe, out mar, out kRatio);
                sharpes.Add(sharpe);
                mars.Add(mar);
                kRatios.Add(kRatio);
            }

            sharpes = sharpes.OrderBy(x => x).ToList();
            mars = mars.OrderBy(x => x).ToList();
            kRatios = kRatios.OrderBy(x => x).ToList();

            for (int i = 0; i < equityCurves.Count; i++)
            {
                var ratiosDR = ds.MCRatios.NewMCRatiosRow();
                ratiosDR.Sharpe = sharpes[i];
                ratiosDR.SharpeCumulativePct = (double)i / equityCurves.Count;
                ratiosDR.MAR = mars[i];
                ratiosDR.MARCumulativePct = (double)i / equityCurves.Count;
                ratiosDR.KRatio = kRatios[i];
                ratiosDR.KRatioCumulativePct = (double)i / equityCurves.Count;
                ds.MCRatios.AddMCRatiosRow(ratiosDR);
            }
        }

        private void DoRollingAlphaBeta(ReturnType benchmarkingReturnsSource, double assumedInterestRate)
        {
            const int rollingPeriod = 100; //possibly allow changes to this...

            double[] b;
            var benchmarkingReturns =
                benchmarkingReturnsSource == ReturnType.ROAC
                    ? _totalPortfolioTracker.RoacEquityCurve.Returns
                    : _totalPortfolioTracker.RotcEquityCurve.Returns;

            if (_benchmarkEC.Returns.Count > rollingPeriod)
            {
                //start at 1 because the first item is just an empty data point from the EquityCurve
                for (int i = 1; i < _benchmarkEC.Returns.Count - rollingPeriod - 1; i++)
                {
                    var dr = ds.rollingAlphaBeta.NewrollingAlphaBetaRow();
                    //do regression of the returns on the benchmark to get alpha/beta/r^2 for the period
                    double rsquared;
                    double dailyRf = assumedInterestRate / 252;

                    MathUtils.MLR(benchmarkingReturns.GetRange(i, rollingPeriod).Select(x => x - dailyRf).ToList(),
                                    new List<IEnumerable<double>> { _benchmarkEC.Returns.GetRange(i, rollingPeriod).Select(x => x - dailyRf).ToList() },
                                    out b,
                                    out rsquared);
                    double alpha = b[0];
                    double beta = b[1];

                    dr.date = _datesInPeriod[rollingPeriod + i];
                    dr.alpha = Math.Pow(1 + alpha, 252) - 1; //annualization, currently used method may overstate alpha due to ignoring volatility
                    dr.beta = beta;
                    ds.rollingAlphaBeta.Rows.Add(dr);
                }
            }
        }

        private void DoBenchmarkStats(ReturnType benchmarkingReturnsSource, double assumedInterestRate)
        {
            var benchmarkingReturns =
                (benchmarkingReturnsSource == ReturnType.ROAC
                    ? _totalPortfolioTracker.RoacEquityCurve.Returns
                    : _totalPortfolioTracker.RotcEquityCurve.Returns).Skip(1); //skip 1 because 1st data point is empty, from EquityCurve

            double[] b;
            double rsq;
            double dailyRf = assumedInterestRate / 252;

            MathUtils.MLR(benchmarkingReturns.Select(x => x - dailyRf).ToList(),
                        new List<IEnumerable<double>> { _benchmarkReturns.Select(x => x - 1 - dailyRf).ToList() },
                        out b,
                        out rsq);

            double alpha = b[0];
            double beta = b[1];

            var dr = ds.benchmarkStats.NewbenchmarkStatsRow();
            dr.correlation = Correlation.Pearson(benchmarkingReturns, _benchmarkReturns);
            dr.alpha = Math.Pow(1 + alpha, 252) - 1; //annualization, currently used method may overstate alpha due to ignoring volatility
            dr.beta = beta;
            dr.rsquare = rsq;

            var activeReturn = benchmarkingReturns.Select((x, i) => x - (_benchmarkReturns[i] - 1)).ToList();
            dr.activeReturn = Math.Pow(1 + activeReturn.Average(), 252) - 1;
            dr.trackingError = activeReturn.StandardDeviation() * Math.Sqrt(252);

            if (Math.Abs(dr.trackingError) < 0.0000001)
            {
                dr.informationRatio = 0;
            }
            else
            {
                dr.informationRatio = dr.activeReturn / dr.trackingError;
            }

            ds.benchmarkStats.Rows.Add(dr);
        }

        private void AddTradeLengthVsRetPlotRows()
        {
            foreach (Trade t in _trades)
            {
                var dr = ds.tradeLengthsVsReturns.NewtradeLengthsVsReturnsRow();
                if (!t.Open && t.DateClosed.HasValue)
                {
                    dr.length = (t.DateClosed.Value - t.DateOpened).TotalDays;
                    dr.ret = t.ResultPct;
                    ds.tradeLengthsVsReturns.Rows.Add(dr);
                }
            }
        }

        private void AddTradeSizeVsRetPlotrows()
        {
            var es = Context
                    .EquitySummaries
                    .OrderByDescending(x => x.Date)
                    .ToList();
            foreach (Trade t in _trades)
            {
                var dr = ds.positionSizesVsReturns.NewpositionSizesVsReturnsRow();

                var closestEquitySummary = es.FirstOrDefault(x => x.Date <= t.DateOpened);
                if (closestEquitySummary == null) continue;

                decimal accountCapital = closestEquitySummary.Total;
                if (accountCapital == 0) return;
                dr.size = (double)(t.CapitalTotal / accountCapital);
                dr.ret = t.ResultPct;
                dr.strategy = t.Strategy.Name;
                dr.tags = t.TagString;
                ds.positionSizesVsReturns.Rows.Add(dr);
            }
        }

        private void AddDailyReturnHistogramRows()
        {
            var returnsOnAllocatedCapital = _totalPortfolioTracker.RoacEquityCurve.Returns;
            if (returnsOnAllocatedCapital.Any(x => Math.Abs(x) > 0.000001))
            {
                var dailyPctRetHistogram = new Histogram(returnsOnAllocatedCapital.Where(x => Math.Abs(x) > 0.000001).Select(x => x), 10);
                for (int i = 0; i < dailyPctRetHistogram.BucketCount; i++)
                {
                    var dprbDR = ds.dailyPctRetBuckets.NewdailyPctRetBucketsRow();
                    dprbDR.limits = string.Format("{0} - {1}",
                        dailyPctRetHistogram[i].LowerBound.ToString("p2"),
                        dailyPctRetHistogram[i].UpperBound.ToString("p2"));
                    dprbDR.count = (int)(dailyPctRetHistogram[i].Count + 0.5);
                    ds.dailyPctRetBuckets.Rows.Add(dprbDR);
                }
            }

            var returnsOnTotalCapital = _totalPortfolioTracker.RotcEquityCurve.Returns;
            if (returnsOnTotalCapital.Count(x => Math.Abs(x) > 0.000001) > 1)
            {
                var dailyPctRetHistogram = new Histogram(returnsOnTotalCapital.Where(x => Math.Abs(x) > 0.000001).Select(x => x), 10);
                for (int i = 0; i < dailyPctRetHistogram.BucketCount; i++)
                {
                    var dtprbDR = ds.dailyTotalPctRetBuckets.NewdailyTotalPctRetBucketsRow();
                    dtprbDR.limits = string.Format("{0} - {1}",
                        dailyPctRetHistogram[i].LowerBound.ToString("p2"),
                        dailyPctRetHistogram[i].UpperBound.ToString("p2"));
                    dtprbDR.count = (int)(dailyPctRetHistogram[i].Count + 0.5);
                    ds.dailyTotalPctRetBuckets.Rows.Add(dtprbDR);
                }
            }
        }

        private void AddHoldingPeriodHistogramRows()
        {
            //manually construct the categories here so they make a bit more sense
            Dictionary<string, int> holdingPeriodLimits = new Dictionary<string, int>
                {
                    {"<= 5 minutes", 5 * 60},
                    {"<= 1 hour", 60 * 60},
                    {"<= 12 hours", 12 * 60 * 60},
                    {"<= 1 day", 24 * 60 * 60},
                    {"<= 2 days", 2 * 24 * 60 * 60},
                    {"<= 1 week", 7 * 24 * 60 * 60},
                    {"<= 1 month", 30 * 24 * 60 * 60},
                    {"<= 3 months", 90 * 24 * 60 * 60},
                    {"<= 1 year", 365 * 24 * 60 * 60},
                    {"> 1 year", int.MaxValue}
                };

            List<int> lengths = _trades
                .Select(x => (x.Open || !x.DateClosed.HasValue)
                    ? (int)(DateTime.Now - x.DateOpened).TotalSeconds
                    : (int)(x.DateClosed.Value - x.DateOpened).TotalSeconds).ToList();

            foreach (var kvp in holdingPeriodLimits)
            {
                int lenLimit = kvp.Value;
                var dr = ds.holdingPeriodBuckets.NewholdingPeriodBucketsRow();
                dr.limits = kvp.Key;
                dr.count = lengths.Count(x => x < lenLimit);
                lengths.RemoveAll(x => x < lenLimit);
                ds.holdingPeriodBuckets.Rows.Add(dr);
            }
        }

        private void AddTradeResultHistogramRows()
        {
            //dollars
            var dollarReturns = _trades.Select(x => x.ResultDollars + x.UnrealizedResultDollars).ToList();
            if (dollarReturns.Count > 1)
            {
                var tradeDollarRetHistogram = new Histogram(dollarReturns.Select(x => (double)x).ToList(), 10);
                for (int i = 0; i < tradeDollarRetHistogram.BucketCount; i++)
                {
                    var tdrbDR = ds.tradeDollarRetBuckets.NewtradeDollarRetBucketsRow();
                    tdrbDR.limits = string.Format("{0} - {1}",
                        tradeDollarRetHistogram[i].LowerBound.ToString("c2"),
                        tradeDollarRetHistogram[i].UpperBound.ToString("c2"));
                    tdrbDR.count = (int)(tradeDollarRetHistogram[i].Count + 0.5);
                    ds.tradeDollarRetBuckets.Rows.Add(tdrbDR);
                }
            }

            var tradeReturns = _trades.Select(x => x.ResultPct + x.UnrealizedResultPct).ToList();
            //percent
            if (tradeReturns.Count > 1)
            {
                var tradePctRetHistogram = new Histogram(tradeReturns, 10);
                for (int i = 0; i < tradePctRetHistogram.BucketCount; i++)
                {
                    var tprbDR = ds.tradePctRetBuckets.NewtradePctRetBucketsRow();
                    tprbDR.limits = string.Format("{0} - {1}",
                        tradePctRetHistogram[i].LowerBound.ToString("p2"),
                        tradePctRetHistogram[i].UpperBound.ToString("p2"));
                    tprbDR.count = (int)(tradePctRetHistogram[i].Count + 0.5);
                    ds.tradePctRetBuckets.Rows.Add(tprbDR);
                }
            }
        }

        private void AddInstrumentROACRows()
        {
            foreach (Position p in _totalPortfolioTracker.Positions.Values.OrderBy(x => x.ROAC))
            {
                var roacDR = ds.instrumentROAC.NewinstrumentROACRow();
                roacDR.instrument = p.Instrument.Symbol ?? "";
                roacDR.ROAC = p.ROAC - 1;
                ds.instrumentROAC.AddinstrumentROACRow(roacDR);
            }
        }

        private void DoTradeStats()
        {
            var stats = PerformanceMeasurement.TradeStats(_trades, _minDate, _maxDate, _capitalInPeriod);

            foreach (var kvp in stats)
            {
                var dr = ds.TradeStats.NewTradeStatsRow();
                dr.stat = kvp.Key;
                dr.value = kvp.Value;
                ds.TradeStats.Rows.Add(dr);
            }
        }

        private void DoTradeStatsByStrategy()
        {
            var tradesByStrat = _trades.Where(x => x.Strategy != null).GroupBy(x => x.Strategy.Name);

            var stats = new Dictionary<string, Dictionary<string, string>>();
            foreach (var grouping in tradesByStrat)
            {
                stats.Add(grouping.Key, PerformanceMeasurement.TradeStats(grouping.Select(x => x).ToList(), _minDate, _maxDate, _capitalInPeriod));
                ds.TradeStatsByStrategy.Columns.Add(grouping.Key, typeof(string));
            }

            if (stats.Count == 0) return;

            foreach (KeyValuePair<string, string> statPair in stats.First().Value)
            {
                var dr = ds.TradeStatsByStrategy.NewRow();
                dr["stat"] = statPair.Key;
                foreach (var kvp in stats)
                {
                    string strategyName = kvp.Key;
                    string statValue = kvp.Value.ContainsKey(statPair.Key) ? kvp.Value[statPair.Key] : "N/A";
                    dr[strategyName] = statValue;
                }
                ds.TradeStatsByStrategy.Rows.Add(dr);
            }
        }

        private void DoCashTransactionByTypeRows()
        {
            Dictionary<string, decimal> cashTransactionPnL = _trades
                .Where(x => x.CashTransactions != null)
                .SelectMany(x => x.CashTransactions)
                .Where(x => x.Type != "Deposits & Withdrawals")
                .GroupBy(x => x.Type)
                .ToDictionary(x => x.Key, z => z.Sum(a => a.AmountInBase));

            foreach (var kvp in cashTransactionPnL)
            {
                var ctpnlDR = ds.cashTransactionPnL.NewcashTransactionPnLRow();
                ctpnlDR.type = kvp.Key;
                ctpnlDR.amount = kvp.Value;
                ds.cashTransactionPnL.Rows.Add(ctpnlDR);
            }
        }

        private void DoCloseToClosePnLCurve()
        {
            //the close to close pnl series needs to start at zero
            var dr = ds.C2CPnLCurve.NewC2CPnLCurveRow();
            dr.date = _minDate;
            dr.value = 0;
            dr.drawdown = 0;
            ds.C2CPnLCurve.Rows.Add(dr);

            decimal lastC2CPnL = 0, maxC2CPnL = 0;

            foreach (KeyValuePair<DateTime, decimal> t in _trades
                .Where(x => x.DateClosed != null && !x.Open)
                .OrderBy(x => x.DateClosed)
                .GroupBy(x => x.DateClosed.Value.Date)
                .ToDictionary(x => x.Key, y => y.Sum(trade => trade.ResultDollars)))
            {
                lastC2CPnL += t.Value;
                var c2CCurveDR = ds.C2CPnLCurve.NewC2CPnLCurveRow();
                c2CCurveDR.date = t.Key;
                maxC2CPnL = Math.Max(lastC2CPnL, maxC2CPnL);
                c2CCurveDR.value = lastC2CPnL;
                c2CCurveDR.drawdown = (double)(lastC2CPnL - maxC2CPnL);
                ds.C2CPnLCurve.Rows.Add(c2CCurveDR);
            }
        }

        private void AddMonthlyROTCRowS()
        {
            var retsByMonth = _totalPortfolioTracker.RotcEquityCurve.ReturnsByMonth;
            foreach (int year in retsByMonth.Keys)
            {
                double total = 1;
                var dr = ds.ROTCByMonth.NewROTCByMonthRow();
                for (int j = 1; j <= 12; j++)
                {
                    string month = DateTime.ParseExact(string.Format("2000-{0}-01", j), "yyyy-M-dd", CultureInfo.InvariantCulture).ToString("MMM").ToLower();
                    double thisMonthsRet =
                        retsByMonth[year].ContainsKey(j)
                            ? retsByMonth[year][j]
                            : 0;


                    total *= (1 + thisMonthsRet);
                    dr[month] = thisMonthsRet;
                }
                dr.total = total - 1;
                dr.year = year;
                ds.ROTCByMonth.Rows.Add(dr);
            }
        }

        private void AddMonthlyROACRows()
        {
            var retsByMonth = _totalPortfolioTracker.RoacEquityCurve.ReturnsByMonth;
            foreach (int year in retsByMonth.Keys)
            {
                double total = 1;
                var dr = ds.ROACByMonth.NewROACByMonthRow();
                for (int j = 1; j <= 12; j++)
                {
                    string month = DateTime.ParseExact(string.Format("2000-{0}-01", j), "yyyy-M-dd", CultureInfo.InvariantCulture).ToString("MMM").ToLower();

                    double thisMonthsRet =
                        retsByMonth[year].ContainsKey(j)
                            ? retsByMonth[year][j]
                            : 0;


                    total *= (1 + thisMonthsRet);
                    dr[month] = thisMonthsRet;
                }
                dr.total = total - 1;
                dr.year = year;
                ds.ROACByMonth.Rows.Add(dr);
            }
        }

        private void AddMonthlyPnLRows()
        {
            var pnlByMonth = _totalPortfolioTracker.ProfitLossEquityCurve.PnLByMonth;
            foreach (int year in pnlByMonth.Keys)
            {
                decimal total = 0;
                var drbmDR = ds.dollarReturnsByMonth.NewdollarReturnsByMonthRow();
                for (int j = 1; j <= 12; j++)
                {
                    string month = DateTime.ParseExact(string.Format("2000-{0}-01", j), "yyyy-M-dd", CultureInfo.InvariantCulture).ToString("MMM").ToLower();

                    decimal thisMonthsPnl =
                        pnlByMonth[year].ContainsKey(j)
                            ? (decimal)pnlByMonth[year][j]
                            : 0;

                    total += thisMonthsPnl;
                    drbmDR[month] = thisMonthsPnl;
                }
                drbmDR.total = total;
                drbmDR.year = year;
                ds.dollarReturnsByMonth.Rows.Add(drbmDR);
            }
        }

        private void AddROACEquityCurveRow()
        {
            var decpoacDR = ds.dailyEquityCurvePctOnAllocatedCapital.NewdailyEquityCurvePctOnAllocatedCapitalRow();
            decpoacDR.date = _todaysDate;
            decpoacDR.value = _totalPortfolioTracker.RoacEquityCurve.Equity.Last() - 1;
            decpoacDR.drawdown = _totalPortfolioTracker.RoacEquityCurve.DrawdownPct.Last();
            decpoacDR.benchmark = _benchmarkSeries == null ? 0 : _benchmarkSeries[_todaysDate] - 1;
            ds.dailyEquityCurvePctOnAllocatedCapital.Rows.Add(decpoacDR);
        }

        private void AddDollarEquityCurveRow()
        {
            var ddecDR = ds.dailyDollarEquityCurve.NewdailyDollarEquityCurveRow();
            ddecDR.date = _todaysDate;
            ddecDR.value = _totalPortfolioTracker.ProfitLossEquityCurve.Equity.Last();
            ddecDR.drawdown = _totalPortfolioTracker.ProfitLossEquityCurve.DrawdownAmt.Last();
            ds.dailyDollarEquityCurve.Rows.Add(ddecDR);

            var ddeclrDR = ds.dailyDollarEquityCurveLongShort.NewdailyDollarEquityCurveLongShortRow();
            ddeclrDR.date = _todaysDate;
            ddeclrDR.valueLong = _totalPortfolioTracker.ProfitLossLongEquityCurve.Equity.Last();
            ddeclrDR.valueShort = _totalPortfolioTracker.ProfitLossShortEquityCurve.Equity.Last();
            ddeclrDR.drawdownLong = _totalPortfolioTracker.ProfitLossLongEquityCurve.DrawdownAmt.Last();
            ddeclrDR.drawdownShort = _totalPortfolioTracker.ProfitLossShortEquityCurve.DrawdownAmt.Last();
            ds.dailyDollarEquityCurveLongShort.Rows.Add(ddeclrDR);
        }

        private void DoCapitalUsage()
        {
            for (int i = 0; i < _totalPortfolioTracker.Capital.Gross.Count; i++)
            {
                decimal allocatedCapital = _totalPortfolioTracker.Capital.Gross[i];
                var cuDR = ds.capitalUsage.NewcapitalUsageRow();
                cuDR.date = _datesInPeriod[i];
                cuDR.totalCapital = _capitalInPeriod[i];
                cuDR.allocatedCapital = allocatedCapital;
                cuDR.utilization = (double)(_capitalInPeriod[i] == 0 ? 0 : allocatedCapital / _capitalInPeriod[i]);
                ds.capitalUsage.Rows.Add(cuDR);
            }
        }

        private void AddDailyEquityCurvePctOnTotalCapitalRow()
        {
            var decpotcDR = ds.dailyEquityCurvePctOnTotalCapital.NewdailyEquityCurvePctOnTotalCapitalRow();
            decpotcDR.date = _todaysDate;
            decpotcDR.value = _totalPortfolioTracker.RotcEquityCurve.Equity.Last() - 1;
            decpotcDR.drawdown = _totalPortfolioTracker.RotcEquityCurve.DrawdownPct.Last();
            decpotcDR.benchmark = _benchmarkSeries == null ? 0 : _benchmarkSeries[_todaysDate] - 1;
            ds.dailyEquityCurvePctOnTotalCapital.Rows.Add(decpotcDR);
        }

        private void DoPnLByInstrument(IEnumerable<Position> positions)
        {
            foreach (Position p in positions.OrderBy(x => x.PnL))
            {
                var pnlbiDR = ds.pnlByInstrument.NewpnlByInstrumentRow();
                pnlbiDR.PnL = p.PnL;
                pnlbiDR.instrument = p.Instrument.Symbol ?? "";
                ds.pnlByInstrument.AddpnlByInstrumentRow(pnlbiDR);
            }
        }



        /// <summary>
        /// determine what dates we need each instrument for
        /// </summary>
        private Dictionary<Instrument, KeyValuePair<DateTime, DateTime>> GetNeededDates()
        {
            //Instrument id - from date/to date
            var neededDates = new Dictionary<Instrument, KeyValuePair<DateTime, DateTime>>();
            foreach (Trade t in _trades.Where(x => x.Orders != null))
            {
                var tmpEndDate = t.Open || !t.DateClosed.HasValue ? _datesInPeriod.Last() : t.DateClosed.Value;

                foreach (Order o in t.Orders)
                {
                    if (neededDates.ContainsKey(o.Instrument))
                    {
                        DateTime newMinDate = neededDates[o.Instrument].Key > o.TradeDate ? o.TradeDate : neededDates[o.Instrument].Key;
                        DateTime newMaxDate = neededDates[o.Instrument].Value < tmpEndDate ? tmpEndDate : neededDates[o.Instrument].Value;
                        neededDates[o.Instrument] = new KeyValuePair<DateTime, DateTime>(newMinDate.Date, newMaxDate);
                    }
                    else
                    {
                        neededDates.Add(o.Instrument, new KeyValuePair<DateTime, DateTime>(o.TradeDate.Date, tmpEndDate));
                    }
                }
            }

            return neededDates;
        }

        private TimeSeries TimeSeriesFromPriorPositions(IEnumerable<PriorPosition> positions)
        {
            var bars = new List<OHLCBar>();
            foreach (PriorPosition p in positions)
            {
                var bar = new OHLCBar
                {
                    Open = p.Price,
                    High = p.Price,
                    Low = p.Price,
                    Close = p.Price,
                    AdjOpen = p.Price,
                    AdjHigh = p.Price,
                    AdjLow = p.Price,
                    AdjClose = p.Price,
                    DT = p.Date
                };
                bars.Add(bar);
            }

            return new TimeSeries(bars);
        }
    }
}