// -----------------------------------------------------------------------
// <copyright file="PositionTest.cs" company="">
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
    public class PositionTest
    {
        private Instrument _instrument;

        [SetUp]
        public void SetUp()
        {
            _instrument = new Instrument { ID = 1, Multiplier = 1, AssetCategory = AssetClass.Stock };
        }

        [Test]
        public void CorrectCapitalUsageReturnedWhenAddingOrderToEmptyPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            var res = pos.AddOrder(o);
            Assert.AreEqual(1200, res);
        }

        [Test]
        public void CorrectCapitalUsageReturnedWhenAddingToExistingPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = 100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            var res = pos.AddOrder(o2);

            Assert.AreEqual(1100, res);
        }

        [Test]
        public void CorrectCapitalUsageReturnedWhenRemovingFromExistingPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            var res = pos.AddOrder(o2);

            Assert.AreEqual(0, res);
        }

        [Test]
        public void CorrectCapitalUsageReturnedWhenReversingExistingPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -150,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            var res = pos.AddOrder(o2);

            Assert.AreEqual(550, res);
        }

        [Test]
        public void CorrectPriceBasisUpdateWhenAddingOrderToEmptyPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);
            Assert.AreEqual(12, pos.CostBasis);
        }

        [Test]
        public void CorrectPriceBasisUpdateWhenAddingToExistingPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = 100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(11.5, pos.CostBasis);
        }

        [Test]
        public void CorrectPriceBasisUpdateWhenRemovingFromExistingPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(12, pos.CostBasis);
        }

        [Test]
        public void CorrectPriceBasisUpdateWhenReversingExistingPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -150,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(11, pos.CostBasis);
        }

        [Test]
        public void ROACValueIsCorrect()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            pos.GetPnL(0, 0);

            Assert.AreEqual(1.1, pos.ROAC);
        }

        [Test]
        public void ROACValueIsCorrectAfterReversing()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -150,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            pos.GetPnL(11, 1);

            Assert.AreEqual(Math.Round(1 + (1.0 * 100) / (10 * 100 + 11 * 50), 5), Math.Round(pos.ROAC, 5));
        }

        [Test]
        public void ROACValueIsCorrectAfterPartialExit()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            pos.GetPnL(10, 1);

            Assert.AreEqual(1.05, pos.ROAC);
        }

        [Test]
        public void ROACValueIsCorrectWithCashTransactions()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var c = new CashTransaction
            {
                Amount = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddCashTransaction(c);

            pos.GetPnL(10, 1);

            Assert.AreEqual(1.01, pos.ROAC);
        }

        [Test]
        public void ROACValueReflectsFXRateChanges()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            pos.GetPnL(10, 1.1m);

            Assert.AreEqual(1.1, pos.ROAC);
        }

        [Test]
        public void RealizedPnLValuesReflectFXRateChanges()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1.05m
            };
            pos.AddOrder(o2);

            Assert.AreEqual(155, pos.RealizedPnL);
        }

        [Test]
        public void RealizedPnLValuesIncludeCommissions()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                Commission = -5
            };
            pos.AddOrder(o);

            Assert.AreEqual(-5, pos.RealizedPnL);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1,
                Commission = -5
            };
            pos.AddOrder(o2);

            Assert.AreEqual(90, pos.RealizedPnL);
        }

        [Test]
        public void GetPnLReturnsCorrectPnLForLongPositions()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            Assert.AreEqual(100, pos.GetPnL(11, 1));
            Assert.AreEqual(-100, pos.GetPnL(10, 1));
        }

        [Test]
        public void GetPnLReturnsCorrectPnLForShortPositions()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            Assert.AreEqual(-100, pos.GetPnL(11, 1));
            Assert.AreEqual(100, pos.GetPnL(10, 1));
        }

        [Test]
        public void GetPnLIncludesCommissions()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                Commission = -5
            };
            pos.AddOrder(o);

            Assert.AreEqual(100 - 5, pos.GetPnL(11, 1));
            Assert.AreEqual(-100, pos.GetPnL(10, 1));
        }

        [Test]
        public void GetPnLReturnsCorrectPnLWithBothRealizedAndUnrealizedPnL()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1,
            };
            pos.AddOrder(o2);

            Assert.AreEqual(50 * 1 + 50 * 2, pos.GetPnL(12, 1));
            Assert.AreEqual(-2 * 50, pos.GetPnL(10, 1));
        }

        [Test]
        public void GetPnLReturnsCorrectPnLIncludingFXRateChanges()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            Assert.AreEqual(45, pos.GetPnL(11, 0.95m));
        }

        [Test]
        public void CumulativeCapitalUsedHasCorrectValueWhenAdding()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = 100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(12 * 100 + 11 * 100, pos.Capital.TodaysCapitalGross);
            Assert.AreEqual(12 * 100 + 11 * 100, pos.Capital.TodaysCapitalNet);
            Assert.AreEqual(12 * 100 + 11 * 100, pos.Capital.TodaysCapitalLong);
        }

        [Test]
        public void CumulativeCapitalUsedHasCorrectValueWhenClosing()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(12 * 100, pos.Capital.TodaysCapitalGross);
            Assert.AreEqual(12 * 100, pos.Capital.TodaysCapitalNet);
            Assert.AreEqual(12 * 100, pos.Capital.TodaysCapitalLong);
        }

        [Test]
        public void CumulativeCapitalUsedHasCorrectValueWhenReversing()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -200,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(12 * 100 + 11 * 100, pos.Capital.TodaysCapitalGross);
            Assert.AreEqual(12 * 100 - 11 * 100, pos.Capital.TodaysCapitalNet);
            Assert.AreEqual(12 * 100, pos.Capital.TodaysCapitalLong);
            Assert.AreEqual(11 * 100, pos.Capital.TodaysCapitalShort);
        }

        [Test]
        public void AddOrderReturnsCorrectCurrentCapitalUsageWhenOpeningAPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 12,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            decimal capUsage = pos.AddOrder(o);

            Assert.AreEqual(1200, capUsage);
        }

        [Test]
        public void AddOrderReturnsCorrectCurrentCapitalUsageWhenAddingToAPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 100,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);


            var o2 = new Order
            {
                Quantity = 50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            var capUsage = pos.AddOrder(o2);

            Assert.AreEqual(550, capUsage);
        }

        [Test]
        public void AddOrderReturnsCorrectCurrentCapitalUsageWhenReversingPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 100,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);


            var o2 = new Order
            {
                Quantity = -200,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            var capUsage = pos.AddOrder(o2);

            Assert.AreEqual(1100, capUsage);
        }

        [Test]
        public void AddOrderReturnsCorrectCurrentCapitalUsageWhenRemovingFromPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 100,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);


            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            var capUsage = pos.AddOrder(o2);

            Assert.AreEqual(0, capUsage);
        }

        [Test]
        public void AddOrderTakesIntoAccountFXRate()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 100,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);


            var o2 = new Order
            {
                Quantity = -200,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1.1m
            };
            var capUsage = pos.AddOrder(o2);

            Assert.AreEqual(1100, capUsage);
        }

        [Test]
        public void FifoPnLIsCorrectAfterSimpleEntryAndExit()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(0, o.PerTradeFIFOPnL);
            Assert.AreEqual(100, o2.PerTradeFIFOPnL);
        }

        [Test]
        public void FifoPnLIsCorrectAfterPartialExit()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(0, o.PerTradeFIFOPnL);
            Assert.AreEqual(50, o2.PerTradeFIFOPnL);
        }

        [Test]
        public void FifoPnLIsCorrectAfterReversal()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -200,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(0, o.PerTradeFIFOPnL);
            Assert.AreEqual(100, o2.PerTradeFIFOPnL);
        }


        [Test]
        public void FifoPnLIsCorrectAfterAddingToPosition()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = 100,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            Assert.AreEqual(0, o.PerTradeFIFOPnL);
            Assert.AreEqual(0, o2.PerTradeFIFOPnL);
        }

        [Test]
        public void FifoPnLTakesIntoAccountFXRate()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1.1m
            };
            pos.AddOrder(o2);

            Assert.AreEqual(0, o.PerTradeFIFOPnL);
            Assert.AreEqual(100, o2.PerTradeFIFOPnL);
        }

        [Test]
        public void UnrealizedPnLReturnsDiffernceBetweenTotalAndRealizedPnL()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            pos.GetPnL(11, 1);

            Assert.AreEqual(50, pos.UnrealizedPnL);
        }

        [Test]
        [ExpectedException]
        public void AddingAnOrderForTheWrongInstrumentThrowsException()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -50,
                Price = 11,
                Instrument = new Instrument { ID = 2, Multiplier = 1, AssetCategory = AssetClass.Stock },
                FXRateToBase = 1
            };
            pos.AddOrder(o2);
        }

        [Test]
        public void PnLIsDeferredForROACCalculationsIfNoCapitalIsUsed()
        {
            var pos = new Position(_instrument);
            var c = new CashTransaction
            {
                Amount = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddCashTransaction(c);

            pos.GetPnL(10, 1);

            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            pos.GetPnL(10, 1);

            Assert.AreEqual(1 + (10d / (10 * 100)), pos.ROAC);
        }

        [Test]
        public void MultiPeriodCapitalUsageIsRecordedCorrectly()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o);

            Assert.AreEqual(1000, pos.Capital.TodaysCapitalGross);
            Assert.AreEqual(1000, pos.Capital.TodaysCapitalLong);
            pos.GetPnL(10, 1);

            var o2 = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o2);

            var o3 = new Order
            {
                Quantity = -200,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o3);

            pos.GetPnL(10, 1);

            Assert.AreEqual(2000, pos.Capital.Gross.Last());
            Assert.AreEqual(2000, pos.Capital.Long.Last());

            pos.GetPnL(10, 1);

            var o4 = new Order
            {
                Quantity = 150,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1
            };
            pos.AddOrder(o4);

            pos.GetPnL(10, 1);


            List<decimal> expectedCapitalGross = new List<decimal> { 1000, 2000, 0, 1500 };
            for (int i = 0; i < expectedCapitalGross.Count; i++)
            {
                Assert.AreEqual(expectedCapitalGross[i], pos.Capital.Gross[i]);
            }
        }

        [Test]
        public void CapitalUsageWhenEnteringNearCloseIsZero()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000,1,1,15,59,0)
            };
            pos.AddOrder(o);
            pos.GetPnL(10, 1);

            Assert.AreEqual(0, pos.Capital.Gross.Last());
            Assert.AreEqual(0, pos.Capital.Long.Last());
        }

        [Test]
        public void CapitalUsageWhenEnteringAndExitingNearCloseIsCounted()
        {
            var pos = new Position(_instrument);
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 0)
            };
            pos.AddOrder(o);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 1)
            };
            pos.AddOrder(o2);

            pos.GetPnL(10, 1);

            Assert.AreEqual(1000, pos.Capital.Gross.Last());
            Assert.AreEqual(1000, pos.Capital.Long.Last());
        }

        [Test]
        public void CapitalUsageWhenEnteringAndReversingNearCloseIsCounted()
        {
            var pos = new Position(_instrument);
            decimal orderCapitalUsage = 0;
            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 0)
            };
            orderCapitalUsage = pos.AddOrder(o);
            Assert.AreEqual(0, orderCapitalUsage);

            var o2 = new Order
            {
                Quantity = -200,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 1)
            };
            orderCapitalUsage = pos.AddOrder(o2);
            Assert.AreEqual(1000, orderCapitalUsage);

            var o3 = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 1)
            };
            orderCapitalUsage = pos.AddOrder(o3);
            Assert.AreEqual(1000, orderCapitalUsage);

            pos.GetPnL(10, 1);

            Assert.AreEqual(2000, pos.Capital.Gross.Last());
            Assert.AreEqual(1000, pos.Capital.Long.Last());
            Assert.AreEqual(1000, pos.Capital.Short.Last());
        }

        [Test]
        public void CapitalUsageWhenEnteringAndReversingShortNearCloseIsCounted()
        {
            var pos = new Position(_instrument);
            decimal orderCapitalUsage = 0;
            var o = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 0)
            };
            orderCapitalUsage = pos.AddOrder(o);
            Assert.AreEqual(0, orderCapitalUsage);

            var o2 = new Order
            {
                Quantity = 200,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 1)
            };
            orderCapitalUsage = pos.AddOrder(o2);
            Assert.AreEqual(1000, orderCapitalUsage);

            var o3 = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 1)
            };
            orderCapitalUsage = pos.AddOrder(o3);
            Assert.AreEqual(1000, orderCapitalUsage);

            pos.GetPnL(10, 1);

            Assert.AreEqual(2000, pos.Capital.Gross.Last());
            Assert.AreEqual(1000, pos.Capital.Long.Last());
            Assert.AreEqual(1000, pos.Capital.Short.Last());
        }

        [Test]
        public void CapitalUsageWhenEnteringAndExitingMultipleOrdersNearCloseIsCounted()
        {
            var pos = new Position(_instrument);
            decimal orderCapitalUsage = 0;

            var o = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 0)
            };
            orderCapitalUsage = pos.AddOrder(o);
            Assert.AreEqual(0, orderCapitalUsage);

            var o2 = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 1)
            };
            orderCapitalUsage = pos.AddOrder(o2);
            Assert.AreEqual(0, orderCapitalUsage);

            var o3 = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 2)
            };
            orderCapitalUsage = pos.AddOrder(o3);
            Assert.AreEqual(1000, orderCapitalUsage);

            var o4 = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 3)
            };
            orderCapitalUsage = pos.AddOrder(o4);
            Assert.AreEqual(1000, orderCapitalUsage);

            pos.GetPnL(10, 1);

            Assert.AreEqual(2000, pos.Capital.Gross.Last());
            Assert.AreEqual(2000, pos.Capital.Long.Last());
        }

        [Test]
        public void CapitalUsageWhenEnteringAndExitingMultipleOrdersNearCloseIsCountedWithShortPosition()
        {
            var pos = new Position(_instrument);
            decimal orderCapitalUsage = 0;

            var o = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 0)
            };
            orderCapitalUsage = pos.AddOrder(o);
            Assert.AreEqual(0, orderCapitalUsage);

            var o2 = new Order
            {
                Quantity = -100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 1)
            };
            orderCapitalUsage = pos.AddOrder(o2);
            Assert.AreEqual(0, orderCapitalUsage);

            var o3 = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 2)
            };
            orderCapitalUsage = pos.AddOrder(o3);
            Assert.AreEqual(1000, orderCapitalUsage);

            var o4 = new Order
            {
                Quantity = 100,
                Price = 10,
                Instrument = _instrument,
                FXRateToBase = 1,
                TradeDate = new DateTime(2000, 1, 1, 15, 59, 3)
            };
            orderCapitalUsage = pos.AddOrder(o4);
            Assert.AreEqual(1000, orderCapitalUsage);

            pos.GetPnL(10, 1);

            Assert.AreEqual(2000, pos.Capital.Gross.Last());
            Assert.AreEqual(2000, pos.Capital.Short.Last());
        }
    }
}