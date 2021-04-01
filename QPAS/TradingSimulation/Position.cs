// -----------------------------------------------------------------------
// <copyright file="Position.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System;
using System.Collections.Generic;

namespace QPAS
{
    public class Position
    {
        /// <summary>
        /// Order quantities (and associated prices) whose resulting positions have not been closed yet.
        /// Used to calculate FIFO PnL when new orders are added.
        /// </summary>
        private readonly List<Tuple<decimal, int>> _openPositions;

        /// <summary>
        /// When we have zero capital allocated but still have costs like commissions,
        /// keep it in deferred pnl until it's accounted for.
        /// </summary>
        private decimal _deferredPnL;

        private decimal _priorPeriodQuantity;

        private decimal _unrecognizedPnLLong;

        private decimal _unrecognizedPnLShort;

        /// <summary>
        /// We don't want to count capital usage for orders after a certain cutoff (toward the end of the day).
        /// Unless it's actually an intraday trade that we enter and exit within the cutoff period.
        /// The following two fields keep track of that stuff.
        /// </summary>
        private decimal _quantityEnteredAfterEodCutoff;

        private decimal _avgPriceEnteredAfterEodCutoff;

        public Position(Instrument instrument, decimal optionsCapitalUsageMultiplier = 1)
        {
            Instrument = instrument;
            Orders = new List<Order>();
            _openPositions = new List<Tuple<decimal, int>>();

            Capital = new AllocatedCapital();

            PnLLong = 0;
            PnLShort = 0;

            RealizedPnLLong = 0;
            RealizedPnLShort = 0;

            _unrecognizedPnLLong = 0;
            _unrecognizedPnLShort = 0;

            _priorPeriodQuantity = 0;
            _deferredPnL = 0;

            ROAC = 1;
            _optionsCapitalUsageMultiplier = optionsCapitalUsageMultiplier;
        }

        public AllocatedCapital Capital { get; private set; }

        /// <summary>
        /// The cost basis of the position.
        /// </summary>
        public decimal CostBasis { get; set; }

        public Currency Currency { get; set; }

        public decimal FXRateBasis { get; set; }

        public Instrument Instrument { get; set; }

        public decimal LastPrice { get; set; }

        public List<Order> Orders { get; set; }

        /// <summary>
        /// Realized + Unrealized PnL Long
        /// </summary>
        public decimal PnL { get { return PnLLong + PnLShort; } }

        /// <summary>
        /// Realized + Unrealized PnL Long
        /// </summary>
        public decimal PnLLong { get; private set; }

        /// <summary>
        /// Realized + Unrealized PnL Short
        /// </summary>
        public decimal PnLShort { get; private set; }

        /// <summary>
        /// The cost basis from the last price update. Used to calculate capital usage.
        /// </summary>
        public decimal PriorPeriodCostBasis { get; set; }

        public decimal PriorPeriodFXRateBasis { get; set; }

        public decimal Quantity { get; set; }

        /// <summary>
        /// Realized PnL
        /// </summary>
        public decimal RealizedPnL { get { return RealizedPnLLong + RealizedPnLShort; } }

        /// <summary>
        /// Realized PnL Long
        /// </summary>
        public decimal RealizedPnLLong { get; private set; }

        /// <summary>
        /// Realized PnL Short
        /// </summary>
        public decimal RealizedPnLShort { get; private set; }

        public double ROAC { get; private set; }

        public decimal UnrealizedPnL { get { return PnL - RealizedPnL; } }

        private decimal _optionsCapitalUsageMultiplier;

