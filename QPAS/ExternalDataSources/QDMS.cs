// -----------------------------------------------------------------------
// <copyright file="QDMS.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NLog;
using QDMS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Instrument = QDMS.Instrument;
using Timer = System.Timers.Timer;

namespace QPAS.ExternalDataSources
{
    public class QDMS : IExternalDataSource
    {
        public string Name => "Interactive Brokers";


        private QDMSClient.QDMSClient _client;
        private List<Instrument> _instrumentsList;
        private readonly Dictionary<int, StoredDataInfo> _storageInfo;
        private DateTime _lastInstrumentsListRefresh;

        private readonly object _storageInfoLock = new object();
        private readonly object _arrivedDataLock = new object();
        private readonly object _requestHistoricalDataLock = new object();

        private readonly SemaphoreSlim _instrumentInfoReqSemaphor = new SemaphoreSlim(1,1);

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Timer _connectionTimer;

        /// <summary>
        /// Not all historical data arrivals are requested in here, so we keep track of the IDs
        /// Key: request ID
        /// Value: true if the request has arrived
        /// </summary>
        private readonly Dictionary<int, bool> _requestIDs;
        private readonly IAppSettings _settings;
        private readonly List<EntityModel.DatasourcePreference> _dataSourcePreferences;

        /// <summary>
        /// Key: request ID
        /// Value: the data
        /// </summary>
        private Dictionary<int, List<OHLCBar>> _arrivedData;

        private string _connectionStatus;

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set { _connectionStatus = value; OnPropertyChanged(); }
        }

        public bool Connected => _client.Connected;

        public QDMS(IAppSettings settings, List<EntityModel.DatasourcePreference> dataSourcePreferences)
        {
            _client = QDMSClientFactory.Get(settings);



            _connectionTimer = new Timer(2000);
            _connectionTimer.Elapsed += _connectionTimer_Elapsed;

            if (settings.AllowExternalDataSource)
            {
                ConnectToDataServer();
                _connectionTimer.Start();
            }

            _requestIDs = new Dictionary<int, bool>();
            _arrivedData = new Dictionary<int, List<OHLCBar>>();
            _instrumentsList = new List<Instrument>();
            _lastInstrumentsListRefresh = new DateTime(1, 1, 1);
            _storageInfo = new Dictionary<int, StoredDataInfo>();

            _client.HistoricalDataReceived += dataClient_HistoricalDataReceived;
            _client.Error += _client_Error;
            _client.PropertyChanged += _client_PropertyChanged;

            _settings = settings;
            _dataSourcePreferences = dataSourcePreferences;

            RefreshInstrumentsList();
        }

