// -----------------------------------------------------------------------
// <copyright file="PerformanceMeasurement.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MathNet.Numerics.Financial;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public static class PerformanceMeasurement
    {
        public static Dictionary<string, string> TradeStats(List<Trade> trades, DateTime minDate, DateTime maxDate, List<decimal> capitalInPeriod)
        {
            var tradeReturns = trades.Select(x => x.TotalResultPct).ToList();
            var commissions = trades.Select(x => x.Commissions).ToList();
            var tradePL = trades.Select(x => x.TotalResultDollars).ToList();

            var returnStats = new DescriptiveStatistics(tradeReturns);
            var dollarReturnStats = new DescriptiveStatistics(tradePL.Select(x => (double)x));
            decimal totalCapital = trades.Sum(x => x.CapitalTotal);

            var stats = new Dictionary<string, string>();

            stats.Add("Average % Return", returnStats.Mean.ToString("p3"));
            stats.Add("Average $ Return", dollarReturnStats.Mean.ToString("c2"));

            stats.Add("Total Realized P/L", (tradePL.Count > 0 ? tradePL.Sum() : 0).ToString("c2"));
            stats.Add("Total Unrealized P/L", trades.Sum(x => x.UnrealizedResultDollars).ToString("c2"));

            stats.Add("Average Commission", (commissions.Count > 0 ? commissions.Average() : 0).ToString("c2"));
            stats.Add("Average Commission (%)", ((double)(totalCapital == 0 ? 0 : commissions.Sum() / totalCapital)).ToString("p2"));
            stats.Add("Total Commissions", (commissions.Count > 0 ? commissions.Sum() : 0).ToString("c2"));

            stats.Add("Best Trade (%)", returnStats.Maximum.ToString("p2"));
            stats.Add("Best Trade ($)", tradePL.Count > 0 ? tradePL.Max().ToString("c2") : "");
            stats.Add("Worst Trade (%)", returnStats.Minimum.ToString("p2"));
            stats.Add("Worst Trade ($)", tradePL.Count > 0 ? tradePL.Min().ToString("c2") : "");

            if (tradeReturns.Any(x => x > 0))
            {
                stats.Add("Average Win (%)", tradeReturns.Where(x => x > 0).Average().ToString("p2"));
                stats.Add("Average Win ($)", tradePL.Where(x => x > 0).Average().ToString("c2"));
            }

            if (tradeReturns.Any(x => x <= 0))
            {
                stats.Add("Average Loss (%)", tradeReturns.Where(x => x <= 0).Average().ToString("p2"));
                stats.Add("Average Loss ($)", tradePL.Where(x => x <= 0).Average().ToString("c2"));
            }



            if (tradeReturns.Any(x => x > 0) && tradeReturns.Any(x => x < 0))
            {
                stats.Add("Risk:Reward (%)",
                    (tradeReturns.Where(x => x > 0).Average() / -tradeReturns.Where(x => x < 0).Average()).ToString("0.00"));
                stats.Add("Risk:Reward ($)",
                    (tradePL.Where(x => x > 0).Average() / -tradePL.Where(x => x < 0).Average()).ToString("0.00"));
            }

            var lengths = trades
                .Select(x => (x.Open || !x.DateClosed.HasValue)
                    ? (int)(DateTime.Now - x.DateOpened).TotalSeconds
                    : (int)(x.DateClosed.Value - x.DateOpened).TotalSeconds).ToList();

            stats.Add("Average Trade Length", Utils.FormatTimespan(TimeSpan.FromSeconds(lengths.Average())));
            stats.Add("Win Rate", tradeReturns.Any() ? ((double)tradeReturns.Count(x => x > 0) / tradeReturns.Count()).ToString("p2") : "");
            stats.Add("Number of Trades", trades.Count.ToString());
            stats.Add("Closed Trades", trades.Count(x => !x.Open).ToString());

            //% gross win/loss & profit factor
            if (tradeReturns.Any(x => x > 0))
            {
                stats.Add("Gross Win (%)", tradeReturns.Where(x => x > 0).Sum().ToString("p2"));
            }

            if (tradeReturns.Any(x => x <= 0))
            {
                stats.Add("Gross Loss (%)", tradeReturns.Where(x => x <= 0).Sum().ToString("p2"));
            }

            if (tradeReturns.Any(x => x > 0) && tradeReturns.Any(x => x <= 0))
            {
                stats.Add("Profit Factor (%)",
                    (tradeReturns.Where(x => x > 0).Sum() / -tradeReturns.Where(x => x <= 0).Sum()).ToString("0.00"));
            }

            //Dollar gross win/loss & profit factor
            if (tradePL.Any(x => x > 0))
            {
                stats.Add("Gross Win ($)", tradePL.Where(x => x > 0).Sum().ToString("c2"));
            }

            if (tradePL.Any(x => x <= 0))
            {
                stats.Add("Gross Loss ($)", tradePL.Where(x => x <= 0).Sum().ToString("c2"));
            }

            if (tradePL.Any(x => x > 0) && tradePL.Any(x => x < 0))
            {
                stats.Add("Profit Factor ($)",
                    (tradePL.Where(x => x > 0).Sum() / -tradePL.Where(x => x < 0).Sum()).ToString("0.00"));
            }

            //StdDev/Skewness
            if (tradeReturns.Count > 0)
            {
                stats.Add("Standard Deviation (%)", returnStats.StandardDeviation.ToString("p2"));
                stats.Add("Skewness (%)", returnStats.Skewness.ToString("0.00"));
            }

            if (tradePL.Count > 0)
            {
                stats.Add("Standard Deviation ($)", dollarReturnStats.StandardDeviation.ToString("c2"));
                stats.Add("Skewness ($)", dollarReturnStats.Skewness.ToString("0.00"));
            }

            //turnover: a bit tricky. Calculated as the sum of absolute trade amounts / 2, divided by the average capital during the period
            double turnover;
            try
            {
                //TODO ensure turnover is calculated using a starting date only after the strat has started trading, not the whole period!
                decimal moneyTraded = trades.Where(x => x.Orders != null).SelectMany(x => x.Orders).Sum(x => Math.Abs(x.TradeMoney)) / 2;
                turnover = ((365 / (maxDate - minDate).TotalDays) * (double)moneyTraded / (double)capitalInPeriod.Average());
            }
            catch
            {
                turnover = 0;
            }

            stats.Add("Annual Turnover", turnover.ToString("p0"));

            // Positive Trade Ratio
            double ptr = 0;
            double avg = (double)tradePL.Average();
            List<double> rval = new List<double>();

            foreach (double value in tradePL)
                rval.Add(value - avg < 0 ? Math.Pow(value - avg, 2) : 0);

            double denom = Math.Sqrt((1 / (double)(rval.Count - 1)) * rval.Sum());
            ptr = denom != 0 ? avg / denom : 0;

            stats.Add("Positive Trade Ratio", ptr.ToString("0.000"));

            // System Quality Number
            double sqn = (double)((decimal)Math.Sqrt(trades.Count) * tradePL.Average() / StandardDeviation(tradePL));
            stats.Add("System Quality Number", sqn.ToString("0.000"));

            return stats;
        }

        public static Dictionary<string, string> EquityCurveStats(EquityCurve ec, int calendarDaysInPeriod, double assumedInterestRate)
        {
            if (ec.Returns.Count <= 1) return new Dictionary<string, string>();

            var stats = new Dictionary<string, string>();
            var ds = new DescriptiveStatistics(ec.Returns);

            stats.Add("Average Return", ds.Mean.ToString("p3"));
            stats.Add("Total Period Return", ((ec.Equity.Last() / ec.Equity.First()) - 1).ToString("p2"));
            double cagr = Math.Pow(ec.Equity.Last() / ec.Equity.First(), 365.0 / calendarDaysInPeriod) - 1;
            stats.Add("CAGR", cagr.ToString("p2"));
            stats.Add("Standard Deviation (Daily)", ds.StandardDeviation.ToString("p2"));
            stats.Add("Standard Deviation (Annualized)", (ds.StandardDeviation * Math.Sqrt(252)).ToString("p2"));
            stats.Add("Skewness", ds.Skewness.ToString("0.00"));
            stats.Add("Best day", ds.Maximum.ToString("p2"));
            stats.Add("Worst day", ds.Minimum.ToString("p2"));
            stats.Add("Average Up Day", ec.Returns.Any(x => x > 0) ? ec.Returns.Where(x => x > 0).Average().ToString("p2") : "-");
            stats.Add("Average Down Day", ec.Returns.Any(x => x < 0) ? ec.Returns.Where(x => x < 0).Average().ToString("p2") : "-");



            if (ec.Returns.Any(x => x > 0) && ec.Returns.Any(x => x < 0))
            {
                stats.Add("Risk:Reward", (ec.Returns.Where(x => x > 0).Average() / -ec.Returns.Where(x => x < 0).Average()).ToString("0.00"));
            }
            else
            {
                stats.Add("Risk:Reward", "-");
            }

            double zeroLimit = 0.0000001;
            stats.Add("% Up Days", ((double)ec.Returns.Count(x => x > zeroLimit) / ec.Returns.Count).ToString("p1"));
            stats.Add("% Down Days", ((double)ec.Returns.Count(x => x < -zeroLimit) / ec.Returns.Count).ToString("p1"));
            stats.Add("% Flat Days", ((double)ec.Returns.Count(x => Math.Abs(x) < zeroLimit) / ec.Returns.Count).ToString("p1"));

            double grossWin = ec.Returns.Where(x => x > 0).Sum();
            stats.Add("Gross Win", grossWin.ToString("p2"));
            double grossLoss = ec.Returns.Where(x => x < 0).Sum();
            stats.Add("Gross Loss", grossLoss.ToString("p2"));

            stats.Add("Profit Factor", grossLoss != 0 ? (grossWin / -grossLoss).ToString("0.00") : "-");

            double downsideDeviation = ec.Returns.DownsideDeviation(0);
            double upsideDeviation = ec.Returns.Where(x => x > 0).StandardDeviation();
            stats.Add("Volatility Skewness", (upsideDeviation / downsideDeviation).ToString("0.00"));

            stats.Add("Max Drawdown", ec.DrawdownPct.Min().ToString("p2"));
            stats.Add("Average Drawdown", ec.DrawdownPct.Average().ToString("p2"));
            if (ec.DrawdownLengths.Count > 0)
                stats.Add("Longest Drawdown", ec.DrawdownLengths.Max().TotalDays.ToString("0.0") + " days");
            else
                stats.Add("Longest Drawdown", "N/A");

            string avgDDLengthStat = "N/A";
            if (ec.DrawdownLengths.Count > 0)
            {
                avgDDLengthStat = ec.DrawdownLengths.Select(x => x.TotalDays).Average().ToString("0.00") + " days";
            }
            stats.Add("Average Drawdown Length", avgDDLengthStat);

            double sharpe, mar, kratio;
            GetRatios(ec, calendarDaysInPeriod, assumedInterestRate, out sharpe, out mar, out kratio);
            stats.Add("Sharpe ratio", sharpe.ToString("0.00"));
            double sortino = (cagr - assumedInterestRate) / (downsideDeviation * Math.Sqrt(252));
            stats.Add("Sortino ratio", sortino.ToString("0.00"));
            stats.Add("MAR Ratio", mar.ToString("0.00"));
            stats.Add("K Ratio", kratio.ToString("0.00"));
            double ulcerIndex = Math.Sqrt(ec.DrawdownPct.Sum(x => x * x) / ec.DrawdownPct.Count);
            stats.Add("Ulcer Index", (ulcerIndex * 100).ToString("0.00"));
            stats.Add("UPI/Martin Ratio", ((cagr - assumedInterestRate) / ulcerIndex).ToString("0.00"));

            return stats;
        }

        /// <summary>
        /// Calculate performance ratios for a given equity curve.
        /// </summary>
        /// <param name="equityCurve"></param>
        /// <param name="drawdownCurve"></param>
        /// <param name="daysInPeriod">Number of calendar days covered by the equity curve.</param>
        /// <param name="sharpeRatio"></param>
        /// <param name="marRatio"></param>
        /// <param name="kRatio"></param>
        public static void GetRatios(List<double> equityCurve, List<double> drawdownCurve, int daysInPeriod, double assumedInterestRate, out double sharpeRatio, out double marRatio, out double kRatio)
        {
            double cagr = Math.Pow(equityCurve.Last() / equityCurve.First(), (double)365 / daysInPeriod) - 1;
            var returns = Price2Ret(equityCurve);
            sharpeRatio = (cagr - assumedInterestRate) / (returns.StandardDeviation() * Math.Sqrt(252));
            marRatio = cagr / -drawdownCurve.Min();
            kRatio = KRatio(equityCurve);
        }

        public static void GetRatios(EquityCurve ec, int daysInPeriod, double assumedInterestRate, out double sharpeRatio, out double marRatio, out double kRatio)
        {
            GetRatios(ec.Equity, ec.DrawdownPct, daysInPeriod, assumedInterestRate, out sharpeRatio, out marRatio, out kRatio);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prices"></param>
        /// <param name="simple"></param>
        /// <returns></returns>
        public static List<double> Price2Ret(List<double> prices, bool simple = true)
        {
            List<double> rets = new List<double>(prices.Count);
            for (int i = 1; i < prices.Count; i++)
            {
                if (simple)
                    rets.Add(prices[i] / prices[i - 1] - 1);
                else
                    rets.Add(Math.Log(prices[i] / prices[i - 1]));
            }
            return rets;
        }

        /// <summary>
        /// K-Ratio: Slope / S.E. of slope
        /// </summary>
        /// <param name="ec">Equity Curve</param>
        public static double KRatio(List<double> ec)
        {
            //start by taking the log (assume compounding)
            ec = ec.Select(x => Math.Log(x)).ToList();

            double avgY = ec.Average();
            double avgX = ((double)ec.Count) / 2;

            //get the slope
            double covar = 0, varY = 0, varX = 0;
            for (int i = 0; i < ec.Count; i++)
            {
                covar += (ec[i] - avgY) * (i - avgX);
                varY += Math.Pow(ec[i] - avgY, 2);
                varX += Math.Pow(i - avgX, 2);
            }
            double slope = covar / varX;

            //get the standard error
            double se = Math.Sqrt((1.0 / (ec.Count - 2)) * (varY - (Math.Pow(covar, 2)) / varX));

            //the 2nd denominator doesn't actually matter, just makes the scale reasonable
            return slope / (se / Math.Sqrt(varX));
        }

        public static decimal StandardDeviation(List<decimal> samples)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (samples.Count <= 1) return decimal.MinValue;

            decimal variance = 0;
            decimal t = samples[0];
            for (int i = 1; i < samples.Count; i++)
            {
                t += samples[i];
                decimal diff = ((i + 1) * samples[i]) - t;
                variance += (diff * diff) / ((i + 1) * i);
            }
            return (decimal)Math.Sqrt((double)(variance / (samples.Count - 1)));
        }
    }
}
