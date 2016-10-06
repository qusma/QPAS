// -----------------------------------------------------------------------
// <copyright file="InstrumentsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using OxyPlot;
using OxyPlot.Wpf;
using QDMS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;
using Instrument = EntityModel.Instrument;

namespace QPAS
{
    public sealed class InstrumentsPageViewModel : ViewModelBase
    {
        internal IDBContext Context;
        internal IDataSourcer Datasourcer;
        private readonly IMainViewModel _mainVm;

        private Instrument _selectedInstrument;
        private string _selectedStrategyName;
        private PlotModel _instrumentChartModel;
        private bool _isExternalClientConnected;

        public CollectionViewSource InstrumentsSource { get; set; }

        public ICommand UpdateChartCommand { get; set; }

        public ObservableCollection<string> StrategyNames { get; set; }

        public ObservableCollection<KeyValuePair<string, int?>> ExternalInstruments { get; set; }

        public string SelectedStrategyName
        {
            get { return _selectedStrategyName; }
            set
            {
                if (value == _selectedStrategyName) return;
                _selectedStrategyName = value;
                OnPropertyChanged();
            }
        }

        public Instrument SelectedInstrument
        {
            get { return _selectedInstrument; }
            set
            {
                if (Equals(value, _selectedInstrument)) return;
                _selectedInstrument = value;
                OnPropertyChanged();
            }
        }

        public bool IsExternalClientConnected
        {
            get { return _isExternalClientConnected; }
            set
            {
                if (value.Equals(_isExternalClientConnected)) return;
                _isExternalClientConnected = value;
                OnPropertyChanged();
            }
        }

        public bool ShowOrderAnnotations { get; set; }

        public bool ShowQuantityAnnotations { get; set; }

        public PlotModel InstrumentChartModel
        {
            get { return _instrumentChartModel; }
            set
            {
                _instrumentChartModel = value;
                OnPropertyChanged();
            }
        }

        public ICommand CopyChart { get; private set; }

        public ICommand SaveChart { get; private set; }

        public InstrumentsPageViewModel(IDBContext context, IDialogCoordinator dialogService, IDataSourcer datasourcer, IMainViewModel mainVm)
            : base(dialogService)
        {
            Context = context;
            Datasourcer = datasourcer;
            _mainVm = mainVm;
            StrategyNames = new ObservableCollection<string>();
            ExternalInstruments = new ObservableCollection<KeyValuePair<string, int?>>();

            InstrumentsSource = new CollectionViewSource();
            InstrumentsSource.Source = Context.Instruments.Local;
            InstrumentsSource.View.SortDescriptions.Add(new SortDescription("Symbol", ListSortDirection.Ascending));

            CreateCommands();
        }

        private void CreateCommands()
        {
            UpdateChartCommand = new RelayCommand(UpdateChart);

            CopyChart = new RelayCommand<PlotView>(x => x.CopyToClipboard());
            SaveChart = new RelayCommand<PlotView>(x =>
            {
                try
                {
                    x.SaveAsPNG();
                }
                catch (Exception ex)
                {
                    DialogService.ShowMessageAsync(_mainVm, "Error saving image", ex.Message);
                }
            });
        }

        private void UpdateChart()
        {
            if (SelectedInstrument == null) return;
            if (Datasourcer.ExternalDataSource == null || !Datasourcer.ExternalDataSource.Connected) return;

            PlotModel newModel =
                InstrumentChartModel ?? InstrumentChartCreator.InitializePlotModel();

            newModel.Title = SelectedInstrument.Symbol;

            //if there's more than 3 series, here we delete any trade line series
            while (newModel.Series.Count > 3)
            {
                newModel.Series.RemoveAt(3);
            }

            //grab data
            List<OHLCBar> data;
            try
            {
                data = Datasourcer.GetAllExternalData(SelectedInstrument);
            }
            catch (Exception ex)
            {
                DialogService.ShowMessageAsync(_mainVm, "Error Getting Data", ex.Message);
                return;
            }

            //add OHLC bars to chart
            InstrumentChartCreator.AddCandlesticks(data, newModel);

            //Tuple: trade time, price, quantity
            List<Tuple<DateTime, decimal, int>> groupedOrders = GetGroupedOrders();

            //add transaction markers
            InstrumentChartCreator.AddTransactionScatterPoints(groupedOrders, newModel, ShowOrderAnnotations, ShowQuantityAnnotations);

            //then we create the lines between trades
            InstrumentChartCreator.AddProfitLossLines(groupedOrders, newModel);

            InstrumentChartModel = newModel;

            //and finally refresh the chart
            InstrumentChartModel.InvalidatePlot(true);
        }

        private List<Tuple<DateTime, decimal, int>> GetGroupedOrders()
        {
            //grab the applicable orders dependong on the selected strategy and instrument
            var orders = Context.Orders.Where(x => x.InstrumentID == SelectedInstrument.ID);

            if (SelectedStrategyName != "All")
            {
                var strat = Context.Strategies.FirstOrDefault(x => x.Name == SelectedStrategyName);
                if (strat == null) return new List<Tuple<DateTime, decimal, int>>();

                orders = orders.Where(x => x.Trade != null && x.Trade.StrategyID == strat.ID);
            }

            //group orders by datetime and price
            return orders
                .ToList()
                .GroupBy(x => new { x.TradeDate, x.Price })
                .OrderBy(x => x.Key.TradeDate)
                .Select(x => new Tuple<DateTime, decimal, int>(x.Key.TradeDate, x.Key.Price, x.Sum(y => y.Quantity)))
                .ToList();
        }

        private void PopulateStrategyNames()
        {
            if (StrategyNames.Count == 0)
            {
                StrategyNames.Add("All");
            }

            foreach (Strategy s in Context.Strategies)
            {
                if (!StrategyNames.Contains(s.Name))
                {
                    StrategyNames.Add(s.Name);
                }
            }
        }

        private void PopulateQDMSInstruments()
        {
            //fill qdms instrument combobox
            if (Datasourcer.ExternalDataSource != null && Datasourcer.ExternalDataSource.Connected)
            {
                ExternalInstruments.Clear();
                ExternalInstruments.Add(new KeyValuePair<string, int?>("Auto", null));

                foreach (var kvp in Datasourcer.ExternalDataSource.GetInstrumentDict())
                {
                    ExternalInstruments.Add(new KeyValuePair<string, int?>(kvp.Key, kvp.Value));
                }
            }
        }

        public override void Refresh()
        {
            Context.Instruments.OrderBy(x => x.Symbol).Load();
            PopulateStrategyNames();
            PopulateQDMSInstruments();

            if (Datasourcer.ExternalDataSource != null)
            {
                IsExternalClientConnected = Datasourcer.ExternalDataSource.Connected;
            }

            InstrumentsSource.View.Refresh();
        }
    }
}