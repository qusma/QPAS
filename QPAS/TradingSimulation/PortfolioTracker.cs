// -----------------------------------------------------------------------
// <copyright file="PortfolioTracker.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    /// <summary>
    /// This class groups together trades into a "portfolio":
    /// could be a single strategy, or the entire account, or something else.
    /// Orders, cash trasactions, fx transactions are tracked and
    /// equity curves generated.
    /// </summary>
    public class PortfolioTracker
    {
        public EquityCurve ProfitLossEquityCurve { get; set; }
        public EquityCurve ProfitLossLongEquityCurve { get; set; }
        public EquityCurve ProfitLossShortEquityCurve { get; set; }
        public EquityCurve RoacEquityCurve { get; set; }
        public EquityCurve RotcEquityCurve { get; set; }

        public AllocatedCapital Capital { get; set; }

        public string Name { get; private set; }

        public Dictionary<int, Position> Positions { get; set; }
        //todo maybe add FX positions at the portfolio level, too?

        /// <summary>
        /// Key: instrument ID
        /// </summary>
        private readonly Dictionary<int, TimeSeries> _data;

        private readonly Dictionary<int, TimeSeries> _fxData;

        private readonly Dictionary<DateTime, List<CashTransaction>> _cashTransactionsByDate;
        private readonly Dictionary<DateTime, List<FXTransaction>> _fxTransactionsByDate;
        private readonly List<Order> _allOrders;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Sometimes we have profits/losses while allocated capital is zero.
        /// So we defer that p/l and add it when capital is eventually allocated.
        /// </summary>
        private decimal _deferredPnL;

        public List<Trade> Trades { get; private set; }
        public Dictionary<int, TradeTracker> TradeTrackers { get; private set; }

        private const int NullInstrumentId = -99;
        private decimal _optionsCapitalUsageMultiplier;

        public PortfolioTracker(Dictionary<int, TimeSeries> data, Dictionary<int, TimeSeries> fxData, List<Trade> trades, string name, DateTime firstDate, decimal optionsCapitalUsageMultiplier)
        {
            _data = data;
            _fxData = fxData;
            Name = name;
            _optionsCapitalUsageMultiplier = optionsCapitalUsageMultiplier;
            Trades = trades;

            TradeTrackers = trades.ToDictionary(t => t.ID, t => new TradeTracker(t, optionsCapitalUsageMultiplier));

            ProfitLossEquityCurve = new EquityCurve();
            ProfitLossLongEquityCurve = new EquityCurve();
            ProfitLossShortEquityCurve = new EquityCurve();

            RoacEquityCurve = new EquityCurve(1, firstDate.Date.AddDays(-1));
            RotcEquityCurve = new EquityCurve(1, firstDate.Date.AddDays(-1));

            Capital = new AllocatedCapital();

            Positions = new Dictionary<int, Position>
            {
                //dummy position used for cash transactions without a related instrument
                { NullInstrumentId, new Position(new Instrument(), _optionsCapitalUsageMultiplier)}
            };

            //group cash transactions by date so they're easily accessible
            _cashTransactionsByDate =
                trades
                .Where(x => x.CashTransactions != null)
                .SelectMany(x => x.CashTransactions)
                .Where(x => x.Type != "Deposits & Withdrawals")
                .GroupBy(x => x.TransactionDate.Date)
                .ToDictionary(x => x.Key, x => x.ToList());

            //group fx transactions
            _fxTransactionsByDate =
                trades
                .Where(x => x.FXTransactions != null)
                .SelectMany(x => x.FXTransactions)
                .GroupBy(x => x.DateTime.Date)
                .ToDictionary(x => x.Key, x => x.ToList());

            _allOrders = Trades.Where(x => x.Orders != null).SelectMany(x => x.Orders).ToList();
        }

        public void ProcessItemsAt(DateTime date)
        {
            //select the orders from today
            var todaysOrders = _allOrders.Where(x => x.TradeDate.Date == date).ToList();

            foreach (Order o in todaysOrders)
            {
                //Add orders to their respective trades
                if (o.TradeID.HasValue)
                {
                    TradeTrackers[o.TradeID.Value].AddOrder(o);
                }

                //add orders to positions
                if (!Positions.ContainsKey(o.InstrumentID))
                {
                    Positions.Add(o.InstrumentID, new Position(o.Instrument, _optionsCapitalUsageMultiplier));
                }
                Positions[o.InstrumentID].AddOrder(o);
            }

            AddTodaysCashTransactions(date);
            AddTodaysFxTransactions(date);
        }

        private void AddTodaysFxTransactions(DateTime date)
        {
            if (!_fxTransactionsByDate.ContainsKey(date.Date)) return;

            foreach (FXTransaction fxt in _fxTransactionsByDate[date.Date])
            {
                //add to trade
                TradeTrackers[fxt.TradeID.Value].AddFXTransaction(fxt);
            }
        }

        /// <summary>
        /// Add the CTs to the trade trackers
        /// </summary>
        private void AddTodaysCashTransactions(DateTime date)
        {
            if (!_cashTransactionsByDate.ContainsKey(date.Date)) return;

            foreach (CashTransaction ct in _cashTransactionsByDate[date.Date].Where(x => x.TradeID.HasValue))
            {
                //add to trade
                TradeTrackers[ct.TradeID.Value].AddCashTransaction(ct);

                //add to position
                if (ct.InstrumentID.HasValue)
                {
                    if (!Positions.ContainsKey(ct.InstrumentID.Value))
                    {
                        Positions.Add(ct.InstrumentID.Value, new Position(ct.Instrument, _optionsCapitalUsageMultiplier));
                    }
                    Positions[ct.InstrumentID.Value].AddCashTransaction(ct);
                }
                else
                {
                    Positions[NullInstrumentId].AddCashTransaction(ct);
                }
            }
        }

        public void OnDayClose(DateTime todaysDate, decimal totalCapitalToday)
        {
            //update the status of each trade
            foreach (TradeTracker t in TradeTrackers.Values)
            {
                t.Update(todaysDate, _data, _fxData);
            }

            //update position stats
            foreach (var kvp in Positions)
            {
                int id = kvp.Key;

                Position p = kvp.Value;
                decimal fxRate = p.Currency == null || p.Currency.ID == 1 ? 1 : _fxData[p.Currency.ID][0].Close;
                decimal? lastPrice = GetLastPrice(id, todaysDate);
                p.GetPnL(lastPrice, fxRate);
            }

            //Capital usage and profit/loss for the day
            Capital.AddLong(Positions.Sum(x => x.Value.Capital.Long.Last()));
            Capital.AddShort(Positions.Sum(x => x.Value.Capital.Short.Last()));
            decimal todaysPnl = TradeTrackers.Sum(x => x.Value.TodaysPnL);

            _logger.Log(LogLevel.Trace, string.Format("Portfolio {0} @ {1}: Capital used: {2:0.00} P/L: {3:0.00}",
                Name,
                todaysDate,
                Capital.TodaysCapitalGross,
                todaysPnl));

            //P/L curves
            ProfitLossEquityCurve.AddChange((double)todaysPnl, todaysDate);
            ProfitLossLongEquityCurve.AddValue((double)TradeTrackers.Sum(x => x.Value.TotalPnlLong), todaysDate);
            ProfitLossShortEquityCurve.AddValue((double)TradeTrackers.Sum(x => x.Value.TotalPnlShort), todaysDate);

            //ROAC
            if (Capital.TodaysCapitalGross == 0)
            {
                _deferredPnL += todaysPnl;
                RoacEquityCurve.AddReturn(0, todaysDate);
            }
            else
            {
                RoacEquityCurve.AddReturn((double)((_deferredPnL + todaysPnl) / Capital.TodaysCapitalGross), todaysDate);
                _deferredPnL = 0;
            }

            //ROTC
            if (totalCapitalToday == 0)
            {
                RotcEquityCurve.AddReturn(0, todaysDate);
            }
            else
            {
                RotcEquityCurve.AddReturn((double)(todaysPnl / totalCapitalToday), todaysDate);
            }

            Capital.EndOfDay();
        }

        private decimal? GetLastPrice(int id, DateTime todaysDate)
        {
            if (!_data.ContainsKey(id) || _data[id].CurrentBar < 0)
            {
                //we have nothing
                return null;
            }

            if (_data[id][0].Date.ToDateTimeUnspecified().Date != todaysDate.Date)
            {
                //if there is no proper data and we are using the PriorPositions, this can be wrong and cause a lot of trouble
                //in that case, just use the trade price
                return null;
            }

            //everything's alright, return today's closing price
            return _data[id][0].Close;
        }
    }
}