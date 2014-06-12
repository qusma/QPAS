// -----------------------------------------------------------------------
// <copyright file="DataSourcer.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EntityModel;
using NLog;
using QDMS;
using Instrument = EntityModel.Instrument;

namespace QPAS
{
    /// <summary>
    /// Finds data from the appropriate data sources
    /// </summary>
    public class DataSourcer : IDataSourcer
    {
        public IExternalDataSource ExternalDataSource { get; private set; }

        public IDBContext Context { get; set; }

        /// <summary>
        /// Key: Instrument ID
        /// Value: Data
        /// </summary>
        private readonly Dictionary<int, List<OHLCBar>> _dataCache;

        private readonly object _dataCacheLock = new object();

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly bool _useExternalDataSource;

        public DataSourcer(IDBContext context, IExternalDataSource externalDataSource)
        {
            _useExternalDataSource = Properties.Settings.Default.allowExternalDataSource && externalDataSource != null;
            if (_useExternalDataSource)
            {
                ExternalDataSource = externalDataSource;
            }

            _dataCache = new Dictionary<int, List<OHLCBar>>();

            Context = context;
        }

        public List<OHLCBar> GetData(Instrument inst, DateTime startTime, DateTime endTime, BarSize frequency = BarSize.OneDay)
        {
            _logger.Log(LogLevel.Info, string.Format("Data request for {0} from {1} to {2} @ {3}", inst, startTime, endTime, frequency));

            //Check the cache
            lock (_dataCacheLock)
            {
                if (_dataCache.ContainsKey(inst.ID) &&
                    _dataCache[inst.ID].First().DT <= startTime &&
                    _dataCache[inst.ID].Last().DT >= endTime)
                {
                    _logger.Log(LogLevel.Info, "Found data in cache");
                    return _dataCache[inst.ID].Where(x => x.DT >= startTime && x.DT <= endTime).ToList();
                }
            }

            //if external data is not allowed, just grab the prior positions data
            if (!_useExternalDataSource)
            {
                return GetLocalData(inst, startTime, endTime);
            }

            //If the cache is not enough, go to the external datasource
            var data = ExternalDataSource.GetData(inst, startTime, endTime);

            //External datasource didn't have anything, get data from prior positions
            if (data == null || data.Count == 0)
            {
                _logger.Log(LogLevel.Info, "Data was not available externally, getting it form prior positions");
                return GetLocalData(inst, startTime, endTime);
            }

            //QDMS data does NOT cover the entire period requested. 
            //Try to supplement the data with the prices from prior positions
            if(frequency == BarSize.OneDay && (data.First().DT.Date > startTime.Date || data.Last().DT.Date < endTime))
            {
                SupplementWithLocalData(inst, startTime, endTime, ref data);
            }

            AddToCache(data, inst.ID);

            return data;
        }

        /// <summary>
        /// Use prior positions data to fill missing data from external source
        /// </summary>
        private void SupplementWithLocalData(Instrument inst, DateTime startTime, DateTime endTime, ref List<OHLCBar> data)
        {
            //lacks data before
            if(data.First().DT.Date > startTime.Date)
            {
                var localData = GetLocalData(inst, startTime, data.First().DT.AddDays(-1).Date);
                data.AddRange(localData);
            }

            //lacks data after
            if(data.Last().DT.Date < endTime.Date)
            {
                var localData = GetLocalData(inst, data.Last().DT.AddDays(1).Date, endTime);
                data.AddRange(localData);
            }

            data = data.Distinct((x,y) => x.DT.Date == y.DT.Date).OrderBy(x => x.DT).ToList();
        }

        private void AddToCache(List<OHLCBar> data, int instrumentID)
        {
            _logger.Log(LogLevel.Info, string.Format("Adding {0} points to cache from instrument id {1}", data.Count, instrumentID));
            if (_dataCache.ContainsKey(instrumentID))
            {
                _dataCache[instrumentID].AddRange(data);
                _dataCache[instrumentID] = _dataCache[instrumentID].Distinct((x, y) => x.LongDate == y.LongDate).OrderBy(x => x.DT).ToList();
            }
            else
            {
                _dataCache.Add(instrumentID, data);
            }
        }

