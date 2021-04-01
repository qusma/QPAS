// -----------------------------------------------------------------------
// <copyright file="IDataSources.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Instrument = EntityModel.Instrument;

namespace QPAS
{
    public interface IDataSourcer : IDisposable
    {
        IExternalDataSource ExternalDataSource { get; }

        Task<List<OHLCBar>> GetData(Instrument inst, DateTime startTime, DateTime endTime, BarSize frequency = BarSize.OneDay);

        Task<List<OHLCBar>> GetAllExternalData(Instrument inst);

        Task<List<OHLCBar>> GetExternalData(int externalInstrumentID, DateTime startTime, DateTime endTime);

        List<OHLCBar> GetLocalData(Instrument instrument, DateTime fromDate, DateTime toDate);

        decimal? GetLastPrice(Instrument inst, out decimal fxRate, string currency = "USD");
    }
}