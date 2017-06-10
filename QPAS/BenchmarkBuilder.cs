// -----------------------------------------------------------------------
// <copyright file="BenchmarkBuilder.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EntityModel;
using NLog;

namespace QPAS
{
    public static class BenchmarkBuilder
    {
        /// <summary>
        /// generate EC and other curves for a benchmark
        /// </summary>
        public static async Task<(EquityCurve equityCurve, Dictionary<DateTime, double> benchmarkSeries, List<double> benchmarkReturns)> GetBenchmarkReturns(
            int benchmarkID, 
            DBContext context, 
            List<DateTime> datesInPeriod, 
            IDataSourcer dataSourcer)
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            List<BenchmarkComponent> components = context.BenchmarkComponents.Where(x => x.BenchmarkID == benchmarkID).ToList();

            DateTime earliestDate = datesInPeriod[0].Date;
            DateTime latestDate = datesInPeriod.Last();

            var data = new Dictionary<int, TimeSeries>();
            foreach (var component in components)
            {
                data.Add(component.QDMSInstrumentID,
                    new TimeSeries(await dataSourcer.GetExternalData(component.QDMSInstrumentID, earliestDate, latestDate).ConfigureAwait(true)));
            }


            Dictionary<int, decimal> weights = components.ToDictionary(x => x.QDMSInstrumentID, x => (decimal)x.Weight);

            var benchmarkSeries = new Dictionary<DateTime, double>();
            var benchmarkReturns = new List<double>();
            var benchmarkEC = new EquityCurve(1, null);

            decimal equity = 1;

            bool first = true;

            foreach (DateTime today in datesInPeriod)
            {
                decimal ret = 0;
                foreach (var kvp in data)
                {
                    var ts = kvp.Value;
                    ts.ProgressTo(today);
                    if (ts.CurrentBar > 0)
                    {
                        decimal todayClose = ts[0].AdjClose.HasValue ? ts[0].AdjClose.Value : ts[0].Close;
                        decimal lastClose = ts[1].AdjClose.HasValue ? ts[1].AdjClose.Value : ts[1].Close;
                        ret += weights[kvp.Key] * (todayClose / lastClose - 1);
#if DEBUG
                        logger.Log(LogLevel.Trace, "Benchmark component: Date: {0} Close: {1} PrevClose: {2} Ret: {3}", today, todayClose, lastClose, ret);
#endif
                    }
                }

                benchmarkEC.AddReturn((double)ret, today);

                if (first)
                {
                    first = false;
                    benchmarkReturns.Add((double)(1 + ret));
                    benchmarkSeries.Add(today, (double)equity);

                    continue;
                }

                equity *= 1 + ret;
                benchmarkReturns.Add((double)(1 + ret));
                benchmarkSeries.Add(today, (double)equity);
            }

            return (benchmarkEC, benchmarkSeries, benchmarkReturns);
        }
    }
}
