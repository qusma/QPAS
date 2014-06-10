// -----------------------------------------------------------------------
// <copyright file="TradesRepository.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EntityModel;
using NLog;

namespace QPAS
{
    public class TradesRepository : GenericRepository<Trade>
    {
        internal IDataSourcer Datasourcer;

        public TradesRepository(IDBContext context, IDataSourcer datasourcer)
            : base(context)
        {
            Datasourcer = datasourcer;
        }

        public void UpdateOpenTrades()
        {
            var logger = LogManager.GetCurrentClassLogger();

            var trades = Get(x => x.Open)
                .Include(x => x.Strategy)
                .Include(x => x.Orders)
                .Include("Orders.Instrument")
                .Include("Orders.Currency")
                .Include(x => x.CashTransactions)
                .Include("CashTransactions.Instrument")
                .Include("CashTransactions.Currency")
                .Include(x => x.FXTransactions)
                .Include("FXTransactions.FXCurrency")
                .Include("FXTransactions.FunctionalCurrency")
                .ToList();

            foreach (Trade t in trades)
            {
                try
                {
                    UpdateStats(t);
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "Error updating stats on trade {0} ({1}): {2}", t.Name, t.ID, ex.Message);
                }
            }
        }

        public void AddOrder(Trade trade, Order order)
        {
            if (trade == null) throw new ArgumentNullException("trade");
            if (order == null) throw new ArgumentNullException("order");

            var oldTrade = order.Trade;

            if (oldTrade != null && trade.ID == oldTrade.ID)
            {
                //no change
                return;
            }

            //remove the order from its current trade
            RemoveOrder(oldTrade, order);

            //and then add it to the new one
            if (trade.Orders == null)
                trade.Orders = new List<Order>();

            trade.Orders.Add(order);
            order.Trade = trade;
            order.TradeID = trade.ID;

            //finally update the stats of the new trade
            UpdateStats(order.Trade);
        }

        public void RemoveOrder(Trade trade, Order order)
        {
            if (trade == null || trade.Orders == null || !trade.Orders.Contains(order)) return;
            trade.Orders.Remove(order);
            order.Trade = null;
            order.TradeID = null;
            UpdateStats(trade);
        }

        public void AddCashTransaction(Trade trade, CashTransaction ct)
        {
            if (trade == null) throw new ArgumentNullException("trade");
            if (ct == null) throw new ArgumentNullException("ct");

            var oldTrade = ct.Trade;

            if (oldTrade != null && trade.ID == oldTrade.ID)
            {
                //no change
                return;
            }

            //remove the ct from its current trade
            RemoveCashTransaction(oldTrade, ct);

            //and then add it to the new one
            if (trade.CashTransactions == null)
                trade.CashTransactions = new List<CashTransaction>();

            trade.CashTransactions.Add(ct);
            ct.Trade = trade;
            ct.TradeID = trade.ID;

            //finally update the stats of the new trade
            UpdateStats(ct.Trade);
        }

        public void RemoveCashTransaction(Trade trade, CashTransaction ct)
        {
            if (trade == null || trade.CashTransactions == null || !trade.CashTransactions.Contains(ct)) return;
            trade.CashTransactions.Remove(ct);
            ct.Trade = null;
            ct.TradeID = null;
            UpdateStats(trade);
        }

        public void AddFXTransaction(Trade trade, FXTransaction fxt)
        {
            if (trade == null) throw new ArgumentNullException("trade");
            if (fxt == null) throw new ArgumentNullException("fxt");

            var oldTrade = fxt.Trade;

            if (oldTrade != null && trade.ID == oldTrade.ID)
            {
                //no change
                return;
            }

            //remove the ct from its current trade
            RemoveFXTransaction(oldTrade, fxt);

            //and then add it to the new one
            if (trade.FXTransactions == null)
                trade.FXTransactions = new List<FXTransaction>();

            trade.FXTransactions.Add(fxt);
            fxt.Trade = trade;
            fxt.TradeID = trade.ID;

            //finally update the stats of the new trade
            UpdateStats(fxt.Trade);
        }

        public void RemoveFXTransaction(Trade trade, FXTransaction fxt)
        {
            if (trade == null || trade.FXTransactions == null || !trade.FXTransactions.Contains(fxt)) return;
            trade.FXTransactions.Remove(fxt);
            fxt.Trade = null;
            fxt.TradeID = null;
            UpdateStats(trade);
        }

        private static DateTime DetermineStartingDate(Trade trade, IDBContext context)
        {
            DateTime startDate = new DateTime(9999, 1, 1); 
            
            if(trade.Orders != null && trade.Orders.Count > 0)
            {
                startDate = trade.Orders.Select(x => x.TradeDate).OrderBy(x => x).First();
            }

            if (trade.CashTransactions != null && trade.CashTransactions.Count > 0)
            {
                DateTime firstCashTransactionDate = trade.CashTransactions.Select(x => x.TransactionDate).OrderBy(x => x).First();
                if (firstCashTransactionDate < startDate)
                {
                    startDate = firstCashTransactionDate;
                }
            }

            var firstSummary = context.EquitySummaries.OrderBy(x => x.Date).First();
            if (startDate < firstSummary.Date)
            {
                startDate = firstSummary.Date;
            }

            return startDate;
        }

