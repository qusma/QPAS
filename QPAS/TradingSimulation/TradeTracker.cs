// -----------------------------------------------------------------------
// <copyright file="TradeTracker.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace QPAS
{
    /// <summary>
    /// Tracks daily evolution of a single trade's performance,
    /// giving daily cumulative return and max favorable/adverse excursion stats.
    /// </summary>
    public class TradeTracker
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();

        private double _currentEquity;

        private int _ordersRemaining;
        private int _cashTransactionsRemaining;

        private decimal _totalPnL;

        public TradeTracker(Trade trade)
        {
            Positions = new Dictionary<int, Position>()
            {
                //dummy position used for cash transactions without a related instrument
                { NullInstrumentId, new Position(new Instrument())} 
            };
            CurrencyPositions = new Dictionary<int, CurrencyPosition>();

            CumulativeReturns = new SortedList<DateTime, double>();
            CumulativePnL = new SortedList<DateTime, decimal>();

            Capital = new AllocatedCapital();

            _ordersRemaining = trade.Orders == null ? 0 : trade.Orders.Count;
            _cashTransactionsRemaining = trade.CashTransactions == null ? 0 : trade.CashTransactions.Count;

            Trade = trade;

            _currentEquity = 1;
            _totalPnL = 0;
            TodaysPnL = 0;
        }

        public void SetTradeStats(Trade trade)
        {
            var positions = Positions.Values;

            //Capital usage stats
            trade.CapitalTotal =
                Capital.Gross.Count(x => x > 0) > 0
                    ? Capital.Gross.Where(x => x > 0).Average()
                    : 0;

            trade.CapitalLong =
                Capital.Long.Count(x => x > 0) > 0
                    ? Capital.Long.Where(x => x > 0).Average()
                    : 0;

            trade.CapitalShort =
                Capital.Short.Count(x => x > 0) > 0
                    ? Capital.Short.Where(x => x > 0).Average()
                    : 0;

            trade.CapitalNet = trade.CapitalLong - trade.CapitalShort;

            //Realized dollar result stats
            trade.ResultDollars = RealizedPnL;
            trade.ResultDollarsLong = RealizedPnLLong;
            trade.ResultDollarsShort = RealizedPnLShort;

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
            trade.UnrealizedResultDollars = TotalPnL - RealizedPnL;
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

        public AllocatedCapital Capital { get; set; }

        public SortedList<DateTime, decimal> CumulativePnL { get; set; }

        public SortedList<DateTime, double> CumulativeReturns { get; set; }

        public Dictionary<int, CurrencyPosition> CurrencyPositions { get; set; }

        public double MaxAdverseExcursion
        {
            get { return CumulativeReturns.Count > 0 ? Math.Min(1, CumulativeReturns.Values.Min()) - 1 : 0; }
        }

        public double MaxFavorableExcursion
        {
            get { return CumulativeReturns.Count > 0 ? Math.Max(1, CumulativeReturns.Values.Max()) - 1 : 0; }
        }

        public bool Open { get; set; }
        
        public Dictionary<int, Position> Positions { get; set; }

        public decimal TodaysPnL { get; set; }

        /// <summary>
        /// Realized + Unrealized PnL from long positions
        /// </summary>
        public decimal TotalPnlLong 
        { 
            get
            {
                return Positions.Sum(x => x.Value.PnLLong);
            }
        }

        /// <summary>
        /// Realized + Unrealized PnL from short positions
        /// </summary>
        public decimal TotalPnlShort
        {
            get
            {
                return Positions.Sum(x => x.Value.PnLShort);
            }
        }

        /// <summary>
        /// Realized + unrealized profit/loss for all positions and currency positions.
        /// </summary>
        public decimal TotalPnL
        {
            get
            {
                return Positions.Sum(x => x.Value.RealizedPnL + x.Value.UnrealizedPnL) + CurrencyPositions.Sum(x => x.Value.TotalPnL);
            }
        }

        public decimal RealizedPnLLong { get { return Positions.Sum(x => x.Value.RealizedPnLLong); } }
        public decimal RealizedPnLShort { get { return Positions.Sum(x => x.Value.RealizedPnLShort); } }
        public decimal RealizedPnL { get { return Positions.Sum(x => x.Value.RealizedPnL) + CurrencyPositions.Sum(x => x.Value.RealizedPnL); } }

        public Trade Trade { get; set; }

        private const int NullInstrumentId = -99;

        public void AddCashTransaction(CashTransaction ct)
        {
            Open = true;

            if (ct.InstrumentID.HasValue)
            {
                if (Positions.ContainsKey(ct.Instrument.ID))
                    Positions[ct.Instrument.ID].AddCashTransaction(ct);
            }
            else
            {
                //InstrumentID is null. This happens frequently 
                //as many cash transactions are not related to a particular instrument
                Positions[NullInstrumentId].AddCashTransaction(ct);
            }

            if(ct.CurrencyID > 1)
            {
                var ft = new FXTransaction
                {
                    FXCurrency = ct.Currency,
                    FXCurrencyID = ct.CurrencyID,
                    Quantity = ct.Amount,
                    Proceeds = ct.Amount * ct.FXRateToBase,
                    Cost = -ct.Amount * ct.FXRateToBase,
                    DateTime = ct.TransactionDate
                };
                AddFXTransaction(ft);
            }

            _cashTransactionsRemaining--;
        }

        public void AddFXTransaction(FXTransaction ft)
        {
            Open = true;

            if (!CurrencyPositions.ContainsKey(ft.FXCurrency.ID))
                CurrencyPositions.Add(ft.FXCurrency.ID, new CurrencyPosition(ft.FXCurrency));

            CurrencyPositions[ft.FXCurrency.ID].AddFXTransaction(ft);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="o"></param>
        /// <returns>Capital usage by this order.</returns>
        public decimal AddOrder(Order o)
        {
            Open = true;

            if (!Positions.ContainsKey(o.Instrument.ID))
                Positions.Add(o.Instrument.ID, new Position(o.Instrument));

            decimal orderCapUsage = Positions[o.Instrument.ID].AddOrder(o);

            _ordersRemaining--;

            if (o.CurrencyID > 1)
            {
                decimal quantity = -o.Quantity * o.Multiplier * o.Price;
                var ft = new FXTransaction
                {
                    FXCurrency = o.Currency,
                    FXCurrencyID = o.CurrencyID,
                    Quantity = quantity,
                    Proceeds = quantity * o.FXRateToBase,
                    Cost = -quantity * o.FXRateToBase,
                    DateTime = o.TradeDate
                };
                AddFXTransaction(ft);
            }

            return orderCapUsage;
        }

        public void Update(DateTime currentDate, Dictionary<int, TimeSeries> data, Dictionary<int, TimeSeries> fxData)
        {
            TodaysPnL = 0;
            if (!Open) return;

            //Update positions
            foreach (var kvp in Positions)
            {
                int id = kvp.Key;

                Position p = kvp.Value;
                decimal fxRate = p.Currency == null || p.Currency.ID <= 1 ? 1 : fxData[p.Currency.ID][0].Close;
                decimal? lastPrice = !data.ContainsKey(id) || data[id].CurrentBar < 0 ? (decimal?)null : data[id][0].Close;
                TodaysPnL += p.GetPnL(lastPrice, fxRate);
            }

            //Update currency positions
            foreach(var kvp in CurrencyPositions)
            {
                int id = kvp.Key;
                if (fxData == null || fxData[id].CurrentBar < 0) continue;

                CurrencyPosition p = kvp.Value;
                decimal fxRate = fxData[id][0].Close;
                TodaysPnL += p.Update(fxRate);
            }

            if (Positions.Any(x => x.Value.Capital.Gross.Count > 0))
            {
                Capital.AddLong(Positions.Sum(x => x.Value.Capital.Long.Last()));
                Capital.AddShort(Positions.Sum(x => x.Value.Capital.Short.Last()));
            }

            if (Capital.TodaysCapitalGross != 0)
                _currentEquity *= (double)(1 + TodaysPnL / Capital.TodaysCapitalGross);
#if DEBUG
            _logger.Log(LogLevel.Trace, string.Format("Trade tracker ID {0} @ {1}, todays capital usage {2:0.00}, P/L: {3:0.00}",
                Trade.ID,
                currentDate,
                Capital.TodaysCapitalGross,
                TodaysPnL));
#endif

            Capital.EndOfDay();

            _totalPnL += TodaysPnL;

            CumulativeReturns.Add(currentDate, _currentEquity);
            CumulativePnL.Add(currentDate, _totalPnL);

            Open = Positions.Values.Sum(x => x.Quantity) != 0 || 
                CurrencyPositions.Values.Sum(x => x.Quantity) != 0 ||
                (_ordersRemaining > 0 && _ordersRemaining < Trade.Orders.Count) ||
                (_cashTransactionsRemaining > 0 && _cashTransactionsRemaining < Trade.CashTransactions.Count);
        }
    }
}