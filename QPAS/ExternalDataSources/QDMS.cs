// -----------------------------------------------------------------------
// <copyright file="QDMS.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using System.Windows;
using NLog;
using QDMS;
using Timer = System.Timers.Timer;

namespace QPAS.ExternalDataSources
{
    public class QDMS : IExternalDataSource
    {
        public string Name { get { return "Interactive Brokers"; } }


        private QDMSClient.QDMSClient _client;
        private List<Instrument> _instrumentsList;
        private readonly Dictionary<int, StoredDataInfo> _storageInfo;
        private DateTime _lastInstrumentsListRefresh;

        private readonly object _storageInfoLock = new object();
        private readonly object _arrivedDataLock = new object();

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Timer _connectionTimer;

        private bool _allowFreshData;

        /// <summary>
        /// Not all historical data arrivals are requested in here, so we keep track of the IDs
        /// Key: request ID
        /// Value: true if the request has arrived
        /// </summary>
        private readonly Dictionary<int, bool> _requestIDs;

        /// <summary>
        /// Key: request ID
        /// Value: the data
        /// </summary>
        private Dictionary<int, List<OHLCBar>> _arrivedData;

        private string _connectionStatus;

        public string ConnectionStatus
        {
            get { return _connectionStatus; }
            private set { _connectionStatus = value; OnPropertyChanged(); }
        }

        public bool Connected
        {
            get
            {
                return _client.Connected;
            }
        }

        public QDMS()
        {
            _client = QDMSClientFactory.Get();

            ConnectToDataServer();

            _allowFreshData = Properties.Settings.Default.qdmsAllowFreshData;

            _connectionTimer = new Timer(2000);
            _connectionTimer.Elapsed += _connectionTimer_Elapsed;
            _connectionTimer.Start();

            _requestIDs = new Dictionary<int, bool>();
            _arrivedData = new Dictionary<int, List<OHLCBar>>();
            _instrumentsList = new List<Instrument>();
            _lastInstrumentsListRefresh = new DateTime(1, 1, 1);
            _storageInfo = new Dictionary<int, StoredDataInfo>();

            _client.HistoricalDataReceived += dataClient_HistoricalDataReceived;
            _client.LocallyAvailableDataInfoReceived += DataClient_LocallyAvailableDataInfoReceived;
            _client.Error += _client_Error;

            RefreshInstrumentsList();
        }

        void _client_Error(object sender, ErrorArgs e)
        {
            ConnectionStatus = string.Format("{0} | {1}",
                _client.Connected ? "Connected" : "Disconnected",
                e.ErrorMessage);
        }

        public List<OHLCBar> GetData(EntityModel.Instrument instrument, DateTime from, DateTime to, BarSize frequency = BarSize.OneDay)
        {
            if (!_client.Connected) return null;

            RefreshInstrumentsList();

            var qdmsInst = instrument.GetQDMSInstrument(_instrumentsList);
            if (qdmsInst == null) //nothing like this in QDMS, just grab local data
            {
                return null;
            }
            StoredDataInfo dataInfo = TryGetStorageInfo(qdmsInst);

            //Here we check if there's is absolutely no 
            if ((dataInfo == null || dataInfo.LatestDate < from || dataInfo.EarliestDate > to) &&
                !_allowFreshData)
            {
                return null;
            }

            //grab the data
            return RequestData(qdmsInst, from, to, frequency);
        }

        public List<OHLCBar> GetAllData(EntityModel.Instrument instrument, BarSize frequency = BarSize.OneDay)
        {
            if (!_client.Connected) return new List<OHLCBar>();

            RefreshInstrumentsList();

            //find instrument
            var qdmsInst = instrument.GetQDMSInstrument(_instrumentsList);
            if (qdmsInst == null) //nothing like this in QDMS, just grab local data
            {
                return new List<OHLCBar>();
            }

            StoredDataInfo dataInfo = TryGetStorageInfo(qdmsInst);
            if (dataInfo == null)
            {
                return new List<OHLCBar>();
            }

            return GetData(
                instrument, 
                dataInfo.EarliestDate, 
                dataInfo.LatestDate,
                frequency);
        }