        public void UpdateStats(Trade trade, bool skipCollectionLoad = false)
        {
            if (!skipCollectionLoad && //used to bypass annoyances w/ automated testing
                Context.Entry(trade).State != EntityState.Added &&
                Context.Entry(trade).State != EntityState.Detached) //trade entry state check, otherwise the load is meaningless and will cause a crash
            {
                Context.Entry(trade).Collection(x => x.Orders).Load();
                Context.Entry(trade).Collection(x => x.CashTransactions).Load();
                Context.Entry(trade).Collection(x => x.FXTransactions).Load();
            }

            DateTime openDate = DetermineStartingDate(trade, Context);
            //Dates
            trade.DateOpened = openDate;
            if (trade.Open)
            {
                trade.DateClosed = null;
            }
            else
            {
                SetClosingDate(trade);
            }

            TradeTracker tracker = TradeSim.SimulateTrade(trade, Context, Datasourcer);

            var positions = tracker.Positions.Values;
            var currencyPositions = tracker.CurrencyPositions.Values;

            //Capital usage stats
            trade.CapitalTotal =
                tracker.Capital.Gross.Count(x => x > 0) > 0
                    ? tracker.Capital.Gross.Where(x => x > 0).Average()
                    : 0;

            trade.CapitalLong =
                tracker.Capital.Long.Count(x => x > 0) > 0
                    ? tracker.Capital.Long.Where(x => x > 0).Average()
                    : 0;

            trade.CapitalShort =
                tracker.Capital.Short.Count(x => x > 0) > 0
                    ? tracker.Capital.Short.Where(x => x > 0).Average()
                    : 0;

            trade.CapitalNet = trade.CapitalLong - trade.CapitalShort;

            //Realized dollar result stats
            trade.ResultDollars = tracker.RealizedPnL;
            trade.ResultDollarsLong = tracker.RealizedPnLLong;
            trade.ResultDollarsShort = tracker.RealizedPnLShort;

            //Realized percent result stats
            trade.ResultPct = trade.CapitalTotal > 0
                ? (double)(trade.ResultDollars / trade.CapitalTotal)
                : 0;

            trade.ResultPctLong = trade.CapitalLong > 0
                ? (double)(trade.ResultDollarsLong / trade.CapitalLong)
                : 0;

            trade.ResultPctShort = trade.CapitalShort > 0
                ? (double)(trade.ResultDollarsShort / trade.CapitalShort)
                : 0;

            //Commissions
            if (trade.Orders != null)
            {
                trade.Commissions = trade.Orders.Sum(x => x.CommissionInBase);
            }


            //Unrealized result stats
            trade.UnrealizedResultDollars = tracker.TotalPnL - tracker.RealizedPnL;
            trade.UnrealizedResultDollarsLong = positions.Sum(x => x.PnLLong - x.RealizedPnLLong);
            trade.UnrealizedResultDollarsShort = positions.Sum(x => x.PnLShort - x.RealizedPnLShort);

            //Unrealized percent result stats
            trade.UnrealizedResultPct = trade.CapitalTotal > 0
                ? (double)(trade.UnrealizedResultDollars / trade.CapitalTotal)
                : 0;

            trade.UnrealizedResultPctLong = trade.CapitalLong > 0
                ? (double)(trade.UnrealizedResultDollarsLong / trade.CapitalLong)
                : 0;

            trade.UnrealizedResultPctShort = trade.CapitalShort > 0
                ? (double)(trade.UnrealizedResultDollarsShort / trade.CapitalShort)
                : 0;
        }

        public void SetClosingDate(Trade trade)
        {
            DateTime lastOrder = new DateTime(1, 1, 1);
            DateTime lastCashTransaction = new DateTime(1, 1, 1);

            if (trade.Orders != null && trade.Orders.Count > 0)
                lastOrder = trade.Orders.Max(x => x.TradeDate);

            if (trade.CashTransactions != null && trade.CashTransactions.Count > 0)
                lastCashTransaction = trade.CashTransactions.Max(x => x.TransactionDate);

            trade.DateClosed = lastOrder > lastCashTransaction ? lastOrder : lastCashTransaction;
        }

        public void Reset(Trade trade)
        {
            Context.Entry(trade).Reload();
            Context.Entry(trade).Collection(x => x.Orders).Load();
            Context.Entry(trade).Collection(x => x.CashTransactions).Load();
            Context.Entry(trade).Collection(x => x.FXTransactions).Load();
            Context.Entry(trade).Collection(x => x.Tags).Load();

            if (trade.Orders != null)
            {
                trade.Orders.Clear();
            }

            if (trade.CashTransactions != null)
            {
                trade.CashTransactions.Clear();
            }

            if (trade.FXTransactions != null)
            {
                trade.FXTransactions.Clear();
            }

            if(trade.Tags != null)
            {
                trade.Tags.Clear();
            }

            UpdateStats(trade);
        }
    }
}
