// -----------------------------------------------------------------------
// <copyright file="DataSourcerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Entity;
using EntityModel;
using Moq;
using NUnit.Framework;
using QDMS;
using QPAS;
using Instrument = EntityModel.Instrument;

namespace QPASTest
{
    [TestFixture]
    public class DataSourcerTest
    {
        private DataSourcer _datasourcer;
        private Mock<IDBContext> _contextMock;
        private Mock<IExternalDataSource> _externalSourceMock;

        [SetUp]
        public void SetUp()
        {
            _contextMock = new Mock<IDBContext>();
            _externalSourceMock = new Mock<IExternalDataSource>();

            _datasourcer = new DataSourcer(_contextMock.Object, _externalSourceMock.Object, true);
        }

        [Test]
        public void DataIsCachedBetweenRequests()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 1, 2);
            var inst = new Instrument { ID = 1, Symbol = "SPY", AssetCategory = AssetClass.Stock, QDMSInstrumentID = 2 };

            var data = new List<OHLCBar>
            {
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,1)},
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,2)},
            };

            var ppSetStub = new DbSetStub<PriorPosition>();

            _contextMock.Setup(x => x.PriorPositions).Returns(ppSetStub);

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .Returns(data);

            _datasourcer.GetData(inst, startDate, endDate);

            //request a second time: should use cache instead of external source
            _datasourcer.GetData(inst, startDate, endDate);

            _externalSourceMock.Verify(x => x.GetData(
                It.IsAny<Instrument>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<BarSize>()),
                Times.Once);
        }

        [Test]
        public void ExternalDataIsSupplementedWithLocalDataWhenObservationsAreMissing()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 1, 3);
            var inst = new Instrument { ID = 1, Symbol = "SPY", AssetCategory = AssetClass.Stock, QDMSInstrumentID = 2 };

            var data = new List<OHLCBar>
            {
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,1)},
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,2)},
            };

            var ppSetStub = new DbSetStub<PriorPosition>();

            _contextMock.Setup(x => x.PriorPositions).Returns(ppSetStub);

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .Returns(data);

            _datasourcer.GetData(inst, startDate, endDate);

            _contextMock.Verify(x => x.PriorPositions);
        }

        [Test]
        public void CashInstrumentLocalRequestsUseFxRates()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 1, 3);
            var inst = new Instrument { ID = 1, Symbol = "EUR.USD", AssetCategory = AssetClass.Cash, QDMSInstrumentID = 2 };

            var fxrSetStub = new DbSetStub<FXRate>();

            var currencySetStub = new DbSetStub<Currency>();
            currencySetStub.Add(new Currency { ID = 2, Name = "EUR" });

            _contextMock.Setup(x => x.Currencies).Returns(currencySetStub);
            _contextMock.Setup(x => x.FXRates).Returns(fxrSetStub);

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .Returns(new List<OHLCBar>());

            _datasourcer.GetData(inst, startDate, endDate);

            _contextMock.Verify(x => x.FXRates);
        }

        [Test]
        public void IfExternalSourceHasNoDataLocalBackupIsUsedInstead()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 2, 1);
            var inst = new Instrument { ID = 1, Symbol = "SPY", AssetCategory = AssetClass.Stock, QDMSInstrumentID = 2};

            var ppSetStub = new DbSetStub<PriorPosition>();

            _contextMock.Setup(x => x.PriorPositions).Returns(ppSetStub);

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .Returns(new List<OHLCBar>());
            
            _datasourcer.GetData(inst, startDate, endDate);

            _externalSourceMock.Verify(x => x.GetData(
                It.IsAny<Instrument>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<BarSize>()));

            _contextMock.Verify(x => x.PriorPositions);
        }

        [Test]
        public void ExternalDataRequestsAreForwardedToExternalDatasource()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 2, 1);

            _datasourcer.GetExternalData(1, startDate, endDate);
            _externalSourceMock.Verify(x => x.GetData(
                It.Is<int>(y => y == 1),
                It.Is<DateTime>(y => y == startDate),
                It.Is<DateTime>(y => y == endDate),
                It.Is<BarSize>(y => y == BarSize.OneDay)));
        }

        [Test]
        public void FxDataIsInvertedWhenNecessary()
        {
            var inst = new Instrument { Symbol = "USD.CAD", ID = 1, AssetCategory = AssetClass.Cash };
            var startDate = new DateTime(2000,1,1);
            var endDate = new DateTime(2000,1,1);

            var fxrSetStub = new DbSetStub<FXRate>();
            fxrSetStub.Add(new FXRate { FromCurrencyID = 2, ToCurrencyID = 1, Date = startDate, Rate = 1.1m });

            var currencySetStub = new DbSetStub<Currency>();
            currencySetStub.Add(new Currency { ID = 2, Name = "CAD" });

            _contextMock.Setup(x => x.Currencies).Returns(currencySetStub);
            _contextMock.Setup(x => x.FXRates).Returns(fxrSetStub);
            
            var data = _datasourcer.GetData(inst, startDate, endDate);

            Assert.AreEqual(1m / 1.1m, data[0].Close);
        }

        [Test]
        public void LastPriceFxDataIsInvertedWhenNecessary()
        {
            var inst = new Instrument { Symbol = "USD.CAD", ID = 1, AssetCategory = AssetClass.Cash };
            var startDate = new DateTime(2000, 1, 1);

            var fxrSetStub = new DbSetStub<FXRate>();
            fxrSetStub.Add(new FXRate { FromCurrencyID = 2, ToCurrencyID = 1, Date = startDate, Rate = 1.1m });

            var currencySetStub = new DbSetStub<Currency>();
            currencySetStub.Add(new Currency { ID = 2, Name = "CAD" });

            _contextMock.Setup(x => x.Currencies).Returns(currencySetStub);
            _contextMock.Setup(x => x.FXRates).Returns(fxrSetStub);

            decimal fxRate;
            var price = _datasourcer.GetLastPrice(inst, out fxRate);

            Assert.AreEqual(1m / 1.1m, price);
        }
    }
}
