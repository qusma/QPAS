// -----------------------------------------------------------------------
// <copyright file="TradesRepository.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace QPAS
{
    public class TradesRepository : GenericRepository<Trade>, ITradesRepository
    {
        internal IDataSourcer Datasourcer;
        private decimal _optionsCapitalUsageMultiplier;

        public TradesRepository(IDBContext context, IDataSourcer datasourcer, decimal optionsCapitalUsageMultiplier)
            : base(context)
        {
            _optionsCapitalUsageMultiplier = optionsCapitalUsageMultiplier;
            Datasourcer = datasourcer;
        }

        public async Task UpdateOpenTrades()
        {
            var logger = LogManager.GetCurrentClassLogger();
            //todo override this get to always return with these includes; enforce usage of repository so everything is always loaded
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
                    await UpdateStats(t).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "Error updating stats on trade {0} ({1}): {2}", t.Name, t.ID, ex.Message);
                }
            }
        }

        public void SetTags(List<Tag> tags, Trade trade)
        {
            if (tags == null) return;
            if (trade.Tags == null) trade.Tags = new ObservableCollection<Tag>();
            trade.Tags.Clear();

            foreach (Tag t in tags)
            {
                trade.Tags.Add(t);
            }
        }

        public async Task AddOrders(Trade trade, IEnumerable<Order> orders)
        {
            foreach (var order in orders)
            {
                await AddOrder(trade, order, false).ConfigureAwait(true);
            }
            await UpdateStats(trade).ConfigureAwait(true);
        }

        public async Task AddOrder(Trade trade, Order order, bool updateStats = true)
        {
            if (trade == null) throw new ArgumentNullException(nameof(trade));
            if (order == null) throw new ArgumentNullException(nameof(order));

            var oldTrade = order.Trade;

            if (oldTrade != null && trade.ID == oldTrade.ID)
            {
                //no change
                return;
            }

            //remove the order from its current trade
            await RemoveOrder(oldTrade, order).ConfigureAwait(true);

            //and then add it to the new one
            if (trade.Orders == null)
                trade.Orders = new List<Order>();

            trade.Orders.Add(order);
            order.Trade = trade;
            order.TradeID = trade.ID;

            //finally update the stats of the new trade
            if (updateStats)
            {
                await UpdateStats(order.Trade).ConfigureAwait(true);
            }
        }

        public async Task RemoveOrder(Trade trade, Order order)
        {
            if (trade?.Orders == null || !trade.Orders.Contains(order)) return;
            trade.Orders.Remove(order);
            order.Trade = null;
            order.TradeID = null;
            await UpdateStats(trade).ConfigureAwait(true);
        }

        public async Task AddCashTransaction(Trade trade, CashTransaction ct)
        {
            if (trade == null) throw new ArgumentNullException(nameof(trade));
            if (ct == null) throw new ArgumentNullException(nameof(ct));

            var oldTrade = ct.Trade;

            if (oldTrade != null && trade.ID == oldTrade.ID)
            {
                //no change
                return;
            }

            //remove the ct from its current trade
            await RemoveCashTransaction(oldTrade, ct).ConfigureAwait(true);

            //and then add it to the new one
            if (trade.CashTransactions == null)
                trade.CashTransactions = new List<CashTransaction>();

            trade.CashTransactions.Add(ct);
            ct.Trade = trade;
            ct.TradeID = trade.ID;

            //finally update the stats of the new trade
            await UpdateStats(ct.Trade).ConfigureAwait(true);
        }

        public async Task RemoveCashTransaction(Trade trade, CashTransaction ct)
        {
            if (trade == null || trade.CashTransactions == null || !trade.CashTransactions.Contains(ct)) return;
            trade.CashTransactions.Remove(ct);
            ct.Trade = null;
            ct.TradeID = null;
            await UpdateStats(trade).ConfigureAwait(true);
        }

        public async Task AddFXTransaction(Trade trade, FXTransaction fxt)
        {
            if (trade == null) throw new ArgumentNullException(nameof(trade));
            if (fxt == null) throw new ArgumentNullException(nameof(fxt));

            var oldTrade = fxt.Trade;

            if (oldTrade != null && trade.ID == oldTrade.ID)
            {
                //no change
                return;
            }

            //remove the ct from its current trade
            await RemoveFXTransaction(oldTrade, fxt).ConfigureAwait(true);

            //and then add it to the new one
            if (trade.FXTransactions == null)
                trade.FXTransactions = new List<FXTransaction>();

            trade.FXTransactions.Add(fxt);
            fxt.Trade = trade;
            fxt.TradeID = trade.ID;

            //finally update the stats of the new trade
            await UpdateStats(fxt.Trade).ConfigureAwait(true);
        }

        public async Task RemoveFXTransaction(Trade trade, FXTransaction fxt)
        {
            if (trade?.FXTransactions == null || !trade.FXTransactions.Contains(fxt)) return;
            trade.FXTransactions.Remove(fxt);
            fxt.Trade = null;
            fxt.TradeID = null;
            await UpdateStats(trade).ConfigureAwait(true);
        }

        private static DateTime DetermineStartingDate(Trade trade, IDBContext context)
        {
            DateTime startDate = new DateTime(9999, 1, 1);

            if (trade.Orders != null && trade.Orders.Count > 0)
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

        public async Task UpdateStats(Trade trade, bool skipCollectionLoad = false)
        {
            var tradeEntry = Context.Entry(trade); //todo fix
            if (!skipCollectionLoad && //used to bypass annoyances w/ automated testing
                tradeEntry.State != EntityState.Added &&
                tradeEntry.State != EntityState.Detached) //trade entry state check, otherwise the load is meaningless and will cause a crash
            {
                tradeEntry.Collection(x => x.Orders).Load();
                tradeEntry.Collection(x => x.CashTransactions).Load();
                tradeEntry.Collection(x => x.FXTransactions).Load();
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

            TradeTracker tracker = await TradeSim.SimulateTrade(trade, Context, Datasourcer, _optionsCapitalUsageMultiplier).ConfigureAwait(true);
            tracker.SetTradeStats(trade);
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

        public async Task Reset(Trade trade)
        {
            Context.Entry(trade).Reload();
            Context.Entry(trade).Collection(x => x.Orders).Load();
            Context.Entry(trade).Collection(x => x.CashTransactions).Load();
            Context.Entry(trade).Collection(x => x.FXTransactions).Load();
            Context.Entry(trade).Collection(x => x.Tags).Load();

            trade.Orders?.Clear();

            trade.CashTransactions?.Clear();

            trade.FXTransactions?.Clear();

            trade.Tags?.Clear();

            await UpdateStats(trade).ConfigureAwait(true);
        }
    }
}