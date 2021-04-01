// -----------------------------------------------------------------------
// <copyright file="CurrencyPosition.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using NLog;
using System;
using System.Collections.Generic;

namespace QPAS
{
    public class CurrencyPosition
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public Currency Currency { get; set; }

        public decimal Quantity { get; set; }

        public List<FXTransaction> Transactions { get; set; }

        public decimal RealizedPnL { get; set; }

        public decimal UnrealizedPnL
        {
            get
            {
                return TotalPnL - RealizedPnL;
            }
        }

        /// <summary>
        /// Realized plus unrealized profit/loss.
        /// </summary>
        public decimal TotalPnL { get; set; }

        /// <summary>
        /// Keeps track of cost basis based on transactions.
        /// </summary>
        public decimal CostBasis { get; set; }

        /// <summary>
        /// Cost basis updated daily to keep track of daily profit/loss.
        /// </summary>
        public decimal PriorPeriodCostBasis { get; set; }

        private decimal _unrecognizedPnL;

        public CurrencyPosition(Currency currency)
        {
            Currency = currency;
            Transactions = new List<FXTransaction>();
            _unrecognizedPnL = 0;
        }

        public void AddFXTransaction(FXTransaction transaction)
        {
            if (transaction.FXCurrency.ID != Currency.ID)
            {
                throw new Exception("Incorrect currency used.");
            }

            Transactions.Add(transaction);

            decimal fxRate = transaction.Proceeds / transaction.Quantity;

            //profit/loss
            if ((transaction.Quantity < 0 && Quantity > 0) ||
                (transaction.Quantity > 0 && Quantity < 0))
            {
                decimal profit = -Math.Min(Math.Abs(transaction.Quantity), Math.Abs(Quantity)) *
                    (fxRate - CostBasis) *
                    Math.Sign(transaction.Quantity);

                RealizedPnL += profit;
                _unrecognizedPnL += profit;
            }

            //update cost basis
            if (Quantity == 0)
            {
                //new position
                CostBasis = fxRate;
                PriorPeriodCostBasis = fxRate;
            }
            else if (Math.Sign(transaction.Quantity) == Math.Sign(Quantity))
            {
                //adding to existing position
                CostBasis = (Quantity * CostBasis + transaction.Quantity * fxRate) / (Quantity + transaction.Quantity);
                PriorPeriodCostBasis = (Quantity * PriorPeriodCostBasis + transaction.Quantity * fxRate) / (Quantity + transaction.Quantity);
            }
            else if (Math.Abs(transaction.Quantity) > Math.Abs(Quantity))
            {
                //removing from position...if we're reversing it, it's as if it's a new position
                CostBasis = fxRate;
                PriorPeriodCostBasis = fxRate;
            }

            Quantity += transaction.Quantity;
        }

        public decimal Update(decimal fxRate)
        {
            decimal todaysPnL = _unrecognizedPnL + Quantity * (fxRate - PriorPeriodCostBasis);
            TotalPnL += todaysPnL;
            _unrecognizedPnL = 0;

#if DEBUG
            _logger.Log(LogLevel.Trace, string.Format("Currency position for {0}, today's P/L: {1:0.00}",
                Currency.Name,
                todaysPnL));
#endif

            PriorPeriodCostBasis = fxRate;

            return todaysPnL;
        }
    }
}