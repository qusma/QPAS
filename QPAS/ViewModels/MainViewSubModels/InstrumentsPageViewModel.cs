// -----------------------------------------------------------------------
// <copyright file="InstrumentsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using OxyPlot;
using OxyPlot.Wpf;
using QDMS;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Instrument = EntityModel.Instrument;

namespace QPAS
{
    public sealed class InstrumentsPageViewModel : ViewModelBase
    {
        internal IDataSourcer Datasourcer;
        private readonly DataContainer _data;
        private readonly IMainViewModel _mainVm;

        private Instrument _selectedInstrument;
        private string _selectedStrategyName;
        private PlotModel _instrumentChartModel;
        private bool _isExternalClientConnected;
        private readonly IContextFactory contextFactory;

        public CollectionViewSource InstrumentsSource { get; set; }

        public ICommand UpdateChartCommand { get; set; }

        public ObservableCollection<string> StrategyNames { get; set; }

        public ObservableCollection<KeyValuePair<string, int?>> ExternalInstruments { get; set; }

        public string SelectedStrategyName
        {
            get => _selectedStrategyName;
            set => this.RaiseAndSetIfChanged(ref _selectedStrategyName, value);
        }

        public Instrument SelectedInstrument
        {
            get => _selectedInstrument;
            set => this.RaiseAndSetIfChanged(ref _selectedInstrument, value);
        }

        public bool IsExternalClientConnected
        {
            get => _isExternalClientConnected;
            set => this.RaiseAndSetIfChanged(ref _isExternalClientConnected, value);
        }

        public bool ShowOrderAnnotations { get; set; }

        public bool ShowQuantityAnnotations { get; set; }

        public PlotModel InstrumentChartModel
        {
            get => _instrumentChartModel;
            set => this.RaiseAndSetIfChanged(ref _instrumentChartModel, value);
        }

        public ICommand CopyChart { get; private set; }

        public ICommand SaveChart { get; private set; }

        public InstrumentsPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, IDataSourcer datasourcer, DataContainer data, IMainViewModel mainVm)
            : base(dialogService)
        {
            Datasourcer = datasourcer;
            _data = data;
            _mainVm = mainVm;
            StrategyNames = new ObservableCollection<string>();
            ExternalInstruments = new ObservableCollection<KeyValuePair<string, int?>>();

            InstrumentsSource = new CollectionViewSource();
            InstrumentsSource.Source = data.Instruments;
            InstrumentsSource.View.SortDescriptions.Add(new SortDescription("Symbol", ListSortDirection.Ascending));

            CreateCommands();
            this.contextFactory = contextFactory;
        }

        private void CreateCommands()
        {
            UpdateChartCommand = ReactiveCommand.CreateFromTask(async _ => await UpdateChart());

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

        private async Task UpdateChart()
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
                data = await Datasourcer.GetAllExternalData(SelectedInstrument);

                //we couldn't get external data, use internal instead
                if (data.Count == 0)
                {
                    var firstOrder = _data.Orders.Where(x => x.InstrumentID == SelectedInstrument.ID).OrderBy(x => x.TradeDate).FirstOrDefault();
                    var lastOrder = _data.Orders.Where(x => x.InstrumentID == SelectedInstrument.ID).OrderByDescending(x => x.TradeDate).FirstOrDefault();

                    if (firstOrder != null && lastOrder != firstOrder)
                    {
                        data = Datasourcer.GetLocalData(SelectedInstrument, firstOrder.TradeDate, lastOrder.TradeDate);
                    }
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowMessageAsync(_mainVm, "Error Getting Data", ex.Message);
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
            var orders = _data.Orders.Where(x => x.InstrumentID == SelectedInstrument.ID);

            if (SelectedStrategyName != "All")
            {
                var strat = _data.Strategies.FirstOrDefault(x => x.Name == SelectedStrategyName);
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

            foreach (Strategy s in _data.Strategies)
            {
                if (!StrategyNames.Contains(s.Name))
                {
                    StrategyNames.Add(s.Name);
                }
            }
        }

        private async Task PopulateQDMSInstruments()
        {
            //fill qdms instrument combobox
            if (Datasourcer.ExternalDataSource != null && Datasourcer.ExternalDataSource.Connected)
            {
                ExternalInstruments.Clear();
                ExternalInstruments.Add(new KeyValuePair<string, int?>("Auto", null));

                foreach (var kvp in await Datasourcer.ExternalDataSource.GetInstrumentDict())
                {
                    ExternalInstruments.Add(new KeyValuePair<string, int?>(kvp.Key, kvp.Value));
                }
            }
        }

        public override async Task Refresh()
        {
            PopulateStrategyNames();
            await PopulateQDMSInstruments();

            if (Datasourcer.ExternalDataSource != null)
            {
                IsExternalClientConnected = Datasourcer.ExternalDataSource.Connected;
            }
        }
    }
}