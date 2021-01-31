// -----------------------------------------------------------------------
// <copyright file="DataSourcerTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using QDMS;
using QPAS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Currency = EntityModel.Currency;
using Instrument = EntityModel.Instrument;

namespace QPASTest
{
    [TestFixture]
    public class DataSourcerTest
    {
        private DataSourcer _datasourcer;
        private DbContextOptions<QpasDbContext> _dbContextOptions;
        private Mock<IExternalDataSource> _externalSourceMock;
        private Mock<IQpasDbContext> _contextMock;
        private QpasDbContext _dbContext;
        private DbContextFactory _contextFactory;
        private DataContainer _data;

        [SetUp]
        public void SetUp()
        {
            _dbContextOptions = new DbContextOptionsBuilder<QpasDbContext>()
                .UseInMemoryDatabase(databaseName: "qpastestdb")
                .Options;

            _externalSourceMock = new Mock<IExternalDataSource>();
            _contextMock = new Mock<IQpasDbContext>();

            _dbContext = new QpasDbContext(_dbContextOptions);

            _contextMock.Setup(x => x.PriorPositions).Returns(_dbContext.PriorPositions);
            _contextMock.Setup(x => x.FXRates).Returns(_dbContext.FXRates);
            _contextMock.Setup(x => x.Currencies).Returns(_dbContext.Currencies);
            _contextMock.Setup(x => x.PriorPositions).Returns(_dbContext.PriorPositions);

            _contextFactory = new DbContextFactory(() => _contextMock.Object);
            _data = new DataContainer();

            _datasourcer = new DataSourcer(_contextFactory, _externalSourceMock.Object, _data, true);
        }

        [Test]
        public async Task DataIsCachedBetweenRequests()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 1, 2);
            var inst = new Instrument { ID = 1, Symbol = "SPY", AssetCategory = AssetClass.Stock, QDMSInstrumentID = 2 };

            var data = new List<OHLCBar>
            {
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,1)},
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,2)},
            };

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .ReturnsAsync(data);

            await _datasourcer.GetData(inst, startDate, endDate).ConfigureAwait(true);

            //request a second time: should use cache instead of external source
            await _datasourcer.GetData(inst, startDate, endDate).ConfigureAwait(true);

            _externalSourceMock.Verify(x => x.GetData(
                It.IsAny<Instrument>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<BarSize>()),
                Times.Once);
        }

        [Test]
        public async Task ExternalDataIsSupplementedWithLocalDataWhenObservationsAreMissing()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 1, 3);
            var inst = new Instrument { ID = 1, Symbol = "SPY", AssetCategory = AssetClass.Stock, QDMSInstrumentID = 2 };

            var data = new List<OHLCBar>
            {
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,1)},
                new OHLCBar { Open = 1, High = 2, Low = 0, Close = 1, DT = new DateTime(2000,1,2)},
            };

            _dbContext.PriorPositions.Add(new PriorPosition() { Instrument = inst, InstrumentID = inst.ID, Date = endDate, Price = 5 });
            _dbContext.SaveChanges();

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .ReturnsAsync(data);

            var resultData = await _datasourcer.GetData(inst, startDate, endDate).ConfigureAwait(true);

            Assert.That(resultData.Count == 3);
        }

        [Test]
        public async Task CashInstrumentLocalRequestsUseFxRates()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 1, 3);
            var inst = new Instrument { ID = 1, Symbol = "EUR.USD", AssetCategory = AssetClass.Cash, QDMSInstrumentID = 2 };
            var eur = new Currency { ID = 2, Name = "EUR" };

            _data.Currencies.Add(eur);

            _data.FXRates.Add(new FXRate() { FromCurrencyID = 2, FromCurrency = eur, Rate = 1.5m, Date = endDate });

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .ReturnsAsync(new List<OHLCBar>());

            var resultData = await _datasourcer.GetData(inst, startDate, endDate).ConfigureAwait(true);

            Assert.That(resultData.Any(x => x.Date.ToDateTime() == endDate));
        }

        [Test]
        public async Task IfExternalSourceHasNoDataLocalBackupIsUsedInstead()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 2, 1);
            var inst = new Instrument { ID = 2, Symbol = "SPY", AssetCategory = AssetClass.Stock, QDMSInstrumentID = 2 };

            _dbContext.PriorPositions.Add(new PriorPosition() { Instrument = inst, InstrumentID = inst.ID, Date = endDate, Price = 5 });
            _dbContext.SaveChanges();

            _externalSourceMock.Setup(
                x => x.GetData(It.IsAny<Instrument>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<BarSize>()))
                    .ReturnsAsync(new List<OHLCBar>());

            var resultData = await _datasourcer.GetData(inst, startDate, endDate).ConfigureAwait(true);

            _externalSourceMock.Verify(x => x.GetData(
                It.IsAny<Instrument>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<BarSize>()));

            Assert.That(resultData.Any(x => x.Date.ToDateTime() == endDate));
        }

        [Test]
        public async Task ExternalDataRequestsAreForwardedToExternalDatasource()
        {
            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endDate = new DateTime(2000, 2, 1);

            await _datasourcer.GetExternalData(1, startDate, endDate).ConfigureAwait(true);
            _externalSourceMock.Verify(x => x.GetData(
                It.Is<int>(y => y == 1),
                It.Is<DateTime>(y => y == startDate),
                It.Is<DateTime>(y => y == endDate),
                It.Is<BarSize>(y => y == BarSize.OneDay)));
        }

        [Test]
        public async Task FxDataIsInvertedWhenNecessary()
        {
            var inst = new Instrument { Symbol = "USD.CAD", ID = 1, AssetCategory = AssetClass.Cash };
            var startDate = new DateTime(2000, 1, 1);
            var endDate = new DateTime(2000, 1, 1);

            _data.FXRates.Add(new FXRate { FromCurrencyID = 2, ToCurrencyID = 1, Date = startDate, Rate = 1.1m });

            _data.Currencies.Add(new Currency { ID = 2, Name = "CAD" });

            var data = await _datasourcer.GetData(inst, startDate, endDate).ConfigureAwait(true);

            Assert.AreEqual(1m / 1.1m, data[0].Close);
        }

        [Test]
        public void LastPriceFxDataIsInvertedWhenNecessary()
        {
            var inst = new Instrument { Symbol = "USD.CAD", ID = 1, AssetCategory = AssetClass.Cash };
            var startDate = new DateTime(2000, 1, 1);

            _data.FXRates.Add(new FXRate { FromCurrencyID = 2, ToCurrencyID = 1, Date = startDate, Rate = 1.1m });

            _data.Currencies.Add(new Currency { ID = 2, Name = "CAD" });

            decimal fxRate;
            var price = _datasourcer.GetLastPrice(inst, out fxRate);

            Assert.AreEqual(1m / 1.1m, price);
        }
    }
}