        private void _client_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Connected")
            {
                OnPropertyChanged("Connected");
            }
        }

        void _client_Error(object sender, ErrorArgs e)
        {
            ConnectionStatus = string.Format("{0} | {1}",
                _client.Connected ? "Connected" : "Disconnected",
                e.ErrorMessage);
            _logger.Error("QDMS client error: " + e.ErrorMessage);
        }

        public async Task<List<OHLCBar>> GetData(EntityModel.Instrument instrument, DateTime from, DateTime to, BarSize frequency = BarSize.OneDay)
        {
            if (!_client.Connected) return null;

            await RefreshInstrumentsList();

            var qdmsInst = instrument.GetQDMSInstrument(_instrumentsList, _dataSourcePreferences);
            if (qdmsInst == null) //nothing like this in QDMS, just grab local data
            {
                return null;
            }

            //grab the data
            return await RequestData(qdmsInst, from, to, frequency);
        }

        public async Task<List<OHLCBar>> GetAllData(EntityModel.Instrument instrument, BarSize frequency = BarSize.OneDay)
        {
            if (!_client.Connected) return new List<OHLCBar>();

            await RefreshInstrumentsList();

            //find instrument
            var qdmsInst = instrument.GetQDMSInstrument(_instrumentsList, _dataSourcePreferences);
            if (qdmsInst == null) //nothing like this in QDMS, just grab local data
            {
                _logger.Warn("No QDMS instrument found for " + instrument.ToString() + ". Using local data only.");
                return new List<OHLCBar>();
            }

            StoredDataInfo dataInfo = await TryGetStorageInfo(qdmsInst);
            if (dataInfo == null)
            {
                return new List<OHLCBar>();
            }

            return await GetData(
                instrument,
                dataInfo.EarliestDate,
                dataInfo.LatestDate,
                frequency);
        }

        public async Task<List<OHLCBar>> GetData(int externalInstrumentID, DateTime from, DateTime to, BarSize frequency = BarSize.OneDay)
        {
            if (!_client.Connected) return null;
            await RefreshInstrumentsList();
            var instrument = _instrumentsList.FirstOrDefault(x => x.ID == externalInstrumentID);
            if (instrument == null) return null;
            return await RequestData(instrument, from, to);
        }

        /// <summary>
        /// Get the latest price we have available for an instrument
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="lastDate"></param>
        /// <returns>Last price and last date</returns>
        public async Task<(decimal?, DateTime?)> GetLastPrice(EntityModel.Instrument instrument)
        {
            var lastDate = new DateTime(1, 1, 1);

            var qdmsInst = instrument.GetQDMSInstrument(_instrumentsList, _dataSourcePreferences);
            if (qdmsInst?.ID == null)
            {
                return (null, null);
            }

            StoredDataInfo dataInfo = await TryGetStorageInfo(qdmsInst);
            if (dataInfo == null)
            {
                return (null, null);
            }

            var lastAvailableDate = dataInfo.LatestDate;

            //Send out the request for the data
            var req = new HistoricalDataRequest
            {
                Instrument = qdmsInst,
                StartingDate = lastAvailableDate.AddDays(-1),
                EndingDate = lastAvailableDate,
                Frequency = BarSize.OneDay,
                DataLocation = _settings.QdmsAllowFreshData ? DataLocation.Both : DataLocation.LocalOnly,
                RTHOnly = true,
                SaveDataToStorage = false
            };

            int requestId;
            lock (_requestHistoricalDataLock)
            {
                requestId = _client.RequestHistoricalData(req);
                _requestIDs.Add(requestId, false);
            }
            


            //Wait until the data arrives
            int i = 0;
            while (i < 300)
            {
                Thread.Sleep(10);
                lock (_arrivedDataLock)
                {
                    if (_requestIDs[requestId])
                    {
                        var data = _arrivedData[requestId].Last();
                        _arrivedData.Remove(requestId);
                        lastDate = data.DT;
                        return (data.Close, lastDate);
                    }
                }
                i++;
            }

            return (null, null);
        }

        public async Task<Dictionary<string, int>> GetInstrumentDict()
        {
            await RefreshInstrumentsList();
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

        /// <summary>
        /// Retrieve a list of sessions that describe the regular trading hours for this instrument.
        /// </summary>
        public async Task<List<InstrumentSession>> GetSessions(EntityModel.Instrument instrument)
        {
            await RefreshInstrumentsList();
            var qdmsInstrument = instrument.GetQDMSInstrument(_instrumentsList, _dataSourcePreferences);

            if (qdmsInstrument?.Sessions == null)
            {
                _logger.Log(LogLevel.Info, string.Format("QDMS instrument not found for local instrument: {0}", instrument));
                return new List<InstrumentSession>();
            }
            return qdmsInstrument.Sessions.ToList();
        }

        /// <summary>
        /// Gets a list of available instruments that represent backtest results.
        /// </summary>
        public async Task<List<Instrument>> GetBacktestSeries()
        {
            await RefreshInstrumentsList();
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
                        OnPropertyChanged(nameof(Connected));
                    }
                }
                catch (Exception ex)
                {
                    ConnectionStatus = "Error connecting: " + ex.Message;
                    if (Application.Current != null)
                        Application.Current.Dispatcher.Invoke(() => _logger.Log(LogLevel.Error, "Error connecting to QDMS: " + ex.Message));
                }
            }
        }

        private async Task<List<OHLCBar>> RequestData(Instrument instrument, DateTime startTime, DateTime endTime, BarSize frequency = BarSize.OneDay)
        {
            if (!instrument.ID.HasValue) return null;

            //the data doesn't exist locally, request it
            var req = new HistoricalDataRequest
            {
                Instrument = instrument,
                StartingDate = startTime.Date,
                EndingDate = endTime.Date,
                Frequency = frequency,
                DataLocation = _settings.QdmsAllowFreshData ? DataLocation.Both : DataLocation.LocalOnly,
                RTHOnly = true,
                SaveDataToStorage = true
            };

            int requestID;
            lock (_requestHistoricalDataLock)
            {
                requestID = _client.RequestHistoricalData(req);
                _requestIDs.Add(requestID, false);
            }

            //Wait until the data arrives
            int i = 0;
            return await Task.Run(() =>
            {
                while (i < 300)
                {
                    Thread.Sleep(10);
                    lock (_arrivedDataLock)
                    {
                        if (_requestIDs[requestID])
                        {
                            var data = _arrivedData[requestID];
                            _arrivedData.Remove(requestID);
                            return data;
                        }
                    }
                    i++;
                }

                return new List<OHLCBar>();
            });
        }

        private void dataClient_HistoricalDataReceived(object sender, HistoricalDataEventArgs e)
        {
            if (!_requestIDs.ContainsKey(e.Request.RequestID)) return;
            if (e.Request.Instrument.ID == null) throw new Exception("Null instrument ID return wtf");

            lock (_arrivedDataLock)
            {
                int id = e.Request.RequestID;
                _arrivedData.Add(id, e.Data);
                _requestIDs[e.Request.RequestID] = true;
            }
        }


        private async Task RefreshInstrumentsList()
        {
            if (!_client.Connected) return;

            await _instrumentInfoReqSemaphor.WaitAsync();
            if (_instrumentsList.Count == 0 || (DateTime.Now - _lastInstrumentsListRefresh).TotalSeconds > 30)
            {
                var instrumentsReq = await _client.GetInstruments();
                if (!instrumentsReq.WasSuccessful)
                {
                    _logger.Error("Error getting instrument list: " + string.Join(",", instrumentsReq.Errors));
                    _instrumentInfoReqSemaphor.Release();
                    return;
                }

                _instrumentsList = instrumentsReq.Result;
                _lastInstrumentsListRefresh = DateTime.Now;
            }
            _instrumentInfoReqSemaphor.Release();
        }

        /// <summary>
        /// Tries to retrieve data on locally available data for a given instrument.
        /// Returns DateTime(1,1,1) if the request is not filled.
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns></returns>
        private async Task<StoredDataInfo> TryGetStorageInfo(Instrument instrument)
        {
            if (instrument.ID == null) throw new Exception("Null instrument ID return wtf");
            int id = instrument.ID.Value;

            lock (_storageInfoLock)
            {
                if (_storageInfo.ContainsKey(id))
                {
                    return _storageInfo[id];
                }
            }


            var response = await _client.GetLocallyAvailableDataInfo(instrument);

            if (!response.WasSuccessful)
            {
                _logger.Error("Error getting data storage info: " + string.Join(",", response.Errors));
                return null;
            }

            
            StoredDataInfo info = response.Result.FirstOrDefault(x => x.Frequency == BarSize.OneDay);
            if (info == null) return null;

            lock (_storageInfoLock)
            {
                if (_storageInfo.ContainsKey(id))
                    _storageInfo[id] = info;
                else
                    _storageInfo.Add(id, info);
            }

            return info;
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
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