        private decimal GetCapitalUsageMultiplier(AssetClass ac)
        {
            if (ac == AssetClass.Option || ac == AssetClass.FutureOption)
            {
                return _optionsCapitalUsageMultiplier;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Add a cash transaction to this position.
        /// </summary>
        public void AddCashTransaction(CashTransaction ct)
        {
            if (Currency == null)
            {
                Currency = ct.Currency;
            }

            decimal amt = ct.Amount * ct.FXRateToBase;
            if (Quantity < 0)
            {
                _unrecognizedPnLShort += amt;
                RealizedPnLShort += amt;
            }
            else
            {
                _unrecognizedPnLLong += amt;
                RealizedPnLLong += amt;
            }
        }

        /// <summary>
        /// Add an order to this position.
        /// </summary>
        /// <returns>The capital usage of this order</returns>
        public decimal AddOrder(Order order)
        {
            if (Instrument.ID != order.Instrument.ID) throw new Exception("Incorrect instrument for position.");

            if (Currency == null) Currency = order.Currency;

            Orders.Add(order);

            //Commissions
            HandleCommissions(order);

            //Profits if we're exiting
            CalculateProfits(order);
            CalculateDaysPnL(order);

            //Cost basis
            UpdateCostBasis(order);

            //Set the order's FIFO PnL
            SetFIFOPnL(order);

            decimal capitalUsage = CalculateCapitalUsage(order);

            Quantity += order.Quantity;

            return capitalUsage;
        }

        private decimal CalculateCapitalUsage(Order order)
        {
            decimal capitalUsage = 0;
            //If the order is toward the end of the day, we don't want to count it for the day's capital usage
            var timeLimit = new TimeSpan(15, 40, 00);
            if (order.TradeDate.TimeOfDay < timeLimit)
            {
                capitalUsage = GetCapitalUsage(order);
            }
            else
            {
                //Unless! It's an intraday trade which we entered and exited after the cutoff,
                //in which case we want to count it
                if (Quantity == 0 || Math.Sign(Quantity) == Math.Sign(order.Quantity))
                {
                    //Adding to a position
                    //We just want to keep track of this, no reason to add capital usage yet
                    _avgPriceEnteredAfterEodCutoff =
                        (Math.Abs(order.Quantity) * GetCapitalUsagePriceBasis(order.Price, order.FXRateToBase) + Math.Abs(_quantityEnteredAfterEodCutoff) * _avgPriceEnteredAfterEodCutoff)
                        / Math.Abs(order.Quantity + _quantityEnteredAfterEodCutoff);
                    _quantityEnteredAfterEodCutoff += order.Quantity;
                }
                else if (_quantityEnteredAfterEodCutoff != 0)
                {
                    //Exiting a position opened after the cutoff
                    decimal originalQuantityEntered = _quantityEnteredAfterEodCutoff;
                    if (order.Quantity > 0)
                    {
                        //exiting a short
                        capitalUsage = Math.Min(order.Quantity, -_quantityEnteredAfterEodCutoff) * _avgPriceEnteredAfterEodCutoff;
                        Capital.AddShort(capitalUsage);
                        _quantityEnteredAfterEodCutoff -= Math.Min(order.Quantity, -_quantityEnteredAfterEodCutoff);
                    }
                    else
                    {
                        //exiting a long
                        capitalUsage = Math.Min(-order.Quantity, _quantityEnteredAfterEodCutoff) * _avgPriceEnteredAfterEodCutoff;
                        Capital.AddLong(capitalUsage);
                        _quantityEnteredAfterEodCutoff -= Math.Min(-order.Quantity, _quantityEnteredAfterEodCutoff);
                    }

                    //Did we actually reverse the position?
                    if (Math.Abs(order.Quantity) > Math.Abs(originalQuantityEntered))
                    {
                        _quantityEnteredAfterEodCutoff = Math.Sign(order.Quantity) * (Math.Abs(order.Quantity) - Math.Abs(originalQuantityEntered));
                        _avgPriceEnteredAfterEodCutoff = GetCapitalUsagePriceBasis(order.Price, order.FXRateToBase);
                    }
                }
            }

            return capitalUsage;
        }

        /// <summary>
        /// Calculates profit/loss given a new price and FX rate. Also updates ROAC.
        /// </summary>
        /// <returns>The profit/loss since the last GetPnL() call.</returns>
        public decimal GetPnL(decimal? newPrice, decimal newFXRate)
        {
            //Null: this happens if there is no data on this anywhere.
            //Can happen when there is no prior position on an instrument.
            decimal updatePrice = newPrice ?? PriorPeriodCostBasis;

            var capitalUsage = GetCapitalUsage(_priorPeriodQuantity, PriorPeriodCostBasis, PriorPeriodFXRateBasis);
            if (_priorPeriodQuantity > 0)
            {
                Capital.AddLong(capitalUsage);
            }
            else
            {
                Capital.AddShort(capitalUsage);
            }

            //the change in value from the previous day
            if (Quantity > 0)
                _unrecognizedPnLLong += Instrument.Multiplier * Quantity * (updatePrice * newFXRate - PriorPeriodCostBasis * PriorPeriodFXRateBasis);
            else
                _unrecognizedPnLShort += Instrument.Multiplier * Quantity * (updatePrice * newFXRate - PriorPeriodCostBasis * PriorPeriodFXRateBasis);

            //calculate ROAC
            if (Capital.TodaysCapitalGross > 0)
            {
                ROAC = ROAC * (double)(1 + (_unrecognizedPnLLong + _unrecognizedPnLShort + _deferredPnL) / Capital.TodaysCapitalGross);
                _deferredPnL = 0;
            }
            else
            {
                //if no capital is deployed, ROAC calculation is impossible
                //so we just defer the profit/loss until some capital usage exists to calculate ROAC on
                //This happens when position is zero, but we get a cash transaction for example.
                _deferredPnL += _unrecognizedPnLLong + _unrecognizedPnLShort;
            }

            LastPrice = updatePrice;

            _priorPeriodQuantity = Quantity;

            //prior period cost basis is updated to reflect the latest prices
            PriorPeriodCostBasis = updatePrice;
            PriorPeriodFXRateBasis = newFXRate;

            //Total p/l is updated with today's profit/loss
            PnLLong += _unrecognizedPnLLong;
            PnLShort += _unrecognizedPnLShort;

            decimal toReturn = _unrecognizedPnLLong + _unrecognizedPnLShort;
            _unrecognizedPnLLong = 0;
            _unrecognizedPnLShort = 0;
            _quantityEnteredAfterEodCutoff = 0;
            _avgPriceEnteredAfterEodCutoff = 0;

            Capital.EndOfDay();

            return toReturn;
        }

        /// <summary>
        /// If the given order results in exiting a position, add the profit/loss to the day's p/l
        /// </summary>
        private void CalculateDaysPnL(Order order)
        {
            decimal daysPnL = -Instrument.Multiplier * Math.Min(Math.Abs(order.Quantity), Math.Abs(Quantity)) *
                                (order.Price * order.FXRateToBase - PriorPeriodCostBasis * PriorPeriodFXRateBasis) *
                                Math.Sign(order.Quantity);
            if (order.Quantity < 0 && Quantity > 0)
            {
                _unrecognizedPnLLong += daysPnL;
            }
            else if (order.Quantity > 0 && Quantity < 0)
            {
                _unrecognizedPnLShort += daysPnL;
            }
        }

        /// <summary>
        /// If the given order results in exiting a position, add the profit/loss to the realized p/l
        /// </summary>
        private void CalculateProfits(Order order)
        {
            decimal profit = -Instrument.Multiplier * Math.Min(Math.Abs(order.Quantity), Math.Abs(Quantity)) *
                                (order.Price * order.FXRateToBase - CostBasis * FXRateBasis) *
                                Math.Sign(order.Quantity);
            if (order.Quantity < 0 && Quantity > 0)
            {
                RealizedPnLLong += profit;
            }
            else if (order.Quantity > 0 && Quantity < 0)
            {
                RealizedPnLShort += profit;
            }
        }

        /// <summary>
        /// Returns capital usage for the position, given a quantity/costbasis/fx rate
        /// </summary>
        private decimal GetCapitalUsage(decimal quantity, decimal costBasis, decimal fxRate)
        {
            return Math.Abs(quantity) * GetCapitalUsagePriceBasis(costBasis, fxRate);
        }

        private decimal GetCapitalUsagePriceBasis(decimal costBasis, decimal fxRate)
        {
            if (Instrument.AssetCategory == AssetClass.Option || Instrument.AssetCategory == AssetClass.FutureOption)
            {
                return fxRate * Instrument.Strike * Instrument.Multiplier * GetCapitalUsageMultiplier(Instrument.AssetCategory);
            }
            else
            {
                return fxRate * Instrument.Multiplier * costBasis;
            }
        }

        /// <summary>
        /// Attributes the commissions for the given order to long or short profit/loss depending on our position.
        /// </summary>
        private void HandleCommissions(Order order)
        {
            if (order.Quantity > 0) //buying
            {
                if (Quantity >= 0) //Adding a long
                {
                    _unrecognizedPnLLong += order.CommissionInBase;
                    RealizedPnLLong += order.CommissionInBase;
                }
                else if (order.Quantity > Math.Abs(Quantity)) //reversing a short position
                {
                    //partially attribute pnl to long/short proportionately
                    decimal portion = Math.Abs(Quantity) / order.Quantity;
                    _unrecognizedPnLShort += portion * order.CommissionInBase;
                    RealizedPnLShort += portion * order.CommissionInBase;

                    _unrecognizedPnLLong += (1 - portion) * order.CommissionInBase;
                    RealizedPnLLong += (1 - portion) * order.CommissionInBase;
                }
                else //covering a short
                {
                    _unrecognizedPnLShort += order.CommissionInBase;
                    RealizedPnLShort += order.CommissionInBase;
                }
            }
            else //selling
            {
                if (Quantity <= 0) //Adding a short
                {
                    _unrecognizedPnLShort += order.CommissionInBase;
                    RealizedPnLShort += order.CommissionInBase;
                }
                else if (Math.Abs(order.Quantity) > Quantity) //reversing a long position
                {
                    //partially attribute pnl to long/short proportionately
                    decimal portion = Quantity / Math.Abs(order.Quantity);
                    _unrecognizedPnLLong += portion * order.CommissionInBase;
                    RealizedPnLLong += portion * order.CommissionInBase;

                    _unrecognizedPnLShort += (1 - portion) * order.CommissionInBase;
                    RealizedPnLShort += (1 - portion) * order.CommissionInBase;
                }
                else //covering a long
                {
                    _unrecognizedPnLLong += order.CommissionInBase;
                    RealizedPnLLong += order.CommissionInBase;
                }
            }
        }

        /// <summary>
        /// Sets the value on the order's PerTradeFIFOPnL property.
        /// This is a FIFO profit/loss value for one trade only, not the account as a whole.
        /// </summary>
        /// <param name="order"></param>
        private void SetFIFOPnL(Order order)
        {
            //there's no fifo pnl if we are adding or opening a new position
            if (Quantity == 0 || Math.Sign(order.Quantity) == Math.Sign(Quantity))
            {
                _openPositions.Add(new Tuple<decimal, int>(order.Price * order.FXRateToBase, order.Quantity));
                return;
            }

            //loop through earlier orders and match them in a first in first out manner, calculating p/l
            decimal totalPnL = 0;
            int quantity = Math.Abs(order.Quantity);
            while (quantity > 0 && _openPositions.Count > 0)
            {
                Tuple<decimal, int> topPosition = _openPositions[0];
                if (Math.Abs(topPosition.Item2) > quantity)
                {
                    //this position is larger than the one exiting right now
                    _openPositions[0] = new Tuple<decimal, int>(topPosition.Item1, topPosition.Item2 + order.Quantity);
                    totalPnL += order.Quantity * (topPosition.Item1 - order.Price * order.FXRateToBase);
                    quantity = 0;
                }
                else
                {
                    _openPositions.RemoveAt(0);
                    totalPnL += -topPosition.Item2 * (topPosition.Item1 - order.Price * order.FXRateToBase);
                    quantity -= topPosition.Item2;
                }
            }

            //if there's anything left it means we reversed the position
            //so we add the remainder to the _openPositions
            if (quantity > 0)
            {
                _openPositions.Add(new Tuple<decimal, int>(order.Price * order.FXRateToBase, quantity * Math.Sign(order.Quantity)));
            }

            order.PerTradeFIFOPnL = totalPnL;
        }

        /// <summary>
        /// Update the capital usage given a new order
        /// </summary>
        private decimal GetCapitalUsage(Order order)
        {
            decimal capitalUsage = 0;
            if (Math.Sign(order.Quantity) == Math.Sign(Quantity) || Math.Abs(order.Quantity) > Math.Abs(Quantity))
            {
                //if reversing
                if ((order.Quantity < 0 && Quantity > 0) ||
                    (order.Quantity > 0 && Quantity < 0))
                {
                    capitalUsage = GetCapitalUsage(Math.Abs(Math.Abs(order.Quantity) - Math.Abs(Quantity)), order.Price, order.FXRateToBase);

                    if (order.Quantity > 0)
                        Capital.AddLong(Math.Abs(Math.Abs(order.Quantity) - Math.Abs(Quantity)) * order.Price * order.FXRateToBase * Instrument.Multiplier * GetCapitalUsageMultiplier(Instrument.AssetCategory));
                    else
                        Capital.AddShort(Math.Abs(Math.Abs(order.Quantity) - Math.Abs(Quantity)) * order.Price * order.FXRateToBase * Instrument.Multiplier * GetCapitalUsageMultiplier(Instrument.AssetCategory));
                }
                else //if adding
                {
                    capitalUsage = GetCapitalUsage(order.Quantity, order.Price, order.FXRateToBase);

                    if (order.Quantity > 0)
                        Capital.AddLong(Math.Abs(order.Quantity * order.Price) * order.FXRateToBase * Instrument.Multiplier * GetCapitalUsageMultiplier(Instrument.AssetCategory));
                    else
                        Capital.AddShort(Math.Abs(order.Quantity * order.Price) * order.FXRateToBase * Instrument.Multiplier * GetCapitalUsageMultiplier(Instrument.AssetCategory));
                }
            }

            return capitalUsage;
        }

        /// <summary>
        /// Updates CostBasis and PriorPeriodCostBasis given a new order to add to the position.
        /// </summary>
        private void UpdateCostBasis(Order order)
        {
            if ((order.Quantity > 0 && Quantity > 0) || //adding
                (order.Quantity < 0 && Quantity < 0))
            {
                CostBasis = (CostBasis * Quantity + order.Price * order.Quantity) / (order.Quantity + Quantity);
                PriorPeriodCostBasis = (PriorPeriodCostBasis * Quantity + order.Price * order.Quantity) / (order.Quantity + Quantity);

                FXRateBasis = (FXRateBasis * Quantity + order.FXRateToBase * order.Quantity) / (order.Quantity + Quantity);
                PriorPeriodFXRateBasis = (PriorPeriodFXRateBasis * Quantity + order.FXRateToBase * order.Quantity) / (order.Quantity + Quantity);
            }
            else if (Quantity == 0) //new
            {
                CostBasis = order.Price;
                PriorPeriodCostBasis = order.Price;

                FXRateBasis = order.FXRateToBase;
                PriorPeriodFXRateBasis = order.FXRateToBase;
            }
            else if (Math.Sign(order.Quantity) != Math.Sign(Quantity) && //reversing position
                Math.Abs(order.Quantity) > Math.Abs(Quantity))
            {
                CostBasis = order.Price;
                PriorPeriodCostBasis = order.Price;

                FXRateBasis = order.FXRateToBase;
                PriorPeriodFXRateBasis = order.FXRateToBase;
            }
        }
    }
}