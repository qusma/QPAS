// -----------------------------------------------------------------------
// <copyright file="EquityCurve.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public class EquityCurve
    {
        public List<double> Equity { get; set; }
        public List<double> DrawdownPct { get; set; }
        public List<double> DrawdownAmt { get; set; }
        public List<double> Returns { get; set; }
        public List<double> Changes { get; set; }
        public List<DateTime?> Dates { get; set; }

        /// <summary>
        /// First key: Year
        /// Second key: Month
        /// Value: compounded return for that month
        /// </summary>
        public Dictionary<int, Dictionary<int, double>> ReturnsByMonth
        {
            get
            {
                return Returns
                    .Zip(Dates, (r, d) => new { Ret = r, Date = d})
                    .Where(x => x.Date.HasValue)
                    .GroupBy(x => x.Date.Value.Year)
                    .ToDictionary(
                        g => g.Key, 
                        g => g
                            .GroupBy(x => x.Date.Value.Month)
                            .ToDictionary(
                                z => z.Key, 
                                z => z.Aggregate(1.0, (x, y) => x * (y.Ret + 1)) - 1));
            }
        }

        /// <summary>
        /// First key: Year
        /// Second key: Month
        /// Value: sum of pnl by that month
        /// </summary>
        public Dictionary<int, Dictionary<int, double>> PnLByMonth
        {
            get
            {
                return Changes
                    .Zip(Dates, (r, d) => new { Change = r, Date = d })
                    .Where(x => x.Date.HasValue)
                    .GroupBy(x => x.Date.Value.Year)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .GroupBy(x => x.Date.Value.Month)
                            .ToDictionary(
                                z => z.Key,
                                z => z.Sum(x => x.Change)));
            }
        }


        /// <summary>
        /// Length between peaks in calendar days.
        /// </summary>
        public List<TimeSpan> DrawdownLengths { get; set; }

        private double _maxEquity;
        private DateTime _drawdownStart;

        public EquityCurve(double startingValue = 100)
        {
            _maxEquity = startingValue;
            Equity = new List<double> { startingValue };
            DrawdownPct = new List<double> { 0 };
            DrawdownAmt = new List<double> { 0 };
            Returns = new List<double> { 0 };
            DrawdownLengths = new List<TimeSpan>();
            Dates = new List<DateTime?>();
            Changes = new List<double>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ret">Net Return</param>
        public void AddReturn(double ret, DateTime? dt = null)
        {
            Equity.Add(Equity.Last() * (1 + ret));
            Changes.Add(Equity[Equity.Count - 1] - Equity[Equity.Count - 2]);
            _maxEquity = Math.Max(_maxEquity, Equity.Last());
            Returns.Add(ret);
            DrawdownPct.Add(_maxEquity == 0 ? 0 : Equity.Last() / _maxEquity - 1);
            DrawdownAmt.Add(Equity.Last() - _maxEquity);

            if (dt != null)
                AddDrawdownLengths(dt.Value);

            Dates.Add(dt);
        }

        public void AddChange(double change, DateTime? dt = null)
        {
            Returns.Add(Equity.Last() != 0 ? change / Equity.Last() : 0);
            Changes.Add(change);
            Equity.Add(Equity.Last() + change);
            _maxEquity = Math.Max(_maxEquity, Equity.Last());
            DrawdownPct.Add(_maxEquity == 0 ? 0 : Equity.Last() / _maxEquity - 1);
            DrawdownAmt.Add(Equity.Last() - _maxEquity);

            if (dt != null)
                AddDrawdownLengths(dt.Value);

            Dates.Add(dt);
        }

        public void AddValue(double value, DateTime? dt = null)
        {
            Returns.Add(Equity.Last() != 0 ? value / Equity.Last() - 1 : 0);
            Equity.Add(value);
            Changes.Add(Equity[Equity.Count - 1] - Equity[Equity.Count - 2]);
            _maxEquity = Math.Max(_maxEquity, value);
            DrawdownPct.Add(_maxEquity == 0 ? 0 : value / _maxEquity - 1);
            DrawdownAmt.Add(Equity.Last() - _maxEquity);

            if (dt != null)
                AddDrawdownLengths(dt.Value);

            Dates.Add(dt);
        }

        private void AddDrawdownLengths(DateTime dt)
        {
            if (Returns.Count > 1 && DrawdownPct[DrawdownPct.Count - 2] < -0.0000001 && DrawdownPct.Last() > -0.0000001)
            {
                DrawdownLengths.Add(dt - _drawdownStart);
            }

            if (DrawdownPct.Last() < -0.000001 && (Returns.Count <= 1 || DrawdownPct[DrawdownPct.Count - 2] > -0.000001))
            {
                _drawdownStart = dt;
            }
        }

        /// <summary>
        /// Call this at the end to add any open drawdowns to the drawdown length
        /// </summary>
        public void CalcFinalValues(DateTime dt)
        {
            if (DrawdownPct.Last() < -0.0000001)
            {
                DrawdownLengths.Add(dt - _drawdownStart);
            }
        }
    }
}
