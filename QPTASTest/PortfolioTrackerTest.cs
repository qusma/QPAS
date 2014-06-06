// -----------------------------------------------------------------------
// <copyright file="PortfolioTrackerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EntityModel;
using NUnit.Framework;
using QPAS;

namespace QPASTest
{
    [TestFixture]
    public class PortfolioTrackerTest
    {
        private Dictionary<int, TimeSeries> _data;
        private Dictionary<int, TimeSeries> _fxData;
        private Instrument _inst;

        [SetUp]
        public void SetUp()
        {
            _data = new Dictionary<int, TimeSeries>();
            _fxData = new Dictionary<int, TimeSeries>();
            _inst = new Instrument { ID = 1, Multiplier = 1, AssetCategory = AssetClass.Stock };
        }

        [Test]
        public void CapitalUsageCorrectlyTracked()
        {
            var inst2 = new Instrument { ID = 2, Multiplier = 1, AssetCategory = AssetClass.Stock };

            var order1 = new Order
            {
                Instrument = _inst,
                InstrumentID = _inst.ID,
                Multiplier = 1,
                FXRateToBase = 1,
                Price = 10,
                Quantity = 100,
                CurrencyID = 1,
                BuySell = "BUY",
                IsReal = true,
                TradeDate = new DateTime(2000, 1, 2)
            };

            var order2 = new Order
            {
                Instrument = inst2,
                InstrumentID = inst2.ID,
                Multiplier = 1,
                FXRateToBase = 1,
                Price = 20,
                Quantity = -100,
                CurrencyID = 1,
                BuySell = "SELL",
                IsReal = true,
                TradeDate = new DateTime(2000, 1, 3, 12, 0, 0)
            };

            var order3 = new Order
            {
                Instrument = inst2,
                InstrumentID = inst2.ID,
                Multiplier = 1,
                FXRateToBase = 1,
                Price = 19,
                Quantity = 100,
                CurrencyID = 1,
                BuySell = "BUY",
                IsReal = true,
                TradeDate = new DateTime(2000, 1, 3, 13, 0, 0)
            };

            var trade = new Trade
            {
                Orders = new List<Order> { order1, order2, order3 }
            };

            var trades = new List<Trade> { trade };

            _data = new Dictionary<int, TimeSeries>
            {
                { 1, TimeSeriesGenerator.GenerateData(new DateTime(2000,1,1), new DateTime(2000,2,1), 11) },
                { 2, TimeSeriesGenerator.GenerateData(new DateTime(2000,1,1), new DateTime(2000,2,1), 20) }
            };

            var tracker = new PortfolioTracker(_data, _fxData, trades, "test");

            var date = new DateTime(2000, 1, 1);
            foreach (TimeSeries ts in _data.Values)
            {
                ts.ProgressTo(date);
            }
            
            tracker.ProcessItemsAt(date);
            tracker.OnDayClose(date, 10000);

            Assert.AreEqual(0, tracker.Capital.Gross.Last());

            date = date.AddDays(1);
            foreach (TimeSeries ts in _data.Values)
            {
                ts.ProgressTo(date);
            }
            tracker.ProcessItemsAt(date);
            tracker.OnDayClose(date, 10000);

            Assert.AreEqual(10 * 100, tracker.Capital.Gross.Last());
            Assert.AreEqual(10 * 100, tracker.Capital.Long.Last());

            date = date.AddDays(1);
            foreach (TimeSeries ts in _data.Values)
            {
                ts.ProgressTo(date);
            }
            tracker.ProcessItemsAt(date);
            tracker.OnDayClose(date, 10000);

            Assert.AreEqual(11 * 100 + 20 * 100, tracker.Capital.Gross.Last());
            Assert.AreEqual(11 * 100, tracker.Capital.Long.Last());
            Assert.AreEqual(20 * 100, tracker.Capital.Short.Last());
        }

        [Test]
        public void PnLCorrectlyReflectedInEquityCurves()
        {
            var inst2 = new Instrument { ID = 2, Multiplier = 1, AssetCategory = AssetClass.Stock };

            var order1 = new Order
            {
                Instrument = _inst,
                InstrumentID = _inst.ID,
                Multiplier = 1,
                FXRateToBase = 1,
                Price = 10,
                Quantity = 100,
                CurrencyID = 1,
                BuySell = "BUY",
                IsReal = true,
                TradeDate = new DateTime(2000, 1, 2),
                TradeID = 1
            };

            var order2 = new Order
            {
                Instrument = inst2,
                InstrumentID = inst2.ID,
                Multiplier = 1,
                FXRateToBase = 1,
                Price = 20,
                Quantity = -100,
                CurrencyID = 1,
                BuySell = "SELL",
                IsReal = true,
                TradeDate = new DateTime(2000, 1, 3, 12, 0, 0),
                TradeID = 1
            };

            var order3 = new Order
            {
                Instrument = inst2,
                InstrumentID = inst2.ID,
                Multiplier = 1,
                FXRateToBase = 1,
                Price = 19,
                Quantity = 100,
                CurrencyID = 1,
                BuySell = "BUY",
                IsReal = true,
                TradeDate = new DateTime(2000, 1, 3, 13, 0, 0),
                TradeID = 1
            };

            var trade = new Trade
            {
                Orders = new List<Order> { order1, order2, order3 },
                ID = 1
            };

            var trades = new List<Trade> { trade };

            _data = new Dictionary<int, TimeSeries>
            {
                { 1, TimeSeriesGenerator.GenerateData(new DateTime(2000,1,1), new DateTime(2000,2,1), 11) },
                { 2, TimeSeriesGenerator.GenerateData(new DateTime(2000,1,1), new DateTime(2000,2,1), 20) }
            };

            var tracker = new PortfolioTracker(_data, _fxData, trades, "test");

            var date = new DateTime(2000, 1, 1);
            foreach (TimeSeries ts in _data.Values)
            {
                ts.ProgressTo(date);
            }

            tracker.ProcessItemsAt(date);
            tracker.OnDayClose(date, 10000);

            Assert.AreEqual(0, tracker.ProfitLossEquityCurve.Equity.Last());

            date = date.AddDays(1);
            foreach (TimeSeries ts in _data.Values)
            {
                ts.ProgressTo(date);
            }
            tracker.ProcessItemsAt(date);
            tracker.OnDayClose(date, 10000);

            Assert.AreEqual((11 - 10) * 100, tracker.ProfitLossEquityCurve.Equity.Last());
            Assert.AreEqual((11 - 10) * 100, tracker.ProfitLossLongEquityCurve.Equity.Last());

            date = date.AddDays(1);
            foreach (TimeSeries ts in _data.Values)
            {
                ts.ProgressTo(date);
            }
            tracker.ProcessItemsAt(date);
            tracker.OnDayClose(date, 10000);

            Assert.AreEqual((11 - 10) * 100 + (20 - 19) * 100, tracker.ProfitLossEquityCurve.Equity.Last());
            Assert.AreEqual((11 - 10) * 100, tracker.ProfitLossLongEquityCurve.Equity.Last());
            Assert.AreEqual((20 - 19) * 100, tracker.ProfitLossShortEquityCurve.Equity.Last());
        }
    }
}
