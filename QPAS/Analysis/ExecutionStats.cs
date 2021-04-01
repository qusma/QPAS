// -----------------------------------------------------------------------
// <copyright file="ExecutionStats.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System;

namespace QPAS
{
    public class ExecutionStats
    {
        public string BuySell;
        public decimal Commission;

        public decimal CommissionPct;

        public string OrderType;

        /// <summary>
        /// The difference in cents between the reference price and the execution price;
        /// </summary>
        public decimal Slippage;

        public double SlippagePct;

        /// <summary>
        /// The difference in seconds between the reference time and execution time.
        /// </summary>
        public double TimeDiff;

        public string Venue;

        public int Quantity;

        public decimal Price;

        public decimal Value { get { return Quantity * Price; } }

        public ExecutionStats(Execution ex, decimal referencePrice, DateTime referenceTime)
        {
            BuySell = ex.BuySell;
            Venue = ex.Exchange;
            OrderType = ex.OrderType;

            if (ex.BuySell == "BUY")
                Slippage = (ex.Price - referencePrice) * ex.Multiplier * ex.FXRateToBase;
            else
                Slippage = (referencePrice - ex.Price) * ex.Multiplier * ex.FXRateToBase;

            SlippagePct = (double)(Slippage / (ex.Price * ex.Multiplier * ex.FXRateToBase));
            Commission = -(ex.CommissionInBase / Math.Abs(ex.Quantity));
            CommissionPct = Commission / ex.Price;

            TimeDiff = (ex.TradeDate - referenceTime).TotalSeconds;

            Quantity = ex.Quantity;
            Price = ex.Price * ex.FXRateToBase * ex.Multiplier;
        }
    }
}