        public List<OHLCBar> GetData(int externalInstrumentID, DateTime from, DateTime to, BarSize frequency = BarSize.OneDay)
        {
            if (!_client.Connected) return null;
            RefreshInstrumentsList();
            var instrument = _instrumentsList.FirstOrDefault(x => x.ID == externalInstrumentID);
            if (instrument == null) return null;
            return RequestData(instrument, from, to);
        }

        public decimal? GetLastPrice(EntityModel.Instrument instrument, out DateTime lastDate)
        {
            lastDate = new DateTime(1, 1, 1);

            var qdmsInst = instrument.GetQDMSInstrument(_instrumentsList);
            if (qdmsInst == null || !qdmsInst.ID.HasValue)
            {
                return null;
            }

            StoredDataInfo dataInfo = TryGetStorageInfo(qdmsInst);
            if (dataInfo == null)
            {
                return null;
            }

            var lastAvailableDate = dataInfo.LatestDate;

            //Send out the request for the data
            var req = new HistoricalDataRequest
            {
                Instrument = qdmsInst,
                StartingDate = lastAvailableDate.AddDays(-1),
                EndingDate = lastAvailableDate,
                Frequency = BarSize.OneDay,
                DataLocation = _allowFreshData ? DataLocation.Both : DataLocation.LocalOnly,
                RTHOnly = true,
                SaveDataToStorage = false
            };
            var id = _client.RequestHistoricalData(req);
            _requestIDs.Add(id, false);


            //Wait until the data arrives
            int i = 0;
            while (i < 100)
            {
                Thread.Sleep(20);
                lock (_arrivedDataLock)
                {
                    if (_requestIDs[id])
                    {
                        var data = _arrivedData[qdmsInst.ID.Value].Last();
                        _arrivedData.Remove(qdmsInst.ID.Value);
                        lastDate = data.DT;
                        return data.Close;
                    }
                }
                i++;
            }

            return null;
        }

        public Dictionary<string, int> GetInstrumentDict()
        {
            RefreshInstrumentsList();
            var items = _instrumentsList
                        .OrderBy(x => x.Symbol)
                        .Where(x => x.ID.HasValue)
                        .ToDictionary(x => String.Format("{0} ({1}) from {2} [ID: {3}]",
                            x.Symbol,
                            x.Type,
                            x.Datasource.Name,
                            x.ID),
                        x => x.ID.Value);

            return items;
        }

        public List<object> GetInstrumentList()
        {
            RefreshInstrumentsList();
            return new List<object>(_instrumentsList);
        }

        /// <summary>
        /// Retrieve a list of sessions that describe the regular trading hours for this instrument.
        /// </summary>
        public List<InstrumentSession> GetSessions(EntityModel.Instrument instrument)
        {
            RefreshInstrumentsList();
            var qdmsInstrument = instrument.GetQDMSInstrument(_instrumentsList);

            if(qdmsInstrument == null || qdmsInstrument.Sessions == null) 
            {
                _logger.Log(LogLevel.Info, string.Format("QDMS instrument not found for local instrument: {0}", instrument));
                return new List<InstrumentSession>();
            }
            return qdmsInstrument.Sessions.ToList();
        }

        /// <summary>
        /// Gets a list of available instruments that represent backtest results.
        /// </summary>
        public List<Instrument> GetBacktestSeries()
        {
            RefreshInstrumentsList();
            return _instrumentsList.Where(x => x.Type == InstrumentType.Backtest).ToList();
        }

        private void _connectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ConnectToDataServer();
        }

