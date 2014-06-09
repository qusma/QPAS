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

        private decimal _totalPnL;

        public TradeTracker(Trade trade)
        {
            Positions = new Dictionary<int, Position>();
            CurrencyPositions = new Dictionary<int, CurrencyPosition>();

            CumulativeReturns = new SortedList<DateTime, double>();
            CumulativePnL = new SortedList<DateTime, decimal>();

            Capital = new AllocatedCapital();

            _ordersRemaining = trade.Orders == null ? 0 : trade.Orders.Count;

            Trade = trade;

            _currentEquity = 1;
            _totalPnL = 0;
            TodaysPnL = 0;
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
        public void AddCashTransaction(CashTransaction ct)
        {
            if (!ct.InstrumentID.HasValue) return;

            if (Positions.ContainsKey(ct.Instrument.ID))
                Positions[ct.Instrument.ID].AddCashTransaction(ct);

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
                TodaysPnL += p.GetPnL(data[id].CurrentBar < 0 ? (decimal?)null : data[id][0].Close, fxRate);
            }

            //Update currency positions
            foreach(var kvp in CurrencyPositions)
            {
                int id = kvp.Key;
                if (fxData[id].CurrentBar < 0) continue;

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
                (_ordersRemaining > 0 && _ordersRemaining < Trade.Orders.Count);
        }
    }
}