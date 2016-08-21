// -----------------------------------------------------------------------
// <copyright file="IDataSources.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EntityModel;
using QDMS;
using Instrument = EntityModel.Instrument;

namespace QPAS
{
    public interface IDataSourcer : IDisposable
    {
        IExternalDataSource ExternalDataSource { get; }
        IDBContext Context { get; set; }
        List<OHLCBar> GetData(Instrument inst, DateTime startTime, DateTime endTime, BarSize frequency = BarSize.OneDay);
        List<OHLCBar> GetAllExternalData(Instrument inst);
        List<OHLCBar> GetExternalData(int externalInstrumentID, DateTime startTime, DateTime endTime);
        decimal? GetLastPrice(Instrument inst, out decimal fxRate, string currency = "USD");
        decimal GetLastFxRate(Currency currency);
    }
}