        /// <summary>
        /// Used for the instrument chart.
        /// </summary>
        public List<OHLCBar> GetAllExternalData(Instrument inst)
        {
            if (!_useExternalDataSource)
            {
                _logger.Log(LogLevel.Info, string.Format("Request for all external data on instrument {0} not fulfilled, external data not allowed.", inst));
                return new List<OHLCBar>();
            }

            _logger.Log(LogLevel.Info, string.Format("Request for all external data on instrument {0}", inst));
            return ExternalDataSource.GetAllData(inst);
        }

        /// <summary>
        /// Used for benchmarking.
        /// </summary>
        public List<OHLCBar> GetExternalData(int externalInstrumentID, DateTime startTime, DateTime endTime)
        {
            if (!_useExternalDataSource)
            {
                _logger.Log(LogLevel.Info, string.Format("Request for external data on instrument with external ID {0} not fulfilled, external data not allowed.", externalInstrumentID));
                return new List<OHLCBar>();
            }

            return ExternalDataSource.GetData(externalInstrumentID, startTime, endTime);
        }

        private List<OHLCBar> GetLocalData(Instrument instrument, DateTime fromDate, DateTime toDate)
        {
            if(instrument.AssetCategory == AssetClass.Cash)
            {
                return GetInstrumentFxRatesData(instrument, fromDate, toDate);
            }
            else
            {
                return GetPriorPositionsData(instrument, fromDate, toDate);
            }
        }

        private List<OHLCBar> GetInstrumentFxRatesData(Instrument instrument, DateTime fromDate, DateTime toDate)
        {
            if(instrument.AssetCategory != AssetClass.Cash) throw new Exception("Wrong asset class.");

            string[] splitSymbol = instrument.Symbol.Split(".".ToCharArray());
            string fxCurrencyName = splitSymbol[0] == "USD" ? splitSymbol[1] : splitSymbol[0];

            var currency = Context.Currencies.FirstOrDefault(x => x.Name == fxCurrencyName);
            if (currency == null) throw new Exception(string.Format("Currency {0} not found.", fxCurrencyName));

            //Some currencies are in the format X.USD, others in format USD.X
            //The latter need to be inverted because all fx rates are given in X.USD
            bool invert = splitSymbol[0] == "USD";

            var prices = 
                Context
                .FXRates
                .Where(x => x.FromCurrencyID == currency.ID && x.Date >= fromDate && x.Date <= toDate)
                .OrderBy(x => x.Date)
                .Select(p => new {p.Date, Price = invert ? 1m / p.Rate : p.Rate})
                .ToList();

            _logger.Log(LogLevel.Info, string.Format("Retrieved {0} data points for instrument {1} from fx rates.", prices.Count, instrument));

            return prices
                .Select(
                    p =>
                        new OHLCBar
                        {
                            Open = p.Price,
                            High = p.Price,
                            Low = p.Price,
                            Close = p.Price,
                            AdjOpen = p.Price,
                            AdjHigh = p.Price,
                            AdjLow = p.Price,
                            AdjClose = p.Price,
                            DT = p.Date
                        })
                .ToList();
        }

        private List<OHLCBar> GetPriorPositionsData(Instrument instrument, DateTime fromDate, DateTime toDate)
        {
            var prices =
                Context
                .PriorPositions
                .Where(x => x.InstrumentID == instrument.ID && x.Date >= fromDate && x.Date <= toDate)
                .OrderBy(x => x.Date)
                .Select(p => new { p.Date, p.Price })
                .ToList();

            _logger.Log(LogLevel.Info, string.Format("Retrieved {0} data points for instrument {1} from prior positions.", prices.Count, instrument));

            return prices
                .Select(
                    p =>
                        new OHLCBar
                        {
                            Open = p.Price,
                            High = p.Price,
                            Low = p.Price,
                            Close = p.Price,
                            AdjOpen = p.Price,
                            AdjHigh = p.Price,
                            AdjLow = p.Price,
                            AdjClose = p.Price,
                            DT = p.Date
                        })
                .ToList();
        }

