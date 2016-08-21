// -----------------------------------------------------------------------
// <copyright file="TradeSim.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using QDMS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public static class TradeSim
    {
        public static TradeTracker SimulateTrade(Trade trade, IDBContext context, IDataSourcer dataSourcer, decimal optionsCapitalUsageMultiplier)
        {
            var tracker = new TradeTracker(trade, optionsCapitalUsageMultiplier);

            //starting and ending dates
            DateTime startDate = trade.DateOpened;
            DateTime endDate;
            
            if(!trade.Open && trade.DateClosed != null)
            {
                endDate = trade.DateClosed.Value.Date;
            }
            else
            {
                var lastSummary = context.EquitySummaries.OrderByDescending(x => x.Date).First();
                endDate = lastSummary.Date.Date;
            }

            var orders = trade.Orders == null 
                ? new List<Order>()
                : trade.Orders.OrderBy(x => x.TradeDate).ToList();

            var cashTransactions = trade.CashTransactions == null
                ? new List<CashTransaction>()
                : trade.CashTransactions.OrderBy(x => x.TransactionDate).ToList();

            var fxTransactions = trade.FXTransactions == null
                ? new List<FXTransaction>()
                : trade.FXTransactions.OrderBy(x => x.DateTime).ToList();

            //Grab the data
            Dictionary<int, TimeSeries> data = GetInstrumentData(trade, dataSourcer, startDate, endDate);
            Dictionary<int, TimeSeries> fxData = GetFXData(trade, context);

            DateTime currentDate = startDate.Date;
            //Loop through the dates
            while (currentDate <= endDate)
            {
                //Progress time series to current date
                foreach (TimeSeries ts in data.Values)
                {
                    ts.ProgressTo(currentDate);
                }
                foreach (TimeSeries ts in fxData.Values)
                {
                    ts.ProgressTo(currentDate);
                }

                //Add orders
                while (orders.Count > 0 && orders[0].TradeDate.Date <= currentDate)
                {
                    tracker.AddOrder(orders[0]);
                    orders.RemoveAt(0);
                }

                //Add cash transactions
                while (cashTransactions.Count > 0 && cashTransactions[0].TransactionDate.Date <= currentDate)
                {
                    tracker.AddCashTransaction(cashTransactions[0]);
                    cashTransactions.RemoveAt(0);
                }

                //add fx transactions
                while (fxTransactions.Count > 0 && fxTransactions[0].DateTime.Date <= currentDate)
                {
                    tracker.AddFXTransaction(fxTransactions[0]);
                    fxTransactions.RemoveAt(0);
                }

                tracker.Update(currentDate, data, fxData);

                if (orders.Count == 0 && cashTransactions.Count == 0 && fxTransactions.Count == 0 && !tracker.Open) break;

                currentDate = currentDate.AddDays(1);
            }

            return tracker;
        }

        private static Dictionary<int, TimeSeries> GetInstrumentData(Trade trade, IDataSourcer dataSourcer, DateTime startDate, DateTime endDate)
        {
            var data = new Dictionary<int, TimeSeries>();
            if (trade.Orders == null) return data;

            foreach (EntityModel.Instrument inst in trade.Orders.Select(x => x.Instrument).Distinct(x => x.ID))
            {
                data.Add(inst.ID, new TimeSeries(dataSourcer.GetData(inst, startDate, endDate)));
            }
            return data;
        }

        private static Dictionary<int, TimeSeries> GetFXData(Trade trade, IDBContext context)
        {
            List<int> neededFXIDs = GetNeededFXIDs(trade);
            var fxData = new Dictionary<int, TimeSeries>();
            foreach (int id in neededFXIDs)
            {
                if (id <= 1) continue;

                int id1 = id;
                fxData.Add(id, TimeSeriesFromFXRates(context.FXRates.Where(x => x.FromCurrencyID == id1).OrderBy(x => x.Date)));
            }
            return fxData;
        }

        private static List<int> GetNeededFXIDs(Trade trade)
        {
            var currencyIDs = new List<int>();
            if (trade.Orders != null)
            {
                currencyIDs.AddRange(trade.Orders.Select(x => x.CurrencyID));
            }

            if (trade.CashTransactions != null)
            {
                currencyIDs.AddRange(trade.CashTransactions.Select(x => x.CurrencyID));
            }

            if (trade.FXTransactions != null)
            {
                currencyIDs.AddRange(trade.FXTransactions.Select(x => x.FXCurrencyID));
            }
            return currencyIDs.Distinct().ToList();
        }

        private static TimeSeries TimeSeriesFromFXRates(IEnumerable<FXRate> rates)
        {
            var bars = new List<OHLCBar>();
            foreach (var rate in rates)
            {
                var bar = new OHLCBar
                {
                    Open = rate.Rate,
                    High = rate.Rate,
                    Low = rate.Rate,
                    Close = rate.Rate,
                    AdjOpen = rate.Rate,
                    AdjHigh = rate.Rate,
                    AdjLow = rate.Rate,
                    AdjClose = rate.Rate,
                    DT = rate.Date
                };
                bars.Add(bar);
            }

            return new TimeSeries(bars);
        }
    }
}