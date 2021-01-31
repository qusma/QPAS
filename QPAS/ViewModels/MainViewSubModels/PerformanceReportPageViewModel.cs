using DynamicData;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QPAS
{
    public class PerformanceReportPageViewModel : ViewModelBase
    {
        private string _toggleStratsText;
        private string _toggleTagsText;

        private readonly IContextFactory _contextFactory;
        private readonly DataContainer _data;
        internal IMainViewModel Parent;
        private string _toggleInstrumentsText;
        private IDataSourcer _datasourcer;
        private BacktestSource _backtestSource;

        public ReportSettings ReportSettings { get; set; }

        public TradeFilterSettings TradeFilterSettings { get; set; }

        public ObservableCollection<CheckListItem<Strategy>> Strategies { get; }

        public ObservableCollection<CheckListItem<Tag>> Tags { get; }

        public ObservableCollection<CheckListItem<Instrument>> Instruments { get; }

        public ObservableCollection<Benchmark> Benchmarks { get; }
        public ObservableCollection<QDMS.Instrument> BacktestSeries { get; }

        public BacktestSource BacktestSource
        {
            get => _backtestSource;
            set => this.RaiseAndSetIfChanged(ref _backtestSource, value);
        }

        public string ToggleStratsText
        {
            get => _toggleStratsText;
            set => this.RaiseAndSetIfChanged(ref _toggleStratsText, value);
        }

        public string ToggleTagsText
        {
            get => _toggleTagsText;
            set => this.RaiseAndSetIfChanged(ref _toggleTagsText, value);
        }

        public string ToggleInstrumentsText
        {
            get => _toggleInstrumentsText;
            set => this.RaiseAndSetIfChanged(ref _toggleInstrumentsText, value);
        }

        public IDataSourcer Datasourcer
        {
            get => _datasourcer;
            private set => this.RaiseAndSetIfChanged(ref _datasourcer, value);
        }

        public ICommand ToggleAllStrategies { get; set; }

        public ICommand ToggleAllTags { get; set; }

        public ICommand ToggleAllInstruments { get; set; }

        public ICommand GenerateReport { get; set; }

        public PerformanceReportPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, IDataSourcer datasourcer, DataContainer data, IMainViewModel parent)
            : base(dialogService)
        {
            _contextFactory = contextFactory;
            Parent = parent;
            Datasourcer = datasourcer;
            _data = data;
            ReportSettings = new ReportSettings();
            TradeFilterSettings = new TradeFilterSettings(_data.EquitySummaries);

            ToggleTagsText = "Select All";
            ToggleStratsText = "Select All";
            ToggleInstrumentsText = "Deselect All";

            Strategies = new ObservableCollection<CheckListItem<Strategy>>(data.Strategies.Select(x => new CheckListItem<Strategy>(x)));
            Tags = new ObservableCollection<CheckListItem<Tag>>(data.Tags.Select(x => new CheckListItem<Tag>(x)));
            Instruments = new ObservableCollection<CheckListItem<Instrument>>(data.Instruments.Select(x => new CheckListItem<Instrument>(x, true)));
            Benchmarks = new ObservableCollection<Benchmark>();
            BacktestSeries = new ObservableCollection<QDMS.Instrument>();

            data.Tags.CollectionChanged += Tags_CollectionChanged;
            data.Strategies.CollectionChanged += Strategies_CollectionChanged;
            data.Instruments.CollectionChanged += Instruments_CollectionChanged;

            CreateCommands();
        }

        private void Instruments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var newItems = e.NewItems.Cast<Instrument>().ToList();
                Instruments.AddRange(newItems.Select(x => new CheckListItem<Instrument>(x)));
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var removedItems = e.OldItems.Cast<Instrument>().ToList();
                foreach (var removedItem in removedItems)
                {
                    var toRemove = Instruments.FirstOrDefault(x => x.Item == removedItem);
                    if (toRemove != null)
                    {
                        Instruments.Remove(toRemove);
                    }
                }
            }
        }

        private void Strategies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var newItems = e.NewItems.Cast<Strategy>().ToList();
                Strategies.AddRange(newItems.Select(x => new CheckListItem<Strategy>(x)));
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var removedItems = e.OldItems.Cast<Strategy>().ToList();
                foreach (var removedItem in removedItems)
                {
                    var toRemove = Strategies.FirstOrDefault(x => x.Item == removedItem);
                    if (toRemove != null)
                    {
                        Strategies.Remove(toRemove);
                    }
                }
            }
        }

        private void Tags_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var newItems = e.NewItems.Cast<Tag>().ToList();
                Tags.AddRange(newItems.Select(x => new CheckListItem<Tag>(x)));
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var removedItems = e.OldItems.Cast<Tag>().ToList();
                foreach (var removedItem in removedItems)
                {
                    var toRemove = Tags.FirstOrDefault(x => x.Item == removedItem);
                    if (toRemove != null)
                    {
                        Tags.Remove(toRemove);
                    }
                }
            }
        }

        private void CreateCommands()
        {
            ToggleAllTags = new RelayCommand(ToggleTags);
            ToggleAllStrategies = new RelayCommand(ToggleStrats);
            ToggleAllInstruments = new RelayCommand(ToggleInstruments);
            GenerateReport = new RelayCommand(GenReport);
        }

        private void ToggleStrats()
        {
            if (ToggleStratsText == "Select All")
            {
                foreach (CheckListItem<Strategy> item in Strategies)
                {
                    item.IsChecked = true;
                }
                ToggleStratsText = "Deselect All";
            }
            else
            {
                foreach (CheckListItem<Strategy> item in Strategies)
                {
                    item.IsChecked = false;
                }
                ToggleStratsText = "Select All";
            }
        }

        private void ToggleTags()
        {
            if (ToggleTagsText == "Select All")
            {
                foreach (CheckListItem<Tag> item in Tags)
                {
                    item.IsChecked = true;
                }
                ToggleTagsText = "Deselect All";
            }
            else
            {
                foreach (CheckListItem<Tag> item in Tags)
                {
                    item.IsChecked = false;
                }
                ToggleTagsText = "Select All";
            }
        }

        private void ToggleInstruments()
        {
            if (ToggleInstrumentsText == "Select All")
            {
                foreach (CheckListItem<Instrument> item in Instruments)
                {
                    item.IsChecked = true;
                }
                ToggleInstrumentsText = "Deselect All";
            }
            else
            {
                foreach (CheckListItem<Instrument> item in Instruments)
                {
                    item.IsChecked = false;
                }
                ToggleInstrumentsText = "Select All";
            }
        }

        public override async Task Refresh()
        {
            //backtest results from the external data source
            BacktestSeries.Clear();
            if (Datasourcer.ExternalDataSource != null && Datasourcer.ExternalDataSource.Connected)
            {
                BacktestSeries.AddRange(
                    await Datasourcer
                        .ExternalDataSource
                        .GetBacktestSeries().ConfigureAwait(true));
            }
        }

        private TimeSeries GetBacktestData()
        {
            return new TimeSeries(new System.Collections.Generic.List<QDMS.OHLCBar>());
        }

        private void GenReport()
        {
            //Load backtest result if it has been specified
            var backtestData = GetBacktestData();

            var selectedTags =
                Tags
                .Where(x => x.IsChecked)
                .Select(x => x.Item)
                .ToList();

            var selectedStrategies =
                Strategies
                .Where(x => x.IsChecked)
                .Select(x => x.Item)
                .ToList();

            var selectedInstruments =
                Instruments
                .Where(x => x.IsChecked)
                .Select(x => x.Item)
                .ToList();

            var trades = TradeFiltering.Filter(selectedTags, selectedStrategies, selectedInstruments, _data.Trades, TradeFilterSettings);

            Parent.GenerateReportFromTrades.Execute(trades);
        }
    }
}