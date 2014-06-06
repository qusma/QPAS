// -----------------------------------------------------------------------
// <copyright file="CurrencyPositionTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using NUnit.Framework;
using QPAS;

namespace QPASTest
{
    [TestFixture]
    public class CurrencyPositionTest
    {
        private Currency _currency;

        [SetUp]
        public void SetUp()
        {
            _currency = new Currency { ID = 2, Name = "EUR" };
        }

        [Test]
        public void QuantityAndPriceReflectAddedTransactions()
        {
            var pos = new CurrencyPosition(_currency);

            decimal fxRate = 1.35m;
            int quantity = 1000;

            var transaction1 = new FXTransaction
            {
                Quantity = quantity,
                Proceeds = fxRate * quantity,
                Cost = -fxRate * quantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction1);

            Assert.AreEqual(quantity, pos.Quantity);
            Assert.AreEqual(fxRate, pos.CostBasis);
            Assert.AreEqual(fxRate, pos.PriorPeriodCostBasis);
        }

        [Test]
        public void ProfitCorrectlyCalculatedAfterSimpleEntryAndExit()
        {
            var pos = new CurrencyPosition(_currency);

            decimal fxRate = 1.35m;
            int quantity = 1000;

            var transaction1 = new FXTransaction
            {
                Quantity = quantity,
                Proceeds = fxRate * quantity,
                Cost = -fxRate * quantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction1);

            decimal newFxRate = 1.36m;
            quantity = -quantity;
            var transaction2 = new FXTransaction
            {
                Quantity = quantity,
                Proceeds = newFxRate * quantity,
                Cost = -newFxRate * quantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction2);

            Assert.AreEqual(-quantity * (newFxRate - fxRate), pos.RealizedPnL);

            pos.Update(newFxRate);

            Assert.AreEqual(-quantity * (newFxRate - fxRate), pos.TotalPnL);
        }

        [Test]
        public void ProfitCorrectlyCalculatedAfterReversing()
        {
            var pos = new CurrencyPosition(_currency);

            decimal fxRate = 1.35m;
            int quantity = 1000;

            var transaction1 = new FXTransaction
            {
                Quantity = quantity,
                Proceeds = fxRate * quantity,
                Cost = -fxRate * quantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction1);

            decimal newFxRate = 1.36m;
            int newQuantity = -2000;
            var transaction2 = new FXTransaction
            {
                Quantity = newQuantity,
                Proceeds = newFxRate * newQuantity,
                Cost = -newFxRate * newQuantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction2);

            Assert.AreEqual(quantity * (newFxRate - fxRate), pos.RealizedPnL);

            pos.Update(newFxRate);

            Assert.AreEqual(quantity * (newFxRate - fxRate), pos.TotalPnL);
        }

        [Test]
        public void ProfitCorrectlyCalculatedAfterPartialExit()
        {
            var pos = new CurrencyPosition(_currency);

            decimal fxRate = 1.35m;
            int quantity = 1000;

            var transaction1 = new FXTransaction
            {
                Quantity = quantity,
                Proceeds = fxRate * quantity,
                Cost = -fxRate * quantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction1);

            decimal newFxRate = 1.36m;
            int newQuantity = -500;
            var transaction2 = new FXTransaction
            {
                Quantity = newQuantity,
                Proceeds = newFxRate * newQuantity,
                Cost = -newFxRate * newQuantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction2);

            Assert.AreEqual(-newQuantity * (newFxRate - fxRate), pos.RealizedPnL);

            pos.Update(fxRate);

            Assert.AreEqual(-newQuantity * (newFxRate - fxRate), pos.TotalPnL);
        }

        [Test]
        public void UnrealizedProfitCorrectlyCalcuatedAfterUpdate()
        {
            var pos = new CurrencyPosition(_currency);

            decimal fxRate = 1.35m;
            int quantity = 1000;

            var transaction1 = new FXTransaction
            {
                Quantity = quantity,
                Proceeds = fxRate * quantity,
                Cost = -fxRate * quantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction1);

            decimal newFxRate = 1.36m;
            int newQuantity = -500;
            var transaction2 = new FXTransaction
            {
                Quantity = newQuantity,
                Proceeds = newFxRate * newQuantity,
                Cost = -newFxRate * newQuantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction2);

            Assert.AreEqual(-newQuantity * (newFxRate - fxRate), pos.RealizedPnL);

            pos.Update(newFxRate);

            Assert.AreEqual(quantity * (newFxRate - fxRate), pos.TotalPnL);
        }

        [Test]
        public void RealizedProfitsWithUpdateBetweenTradesCalculatedCorrectly()
        {
            var pos = new CurrencyPosition(_currency);

            decimal fxRate = 1.35m;
            int quantity = 1000;

            var transaction1 = new FXTransaction
            {
                Quantity = quantity,
                Proceeds = fxRate * quantity,
                Cost = -fxRate * quantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction1);

            decimal newFxRate = 1.36m;

            pos.Update(newFxRate);

            Assert.AreEqual(quantity * (newFxRate - fxRate), pos.TotalPnL);


            int newQuantity = -1000;
            decimal newFxRate2 = 1.37m;

            var transaction2 = new FXTransaction
            {
                Quantity = newQuantity,
                Proceeds = newFxRate2 * newQuantity,
                Cost = -newFxRate2 * newQuantity,
                FXCurrency = _currency
            };
            pos.AddFXTransaction(transaction2);

            Assert.AreEqual(-newQuantity * (newFxRate2 - fxRate), pos.RealizedPnL);
        }
    }
}