        public decimal? GetLastPrice(Instrument inst, out decimal fxRate, string currency = "USD")
        {
            _logger.Log(LogLevel.Info, string.Format("Last price request for {0} and currency {1}", inst, currency));
            DateTime lastLocalDate;
            decimal? lastLocalPrice = GetLocalLastPrice(inst, out fxRate, out lastLocalDate);

            if (!_useExternalDataSource || !ExternalDataSource.Connected)
            {
                return lastLocalPrice;
            }
            else
            {
                DateTime lastExternalDate;
                decimal? lastExternalPrice = ExternalDataSource.GetLastPrice(inst, out lastExternalDate);

                if (lastExternalPrice.HasValue && lastExternalDate >= lastLocalDate)
                {
                    fxRate = GetLastFXRate(currency);
                    return lastExternalPrice.Value;
                }
                else
                {
                    return lastLocalPrice;
                }
            }
        }

        public decimal GetLastFxRate(Currency currency)
        {
            return Context.FXRates.Where(x => x.FromCurrencyID == currency.ID).OrderByDescending(x => x.Date).First().Rate;
        }

        private decimal GetLastFXRate(string currency)
        {
            decimal fxRate;

            if (currency == "USD")
            {
                fxRate = 1;
            }
            else
            {
                var curr = Context.Currencies.FirstOrDefault(x => x.Name == currency);

                if (curr == null)
                {
                    throw new Exception("Could not find currency " + currency);
                }

                fxRate = Context.FXRates.Where(x => x.FromCurrencyID == curr.ID).OrderByDescending(x => x.Date).First().Rate;
            }
            return fxRate;
        }

        private decimal? GetLocalLastPrice(Instrument instrument, out decimal fxRate, out DateTime date)
        {
            if (instrument.AssetCategory == AssetClass.Cash)
            {
                return GetLocalLastFxRatePrice(instrument, out fxRate, out date);
            }
            else
            {
                return GetLocalLastPriorPositionPrice(instrument, out fxRate, out date);
            }
        }

        private decimal? GetLocalLastFxRatePrice(Instrument instrument, out decimal fxRate, out DateTime date)
        {
            if (instrument.AssetCategory != AssetClass.Cash) throw new Exception("Wrong asset class.");
            fxRate = 1;

            string[] splitSymbol = instrument.Symbol.Split(".".ToCharArray());
            string fxCurrencyName = splitSymbol[0] == "USD" ? splitSymbol[1] : splitSymbol[0];

            var currency = Context.Currencies.FirstOrDefault(x => x.Name == fxCurrencyName);
            if (currency == null) throw new Exception(string.Format("Currency {0} not found.", fxCurrencyName));

            //Some currencies are in the format X.USD, others in format USD.X
            //The latter need to be inverted because all fx rates are given in X.USD
            bool invert = splitSymbol[0] == "USD";

            var price =
                Context
                .FXRates
                .Where(x => x.FromCurrencyID == currency.ID)
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            if (price == null)
            {
                _logger.Log(LogLevel.Error, "Could not find any local data on instrument " + instrument.ToString());
                date = DateTime.Now;
                return null;
            }

            date = price.Date;
            return invert ? 1m / price.Rate : price.Rate;
        }

        private decimal? GetLocalLastPriorPositionPrice(Instrument inst, out decimal fxRate, out DateTime date)
        {
            var pos = Context.PriorPositions.Where(x => x.InstrumentID == inst.ID).OrderByDescending(x => x.Date).FirstOrDefault();
            if (pos == null)
            {
                _logger.Log(LogLevel.Error, "Could not find any local data on instrument " + inst.ToString());
                fxRate = 1;
                date = DateTime.Now;
                return null;
            }

            fxRate = pos.FXRateToBase;
            date = pos.Date;

            return pos.Price;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (ExternalDataSource != null)
            {
                ExternalDataSource.Dispose();
                ExternalDataSource = null;
            }
        }
    }
}