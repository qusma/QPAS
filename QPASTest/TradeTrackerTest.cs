// -----------------------------------------------------------------------
// <copyright file="TradeTrackerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using NUnit.Framework;
using QPAS;
using System;
using System.Collections.Generic;

namespace QPASTest
{
    [TestFixture]
    public class TradeTrackerTest
    {
        private Instrument _inst;

        [SetUp]
        public void SetUp()
        {
            _inst = new Instrument { ID = 1, Multiplier = 1, AssetCategory = AssetClass.Stock };
        }

        [Test]
        public void TotalPnlLongIsTheSumOfRealizedAndUnrealizedPnlForLongPositions()
        {
            var trade = new Trade();
            var date = new DateTime(2000, 1, 1);
            trade.Orders = new List<Order>
            {
                new Order { Instrument = _inst, Quantity = 10, FXRateToBase = 1, Price = 100, BuySell = "BUY", TradeDate = date },
                new Order { Instrument = _inst, Quantity = -5, FXRateToBase = 1, Price = 105, BuySell = "SELL", TradeDate = date }
            };

            var tracker = new TradeTracker(trade, 1);
            foreach (Order o in trade.Orders)
            {
                tracker.AddOrder(o);
            }

            var data = new Dictionary<int, TimeSeries> {
                { 1, TimeSeriesGenerator.GenerateData(date, date, 110) }
            };

            foreach (TimeSeries ts in data.Values)
            {
                ts.ProgressTo(date);
            }

            tracker.Update(date, data, null);

            Assert.AreEqual(5 * 5 + 10 * 5, tracker.TotalPnL);
            Assert.AreEqual(5 * 5 + 10 * 5, tracker.TotalPnlLong);
        }

        [Test]
        public void TotalPnlLongIsTheSumOfRealizedAndUnrealizedPnlForShortPositions()
        {
            var trade = new Trade();
            var date = new DateTime(2000, 1, 1);
            trade.Orders = new List<Order>
            {
                new Order { Instrument = _inst, Quantity = -10, FXRateToBase = 1, Price = 100, BuySell = "BUY", TradeDate = date },
                new Order { Instrument = _inst, Quantity = 5, FXRateToBase = 1, Price = 95, BuySell = "SELL", TradeDate = date }
            };

            var tracker = new TradeTracker(trade, 1);
            foreach (Order o in trade.Orders)
            {
                tracker.AddOrder(o);
            }

            var data = new Dictionary<int, TimeSeries> {
                { 1, TimeSeriesGenerator.GenerateData(date, date, 90) }
            };

            foreach (TimeSeries ts in data.Values)
            {
                ts.ProgressTo(date);
            }

            tracker.Update(date, data, null);

            Assert.AreEqual(5 * 5 + 10 * 5, tracker.TotalPnL);
            Assert.AreEqual(5 * 5 + 10 * 5, tracker.TotalPnlShort);
        }

        [Test]
        public void TotalPnlLongIsTheSumOfRealizedAndUnrealizedPnlForCurrencyPositions()
        {
            var trade = new Trade();
            var date = new DateTime(2000, 1, 1);
            var fxCurrency = new Currency { ID = 2, Name = "CAD" };
            trade.FXTransactions = new List<FXTransaction>
            {
                new FXTransaction { FXCurrency = fxCurrency, Quantity = 1000, Proceeds = 1500, Cost = -1500 },
                new FXTransaction { FXCurrency = fxCurrency, Quantity = -500, Proceeds = -850, Cost = 850 },
            };

            var tracker = new TradeTracker(trade, 1);
            foreach (FXTransaction fxt in trade.FXTransactions)
            {
                tracker.AddFXTransaction(fxt);
            }

            var data = new Dictionary<int, TimeSeries> {
                { 2, TimeSeriesGenerator.GenerateData(date, date, 1.55m) }
            };

            foreach (TimeSeries ts in data.Values)
            {
                ts.ProgressTo(date);
            }

            tracker.Update(date, new Dictionary<int, TimeSeries>(), data);

            Assert.AreEqual(100 + 500 * (1.55m - 1.5m), tracker.TotalPnL);
        }

        [Test]
        public void AddingCashTransactionInForeignCurrencyResultsInFxPositionAddition()
        {
            var trade = new Trade();
            var date = new DateTime(2000, 1, 1);
            var eur = new Currency { ID = 2, Name = "EUR" };
            var ct = new CashTransaction { InstrumentID = 1, Currency = eur, CurrencyID = 2, Amount = 50, FXRateToBase = 1.5m, Instrument = _inst, TransactionDate = date, AssetCategory = AssetClass.Stock };
            trade.CashTransactions = new List<CashTransaction> { ct };
            var tracker = new TradeTracker(trade, 1);
            foreach (CashTransaction c in trade.CashTransactions)
            {
                tracker.AddCashTransaction(c);
            }

            Assert.IsTrue(tracker.CurrencyPositions.ContainsKey(2));
            Assert.IsTrue(tracker.CurrencyPositions[2].Quantity == 50);
            Assert.IsTrue(tracker.CurrencyPositions[2].CostBasis == 1.5m);
        }

        [Test]
        public void AddingOrderInForeignCurrencyResultsInFxPositionAddition()
        {
            var trade = new Trade();
            var date = new DateTime(2000, 1, 1);
            var eur = new Currency { ID = 2, Name = "EUR" };
            trade.Orders = new List<Order>
            {
                new Order { Instrument = _inst, Quantity = 10, FXRateToBase = 1.5m, Multiplier = 1, Price = 100, BuySell = "BUY", TradeDate = date, Currency = eur, CurrencyID = 2 },
            };

            var tracker = new TradeTracker(trade, 1);
            foreach (Order o in trade.Orders)
            {
                tracker.AddOrder(o);
            }

            Assert.IsTrue(tracker.CurrencyPositions.ContainsKey(2));
            Assert.IsTrue(tracker.CurrencyPositions[2].Quantity == -10 * 100);
            Assert.IsTrue(tracker.CurrencyPositions[2].CostBasis == 1.5m);
        }

        [Test]
        public void TodaysPnlResetsEvenWhenTrackerIsNotOpen()
        {
            var trade = new Trade();
            var date = new DateTime(2000, 1, 1);
            trade.Orders = new List<Order>
            {
                new Order { Instrument = _inst, Quantity = -10, FXRateToBase = 1, Price = 100, BuySell = "BUY", TradeDate = date },
                new Order { Instrument = _inst, Quantity = 5, FXRateToBase = 1, Price = 95, BuySell = "SELL", TradeDate = date }
            };

            var tracker = new TradeTracker(trade, 1);
            foreach (Order o in trade.Orders)
            {
                tracker.AddOrder(o);
            }

            var data = new Dictionary<int, TimeSeries> {
                { 1, TimeSeriesGenerator.GenerateData(date, date.AddDays(1), 90) }
            };

            foreach (TimeSeries ts in data.Values)
            {
                ts.ProgressTo(date);
            }

            tracker.Update(date, data, null);

            Assert.AreEqual(5 * 5 + 10 * 5, tracker.TodaysPnL);

            data[1].ProgressTo(date.AddDays(1));
            tracker.Update(date.AddDays(1), data, null);
            Assert.AreEqual(0, tracker.TodaysPnL);
        }
    }
}