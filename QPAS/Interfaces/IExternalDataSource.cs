// -----------------------------------------------------------------------
// <copyright file="IExternalDataSource.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using QDMS;

namespace QPAS
{
    public interface IExternalDataSource : IDisposable, INotifyPropertyChanged
    {
        string Name { get; }

        /// <summary>
        /// Details on the connection. Any errors, etc. go here.
        /// </summary>
        string ConnectionStatus { get; }

        /// <summary>
        /// Returns true if we're connected to the external data source.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Given a native-format Instrument, return data from the external data source between the from and to dates.
        /// </summary>
        List<OHLCBar> GetData(EntityModel.Instrument instrument, DateTime from, DateTime to, BarSize frequency = BarSize.OneDay);
        
        /// <summary>
        /// Given a native-format Instrument, return all available data from the external source.
        /// </summary>
        List<OHLCBar> GetAllData(EntityModel.Instrument instrument, BarSize frequency = BarSize.OneDay);

        /// <summary>
        /// Given an instrument ID from the external data source, returns data between the from and to dates.
        /// </summary>
        List<OHLCBar> GetData(int externalInstrumentID, DateTime from, DateTime to, BarSize frequency = BarSize.OneDay);

        /// <summary>
        /// Returns the last price and the DateTime at which it occured, from the external data source.
        /// Null if not found.
        /// </summary>
        decimal? GetLastPrice(EntityModel.Instrument instrument, out DateTime lastDate);

        /// <summary>
        /// Returns a Dictionary with info on instruments from the external data source.
        /// The string key is a description of the instrument.
        /// The value is the instrument ID.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, int> GetInstrumentDict();

        /// <summary>
        /// Returns a list of instruments in the format of the external data source.
        /// </summary>
        List<object> GetInstrumentList();

        /// <summary>
        /// Retrieve a list of sessions that describe the regular trading hours for this instrument.
        /// </summary>
        List<InstrumentSession> GetSessions(EntityModel.Instrument instrument);
    }
}