        private void ConnectToDataServer()
        {
            if (!_client.Connected)
            {
                try
                {
                    _client.Connect();
                    if (_client.Connected)
                    {
                        ConnectionStatus = "Connected";
                        OnPropertyChanged("Connected");
                    }
                }
                catch (Exception ex)
                {
                    ConnectionStatus = "Error connecting: " + ex.Message;
                    if(Application.Current != null)
                        Application.Current.Dispatcher.Invoke(() =>_logger.Log(LogLevel.Error, "Error connecting to QDMS: " + ex.Message));
                }
            }
        }

        private List<OHLCBar> RequestData(Instrument instrument, DateTime startTime, DateTime endTime, BarSize frequency = BarSize.OneDay)
        {
            if (!instrument.ID.HasValue) return null;

            //the data doesn't exist locally, request it
            var req = new HistoricalDataRequest
            {
                Instrument = instrument,
                StartingDate = startTime.Date,
                EndingDate = endTime.Date,
                Frequency = frequency,
                DataLocation = _allowFreshData ? DataLocation.Both : DataLocation.LocalOnly,
                RTHOnly = true,
                SaveDataToStorage = true
            };

            int requestID = _client.RequestHistoricalData(req);
            _requestIDs.Add(requestID, false);

            //Wait until the data arrives
            int i = 0;
            while (i < 100)
            {
                Thread.Sleep(20);
                lock (_arrivedDataLock)
                {
                    if (_requestIDs[requestID])
                    {
                        var data = _arrivedData[instrument.ID.Value];
                        _arrivedData.Remove(instrument.ID.Value);
                        return data;
                    }
                }
                i++;
            }

            return new List<OHLCBar>();
        }

        private void dataClient_HistoricalDataReceived(object sender, HistoricalDataEventArgs e)
        {
            if (!_requestIDs.ContainsKey(e.Request.RequestID)) return;
            if (e.Request.Instrument.ID == null) throw new Exception("Null instrument ID return wtf");

            lock (_arrivedDataLock)
            {
                int id = e.Request.Instrument.ID.Value;
                _arrivedData.Add(id, e.Data);
                _requestIDs[e.Request.RequestID] = true;
            }
        }


        private void RefreshInstrumentsList()
        {
            if (!_client.Connected) return;

            if (_instrumentsList.Count == 0 || (DateTime.Now - _lastInstrumentsListRefresh).TotalSeconds > 10)
            {
                _instrumentsList = _client.GetAllInstruments();
                _lastInstrumentsListRefresh = DateTime.Now;
            }
        }

        /// <summary>
        /// Tries to retrieve data on locally available data for a given instrument.
        /// Returns DateTime(1,1,1) if the request is not filled.
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns></returns>
        private StoredDataInfo TryGetStorageInfo(Instrument instrument)
        {
            if (instrument.ID == null) throw new Exception("Null instrument ID return wtf");
            _client.GetLocallyAvailableDataInfo(instrument);

            //wait until the storage info arrives
            int i = 0;
            while (i < 100)
            {
                Thread.Sleep(20);
                lock (_storageInfoLock)
                {
                    if (_storageInfo.ContainsKey(instrument.ID.Value))
                    {
                        return _storageInfo[instrument.ID.Value];
                    }
                }
                i++;
            }

            return null;
        }

        private void DataClient_LocallyAvailableDataInfoReceived(object sender, LocallyAvailableDataInfoReceivedEventArgs e)
        {
            if (e.Instrument.ID == null) throw new Exception("Null instrument ID return wtf");
            int id = e.Instrument.ID.Value;
            lock (_storageInfoLock)
            {
                StoredDataInfo info = e.StorageInfo.FirstOrDefault(x => x.Frequency == BarSize.OneDay);
                if (info == null) return;

                if (_storageInfo.ContainsKey(id))
                    _storageInfo[id] = info;
                else
                    _storageInfo.Add(id, info);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_connectionTimer != null)
            {
                _connectionTimer.Dispose();
                _connectionTimer = null;
            }
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
