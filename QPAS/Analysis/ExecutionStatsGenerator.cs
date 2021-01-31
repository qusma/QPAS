// -----------------------------------------------------------------------
// <copyright file="ExecutionStats.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using QDMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument = EntityModel.Instrument;

namespace QPAS
{
    public class ExecutionStatsGenerator
    {
        public List<ExecutionStats> Stats { get; set; }

        public List<Order> Orders;
        private readonly IDataSourcer _datasourcer;

        private Dictionary<int, List<OHLCBar>> _data;

        private TimeSpan? _referenceTime;

        private Dictionary<int, List<InstrumentSession>> _instrumentSessions;

        public ExecutionStatsGenerator(IDataSourcer datasourcer)
        {
            Orders = new List<Order>();
            _datasourcer = datasourcer;
            _data = new Dictionary<int, List<OHLCBar>>();
        }

        public void SetOrders(List<Order> orders)
        {
            Orders = orders;
        }

        /// <summary>
        /// Generates stats based on the difference of the order executions from the reference price and time
        /// </summary>
        /// <param name="benchmarkPrice"></param>
        /// <param name="referenceTime">Null to use QDMS-provided time, the reference time otherwise.</param>
        public async Task GenerateExecutionStats(ExecutionBenchmark benchmarkPrice, TimeSpan? referenceTime)
        {
            if (Orders == null || Orders.Count == 0)
            {
                throw new Exception("No orders selected.");
            }

            _referenceTime = referenceTime;
            Stats = new List<ExecutionStats>();

            if (referenceTime == null && benchmarkPrice != ExecutionBenchmark.Reference)
            {
                _instrumentSessions = await GetSessionTimes(Orders.Select(x => x.Instrument).Distinct());
            }

            //if it's at the open we have to grab external data
            if (benchmarkPrice == ExecutionBenchmark.Open)
            {
                //make sure we're connected to external data source
                if (_datasourcer.ExternalDataSource == null || !_datasourcer.ExternalDataSource.Connected)
                {
                    throw new Exception("Must be connected to external data source.");
                }

                //grab the data
                await RequestRequiredData().ConfigureAwait(true);
            }

            //generate the stats
            Benchmark(Orders, benchmarkPrice);
        }

        /// <summary>
        /// When we need external data (benchmarking vs Opening price), this is
        /// where we request it.
        /// </summary>
        private async Task RequestRequiredData()
        {
            _data.Clear();
            Dictionary<Instrument, KeyValuePair<DateTime, DateTime>> neededDates = GetNeededDates();
            foreach (var kvp in neededDates)
            {
                var instrument = kvp.Key;
                DateTime fromDate = kvp.Value.Key;
                DateTime toDate = kvp.Value.Value;

                var data = await _datasourcer.GetData(instrument, fromDate, toDate).ConfigureAwait(true);
                _data.Add(instrument.ID, data);
            }
        }

        /// <summary>
        /// Finds the earliest and latest orders for each instrument under analysis.
        /// Used to make data requests.
        /// </summary>
        private Dictionary<Instrument, KeyValuePair<DateTime, DateTime>> GetNeededDates()
        {
            //Instrument id - from date/to date
            var neededDates = new Dictionary<Instrument, KeyValuePair<DateTime, DateTime>>();

            var groupedOrders = Orders.GroupBy(x => x.Instrument);
            foreach (var grouping in groupedOrders)
            {
                DateTime earliestOrder = grouping.OrderBy(x => x.TradeDate).First().TradeDate.Date;
                DateTime latestOrder = grouping.OrderByDescending(x => x.TradeDate).First().TradeDate.Date;
                neededDates.Add(grouping.Key, new KeyValuePair<DateTime, DateTime>(earliestOrder, latestOrder));
            }

            return neededDates;
        }

        /// <summary>
        /// Returns a dictionary of instrument IDs and the sessions that correspond to that instrument.
        /// </summary>
        private async Task<Dictionary<int, List<InstrumentSession>>> GetSessionTimes(IEnumerable<Instrument> instruments)
        {
            if (_datasourcer.ExternalDataSource == null || !_datasourcer.ExternalDataSource.Connected)
            {
                throw new Exception("Must be connected to external data source.");
            }

            var dict = new Dictionary<int, List<InstrumentSession>>();
            foreach (var inst in instruments)
            {
                dict.Add(inst.ID, await _datasourcer.ExternalDataSource.GetSessions(inst).ConfigureAwait(true));
            }

            return dict;
        }

        /// <summary>
        /// Gets the reference price for a particular order, given an execution benchmark.
        /// </summary>
        private decimal? GetReferencePrice(Order order, ExecutionBenchmark benchmark)
        {
            if (benchmark == ExecutionBenchmark.Close)
            {
                return order.ClosePrice;
            }
            else if (benchmark == ExecutionBenchmark.Open)
            {
                //For the open we need to look at the external data
                if (!_data.ContainsKey(order.InstrumentID)) return null;
                var bar = _data[order.InstrumentID].FirstOrDefault(x => x.DT.Date == order.TradeDate.Date);
                if (bar == null) return null;
                return bar.Open;
            }
            else if (benchmark == ExecutionBenchmark.VWAP)
            {
                throw new NotImplementedException();
            }
            else if (benchmark == ExecutionBenchmark.Reference)
            {
                return order.ReferencePrice;
            }

            return 0;
        }

        private DateTime? GetReferenceTime(Order order, ExecutionBenchmark benchmark)
        {
            //If the user sets a fixed reference time, we use that one in all cases
            if (_referenceTime != null)
            {
                return order.TradeDate.Date + _referenceTime.Value;
            }

            //Otherwise use QDMS or the reference time set at the order
            if (benchmark == ExecutionBenchmark.Close)
            {
                var session = _instrumentSessions[order.InstrumentID]
                    .FirstOrDefault(x => x.IsSessionEnd && (int)x.ClosingDay == order.TradeDate.DayOfWeek.ToInt());
                if (session == null) return null;

                return order.TradeDate.Date + session.ClosingTime;
            }
            else if (benchmark == ExecutionBenchmark.Open)
            {
                var session = _instrumentSessions[order.InstrumentID]
                    .Where(x => (int)x.ClosingDay == order.TradeDate.DayOfWeek.ToInt())
                    .OrderBy(x => x.OpeningTime)
                    .FirstOrDefault();
                if (session == null) return null;

                return order.TradeDate.Date + session.OpeningTime;
            }
            else if (benchmark == ExecutionBenchmark.VWAP)
            {
                throw new NotImplementedException();
            }
            else if (benchmark == ExecutionBenchmark.Reference)
            {
                return order.ReferenceTime;
            }

            return new DateTime(1, 1, 1);
        }

        /// <summary>
        /// Loop through the orders and benchmark their executions.
        /// </summary>
        private void Benchmark(IEnumerable<Order> orders, ExecutionBenchmark benchmark)
        {
            foreach (Order order in orders)
            {
                decimal? referencePrice = GetReferencePrice(order, benchmark);
                if (!referencePrice.HasValue) continue;

                DateTime? referenceTime = GetReferenceTime(order, benchmark);
                if (!referenceTime.HasValue) continue;

                GenerateStats(order, referenceTime.Value, referencePrice.Value);
            }
        }

        /// <summary>
        /// We have the reference time and price, now generate stats for every execution in the given order.
        /// </summary>
        private void GenerateStats(Order order, DateTime referenceTime, decimal referencePrice)
        {
            if (order.Executions == null) return;

            foreach (Execution ex in order.Executions)
            {
                Stats.Add(new ExecutionStats(ex, referencePrice, referenceTime));
            }
        }
    }
}
