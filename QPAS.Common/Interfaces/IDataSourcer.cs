// -----------------------------------------------------------------------
// <copyright file="IDataSources.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EntityModel;
using QDMS;
using Instrument = EntityModel.Instrument;
using Currency = EntityModel.Currency;

namespace QPAS
{
    public interface IDataSourcer : IDisposable
    {
        IExternalDataSource ExternalDataSource { get; }
        IDBContext Context { get; set; }
        Task<List<OHLCBar>> GetData(Instrument inst, DateTime startTime, DateTime endTime, BarSize frequency = BarSize.OneDay);
        Task<List<OHLCBar>> GetAllExternalData(Instrument inst);
        Task<List<OHLCBar>> GetExternalData(int externalInstrumentID, DateTime startTime, DateTime endTime);
        decimal? GetLastPrice(Instrument inst, out decimal fxRate, string currency = "USD");
        decimal GetLastFxRate(Currency currency);
    }
}
