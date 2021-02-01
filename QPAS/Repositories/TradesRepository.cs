using EntityModel;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace QPAS
{
    public class TradesRepository
    {
        private IContextFactory _contextFactory;
        private readonly IDataSourcer _dataSourcer;
        private readonly IAppSettings _settings;
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public TradesRepository(IContextFactory contextFactory, IDataSourcer dataSourcer, IAppSettings settings)
        {
            _contextFactory = contextFactory;
            _dataSourcer = dataSourcer;
            _settings = settings;
        }

        /// <summary>
        /// Simulate the trade and update its stats. Does not save to db.
        /// </summary>
        /// <param name="trade"></param>
        /// <returns></returns>
        public async Task UpdateStats(Trade trade)
        {
            DateTime openDate = DetermineStartingDate(trade);
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

            TradeTracker tracker = await TradeSim.SimulateTrade(trade, _contextFactory, _dataSourcer, _settings.OptionsCapitalUsageMultiplier);
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

        private DateTime DetermineStartingDate(Trade trade)
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

            return startDate;
        }

        public async Task RemoveCashTransaction(Trade trade, CashTransaction ct, bool save = true)
        {
            if (trade == null || trade.CashTransactions == null || !trade.CashTransactions.Contains(ct)) return;
            trade.CashTransactions.Remove(ct);
            ct.Trade = null;
            ct.TradeID = null;
            await UpdateStats(trade).ConfigureAwait(false);
            if (save)
            {
                await UpdateTrade(trade).ConfigureAwait(false);
            }
        }


        internal async Task CloseTrades(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            var closedTrades = new List<Trade>();
            foreach (Trade trade in trades)
            {
                //Already closed or can't close -> skip it
                if (!trade.Open) continue;

                //this needs to be done after loading the orders
                if (!trade.IsClosable()) continue;

                trade.Open = false;
                closedTrades.Add(trade);
            }

            foreach (Trade trade in closedTrades)
            {
                await UpdateStats(trade).ConfigureAwait(false);
            }

            await UpdateTrade(trades: closedTrades).ConfigureAwait(false);
        }

        public async Task AddTags(Trade trade, List<Tag> tags)
        {
            if (tags == null) return;
            if (trade.Tags == null) trade.Tags = new ObservableCollection<Tag>();
            trade.Tags.Clear();

            foreach (Tag t in tags)
            {
                trade.Tags.Add(t);
            }

            await UpdateTrade(trade).ConfigureAwait(false);
        }

        public async Task SetTag(Trade trade, Tag tag, bool add)
        {
            if (!add)
            {
                trade.Tags.Remove(tag);
            }
            else
            {
                if (!trade.Tags.Any(x => x.Name == tag.Name))
                {
                    trade.Tags.Add(tag);
                }
            }
            trade.TagStringUpdated();

            await UpdateTrade(trade).ConfigureAwait(false);
        }

        public async Task AddOrders(Trade trade, IEnumerable<Order> orders)
        {
            foreach (var order in orders)
            {
                await AddOrder(trade, order, false).ConfigureAwait(false);
            }

            await Task.Run(async () => await UpdateStats(trade).ConfigureAwait(false)).ConfigureAwait(false);
            await UpdateTrade(trade).ConfigureAwait(false);
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
            await RemoveOrder(oldTrade, order).ConfigureAwait(false);

            //and then add it to the new one
            if (trade.Orders == null)
                trade.Orders = new ObservableCollection<Order>();

            trade.Orders.Add(order);
            order.Trade = trade;
            order.TradeID = trade.ID;

            //finally update the stats of the new trade
            if (updateStats)
            {
                await Task.Run(async () => await UpdateStats(trade).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        public async Task RemoveOrder(Trade trade, Order order)
        {
            if (trade?.Orders == null || !trade.Orders.Contains(order)) return;
            trade.Orders.Remove(order);
            order.Trade = null;
            order.TradeID = null;
            await Task.Run(async () => await UpdateStats(trade).ConfigureAwait(false)).ConfigureAwait(false);
            await UpdateTrade(trade).ConfigureAwait(false);
        }

        public async Task UpdateOpenTrades(DataContainer data)
        {
            foreach (Trade t in data.Trades.Where(x => x.Open))
            {
                try
                {
                    await UpdateStats(t);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "Error updating stats on trade {0} ({1}): {2}", t.Name, t.ID, ex.Message);
                }
            }
        }

        public async Task AddCashTransaction(Trade trade, CashTransaction ct, bool updateStats = true, bool save = true)
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
            await RemoveCashTransaction(oldTrade, ct).ConfigureAwait(false);

            //and then add it to the new one
            if (trade.CashTransactions == null)
                trade.CashTransactions = new List<CashTransaction>();

            trade.CashTransactions.Add(ct);
            ct.Trade = trade;
            ct.TradeID = trade.ID;

            //finally update the stats of the new trade
            if (updateStats)
            {
                await UpdateStats(trade).ConfigureAwait(false);
            }

            if (save)
            {
                await UpdateTrade(trade).ConfigureAwait(false);
            }
        }

        public async Task AddCashTransactions(Trade trade, IEnumerable<CashTransaction> cashTransactions)
        {
            if (trade == null) throw new ArgumentNullException(nameof(trade));
            if (cashTransactions == null) throw new ArgumentNullException(nameof(cashTransactions));

            foreach (var ct in cashTransactions)
            {
                await AddCashTransaction(trade, ct, false, false).ConfigureAwait(false);
            }

            await UpdateStats(trade).ConfigureAwait(false);
            await UpdateTrade(trade).ConfigureAwait(false);
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
            await RemoveFXTransaction(oldTrade, fxt);

            //and then add it to the new one
            if (trade.FXTransactions == null)
                trade.FXTransactions = new List<FXTransaction>();

            trade.FXTransactions.Add(fxt);
            fxt.Trade = trade;
            fxt.TradeID = trade.ID;

            //finally update the stats of the new trade
            await UpdateStats(fxt.Trade);
        }

        public async Task RemoveFXTransaction(Trade trade, FXTransaction fxt)
        {
            if (trade?.FXTransactions == null || !trade.FXTransactions.Contains(fxt)) return;
            trade.FXTransactions.Remove(fxt);
            fxt.Trade = null;
            fxt.TradeID = null;
            await UpdateStats(trade);
        }


        /// <summary>
        /// Does not save the operation
        /// </summary>
        /// <param name="trade"></param>
        /// <returns></returns>
        public async Task Reset(Trade trade)
        {
            using (var dbContext = _contextFactory.Get())
            {
                var dbTrade = await dbContext
                    .Trades
                    .Include(x => x.Orders)
                    .Include(x => x.Tags)
                    .Include(x => x.Strategy)
                    .Include(x => x.FXTransactions)
                    .Include(x => x.CashTransactions)
                    .FirstAsync(x => x.ID == trade.ID).ConfigureAwait(false);

                dbTrade.Orders.Clear();
                dbTrade.CashTransactions.Clear();
                dbTrade.FXTransactions.Clear();
                dbTrade.Tags.Clear();

                await dbContext.SaveChangesAsync();
            }

            if (trade.Orders != null)
            {
                foreach (var order in trade.Orders)
                {
                    order.Trade = null;
                }
            }

            trade.Orders?.Clear();

            if (trade.CashTransactions != null)
            {
                foreach (var ct in trade.CashTransactions)
                {
                    ct.Trade = null;
                }
            }

            trade.CashTransactions?.Clear();

            if (trade.FXTransactions != null)
            {
                foreach (var fxt in trade.FXTransactions)
                {
                    fxt.Trade = null;
                }
            }

            trade.FXTransactions?.Clear();

            trade.Tags?.Clear();

            await UpdateStats(trade);
        }

        public async Task Add(Trade trade)
        {
            using (var dbContext = _contextFactory.Get())
            {
                dbContext.Attach(trade).State = EntityState.Added;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// This method must be used to update a trade in the db
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public async Task UpdateTrade(Trade trade = null, IEnumerable<Trade> trades = null)
        {
            if (trade == null && trades == null) throw new ArgumentException("Exactly one of the two parameters must be given.");
            if (trade != null && trades != null) throw new ArgumentException("Exactly one of the two parameters must be given.");

            if (trades == null)
            {
                trades = new List<Trade> { trade };
            }


            //The reason we need these: when we hit SaveChanges(), EF sets the Trade
            //to the instance we get from the db, not the one we are using locally
            //so we keep track of the objects in order to reverse that change later
            var ordersToUpdate = new List<Order>();
            var fxTransactionsToUpdate = new List<FXTransaction>();
            var cashTransactionsToUpdate = new List<CashTransaction>();

            using (var dbContext = _contextFactory.Get())
            {
                //this is because of some tracking strangeness
                var dbTags = await dbContext.Tags.ToListAsync().ConfigureAwait(false);
                var dbStrategies = await dbContext.Strategies.ToListAsync().ConfigureAwait(false);

                foreach (var t in trades)
                {
                    if (t.ID == 0)
                    {
                        throw new Exception("Trade must already be in db");
                    }


                    var dbTrade = await dbContext
                        .Trades
                        .Include(x => x.Orders)
                        .Include(x => x.Tags)
                        .Include(x => x.Strategy)
                        .Include(x => x.FXTransactions)
                        .Include(x => x.CashTransactions)
                        .FirstAsync(x => x.ID == t.ID).ConfigureAwait(false);

                    //start with all the props
                    dbTrade.Name = t.Name;
                    dbTrade.Open = t.Open;
                    dbTrade.DateOpened = t.DateOpened;
                    dbTrade.DateClosed = t.DateClosed;
                    dbTrade.ResultPct = t.ResultPct;
                    dbTrade.ResultDollars = t.ResultDollars;
                    dbTrade.Commissions = t.Commissions;
                    dbTrade.UnrealizedResultPct = t.UnrealizedResultPct;
                    dbTrade.UnrealizedResultDollars = t.UnrealizedResultDollars;
                    dbTrade.ResultDollarsLong = t.ResultDollarsLong;
                    dbTrade.ResultDollarsShort = t.ResultDollarsShort;
                    dbTrade.ResultPctLong = t.ResultPctLong;
                    dbTrade.ResultPctShort = t.ResultPctShort;
                    dbTrade.UnrealizedResultDollarsLong = t.UnrealizedResultDollarsLong;
                    dbTrade.UnrealizedResultDollarsShort = t.UnrealizedResultDollarsShort;
                    dbTrade.UnrealizedResultPctLong = t.UnrealizedResultPctLong;
                    dbTrade.UnrealizedResultPctShort = t.UnrealizedResultPctShort;
                    dbTrade.CapitalLong = t.CapitalLong;
                    dbTrade.CapitalShort = t.CapitalShort;
                    dbTrade.CapitalTotal = t.CapitalTotal;
                    dbTrade.Notes = t.Notes;

                    // TAGS
                    //this is necessary because EF can't do many-to-many relationships without this hack, and will try to insert duplicate entries in TagMap
                    var toAddTags = t.Tags.Except(dbTrade.Tags).ToList();
                    var toRemoveTags = dbTrade.Tags.Except(t.Tags).ToList();

                    if (toAddTags.Count > 0 || toRemoveTags.Count > 0)
                    {
                        foreach (var tag in toAddTags)
                        {
                            dbTrade.Tags.Add(dbTags.First(x => x.ID == tag.ID));
                        }

                        foreach (var tag in toRemoveTags)
                        {
                            dbTrade.Tags.Remove(tag);
                        }
                    }


                    // ORDERS
                    var toAddOrders = t.Orders.Where(x => !dbTrade.Orders.Any(y => y.ID == x.ID)).ToList();
                    var toRemoveOrders = dbTrade.Orders.Where(x => !t.Orders.Any(y => y.ID == x.ID)).ToList();
                    if (toAddOrders.Count > 0 || toRemoveOrders.Count > 0)
                    {
                        foreach (var order in toAddOrders)
                        {
                            ordersToUpdate.Add(order);
                            dbTrade.Orders.Add(order);
                        }

                        foreach (var order in toRemoveOrders)
                        {
                            dbTrade.Orders.Remove(order);
                        }
                    }


                    // FX TRANSACTIONS
                    var toAddFxts = t.FXTransactions.Where(x => !dbTrade.FXTransactions.Any(y => y.ID == x.ID)).ToList();
                    var toRemoveFxts = dbTrade.FXTransactions.Where(x => !t.FXTransactions.Any(y => y.ID == x.ID)).ToList();
                    if (toAddFxts.Count > 0 || toRemoveFxts.Count > 0)
                    {
                        foreach (var fxt in toAddFxts)
                        {
                            fxTransactionsToUpdate.Add(fxt);
                            dbTrade.FXTransactions.Add(fxt);
                        }

                        foreach (var fxt in toRemoveFxts)
                        {
                            dbTrade.FXTransactions.Remove(fxt);
                        }
                    }

                    // CASH TRANSACTIONS
                    var toAddCts = t.CashTransactions.Where(x => !dbTrade.CashTransactions.Any(y => y.ID == x.ID)).ToList();
                    var toRemoveCts = dbTrade.CashTransactions.Where(x => !t.CashTransactions.Any(y => y.ID == x.ID)).ToList();
                    if (toAddCts.Count > 0 || toRemoveCts.Count > 0)
                    {
                        foreach (var ct in toAddCts)
                        {
                            cashTransactionsToUpdate.Add(ct);
                            dbTrade.CashTransactions.Add(ct);
                        }

                        foreach (var ct in toRemoveCts)
                        {
                            dbTrade.CashTransactions.Remove(ct);
                        }
                    }

                    // STRATEGY
                    if (dbTrade.StrategyID != t.StrategyID)
                    {
                        if (!t.StrategyID.HasValue || t.StrategyID.Value == 0)
                        {
                            dbTrade.Strategy = null;
                        }
                        else
                        {
                            dbTrade.Strategy = dbStrategies.First(x => x.ID == t.StrategyID);
                        }
                    }

                    dbContext.Entry(dbTrade).State = EntityState.Modified;
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            foreach (var order in ordersToUpdate)
            {
                order.Trade = trades.First(x => x.ID == order.Trade.ID);
            }

            foreach (var ct in cashTransactionsToUpdate)
            {
                ct.Trade = trades.First(x => x.ID == ct.Trade.ID);
            }

            foreach (var fxt in fxTransactionsToUpdate)
            {
                fxt.Trade = trades.First(x => x.ID == fxt.Trade.ID);
            }
        }
    }
}
