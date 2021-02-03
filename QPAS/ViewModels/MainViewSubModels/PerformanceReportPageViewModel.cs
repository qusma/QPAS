using DynamicData;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QPAS
{
    public class PerformanceReportPageViewModel : ViewModelBase
    {
        private readonly IContextFactory _contextFactory;
        private readonly DataContainer _data;
        internal readonly IMainViewModel Parent;
        private readonly IDataSourcer _datasourcer;
        private BacktestSource _backtestSource;
        private string _toggleStratsText;
        private string _toggleTagsText;
        private string _toggleInstrumentsText;
        private string _backtestFilePath;
        private ReportSettings _reportSettings = new ReportSettings();

        public ReportSettings ReportSettings
        {
            get { return _reportSettings; }
            set { this.RaiseAndSetIfChanged(ref _reportSettings, value); }
        }

        public ObservableCollection<ReportSettings> SavedReportSettings { get; } = new ObservableCollection<ReportSettings>();

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

        public ICommand ToggleAllStrategies { get; set; }

        public ICommand ToggleAllTags { get; set; }

        public ICommand ToggleAllInstruments { get; set; }

        public ICommand GenerateReport { get; set; }
        public ReactiveCommand<Unit, Unit> LoadBacktestFileCmd { get; private set; }
        public ReactiveCommand<Unit, Unit> NewSettingsCmd { get; private set; }
        public ReactiveCommand<Unit, Unit> SaveSettingsCmd { get; private set; }
        public ReactiveCommand<string, Unit> LoadSettingsCmd { get; private set; }
       

        public EquityCurve BacktestData { get; private set; }

        public string BacktestFilePath
        {
            get { return _backtestFilePath; }
            private set { this.RaiseAndSetIfChanged(ref _backtestFilePath, value); }
        }

        public PerformanceReportPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, IDataSourcer datasourcer, DataContainer data, IMainViewModel parent)
            : base(dialogService)
        {
            _contextFactory = contextFactory;
            Parent = parent;
            _datasourcer = datasourcer;
            _data = data;
            TradeFilterSettings = new TradeFilterSettings(_data.EquitySummaries);

            ToggleTagsText = "Deselect All";
            ToggleStratsText = "Select All";
            ToggleInstrumentsText = "Deselect All";

            Strategies = new ObservableCollection<CheckListItem<Strategy>>(data.Strategies.Select(x => new CheckListItem<Strategy>(x)));
            Tags = new ObservableCollection<CheckListItem<Tag>>(data.Tags.Select(x => new CheckListItem<Tag>(x, true)));
            Instruments = new ObservableCollection<CheckListItem<Instrument>>(data.Instruments.Select(x => new CheckListItem<Instrument>(x, true)));
            Benchmarks = new ObservableCollection<Benchmark>();
            BacktestSeries = new ObservableCollection<QDMS.Instrument>();

            data.Tags.CollectionChanged += Tags_CollectionChanged;
            data.Strategies.CollectionChanged += DataStrategies_CollectionChanged;
            data.Instruments.CollectionChanged += Instruments_CollectionChanged;

            CreateCommands();

            using (var dbContext = contextFactory.Get())
            {
                SavedReportSettings.AddRange(dbContext.ReportSettings.ToList());
            }
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

        private void DataStrategies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
            LoadBacktestFileCmd = ReactiveCommand.Create(() => LoadBacktestFile());
            NewSettingsCmd = ReactiveCommand.CreateFromTask(async () => await NewSettings());
            LoadSettingsCmd = ReactiveCommand.CreateFromTask<string>(async name => await LoadSettings(name));
            SaveSettingsCmd = ReactiveCommand.CreateFromTask(async () => await SaveSettings());
        }

        private async Task SaveSettings()
        {
            if (ReportSettings.Id == 0)
            {
                await NewSettings();
                return;
            }

            RecordSelectionIds();

            using (var dbContext = _contextFactory.Get())
            {
                dbContext.Entry(ReportSettings).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                dbContext.SaveChanges();
            }
        }

        private async Task LoadSettings(string name)
        {
            using (var dbContext = _contextFactory.Get())
            {
                var loadedSettings = dbContext.ReportSettings.FirstOrDefault(x => x.Name == name);
                if (loadedSettings == null)
                {
                    await DialogService.ShowMessageAsync(Parent, "Error", "Could not load selected settings");
                    return;
                }
                ReportSettings = loadedSettings;

                ApplySelectionIds();
            }
        }

        private async Task NewSettings()
        {
            //creates a new settings object with the values of the old one?
            string name = await DialogService.ShowInputAsync(Parent, "New Settings", "Enter name:");
            using (var dbContext = _contextFactory.Get())
            {
                var existing = dbContext.ReportSettings.FirstOrDefault(x => x.Name == name);
                if (existing != null)
                {
                    await DialogService.ShowMessageAsync(Parent, "Error", "A settings entry with that name already exists");
                    return;
                }
                ReportSettings.Id = 0;
                ReportSettings.Name = name;

                RecordSelectionIds();

                dbContext.Entry(ReportSettings).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                await dbContext.SaveChangesAsync();

                SavedReportSettings.Add(ReportSettings);
            }
        }

        /// <summary>
        /// Takes the selections of tags, strategies, and instruments and adds the selected IDs to the Settings
        /// </summary>
        private void RecordSelectionIds()
        {
            var selectedTags = Tags.Where(x => x.IsChecked).Select(x => x.Item.ID).ToList();
            var selectedStrategies = Strategies.Where(x => x.IsChecked).Select(x => x.Item.ID).ToList();
            var selectedInstruments = Instruments.Where(x => x.IsChecked).Select(x => x.Item.ID).ToList();

            ReportSettings.SelectedTags.AddRange(selectedTags);
            ReportSettings.SelectedStrategies.AddRange(selectedStrategies);
            ReportSettings.SelectedInstruments.AddRange(selectedInstruments);
        }

        /// <summary>
        /// When loading settings, takes the tag, strategy, instrument IDs and applies the selections
        /// </summary>
        private void ApplySelectionIds()
        {
            foreach (var tagCheckItem in Tags)
            {
                tagCheckItem.IsChecked = ReportSettings.SelectedTags.Contains(tagCheckItem.Item.ID);
            }

            foreach (var strategyCheckItem in Strategies)
            {
                strategyCheckItem.IsChecked = ReportSettings.SelectedStrategies.Contains(strategyCheckItem.Item.ID);
            }

            foreach (var instrumentCheckItem in Instruments)
            {
                instrumentCheckItem.IsChecked = ReportSettings.SelectedInstruments.Contains(instrumentCheckItem.Item.ID);
            }
        }

        private void LoadBacktestFile()
        {
            var window = new BacktestImportWindow();
            window.ShowDialog();

            if (!window.Canceled)
            {
                //quite ugly, but eh...
                BacktestFilePath = window.ViewModel.FilePath;
                BacktestData = window.ViewModel.EquityCurve;
            }
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
            if (_datasourcer.ExternalDataSource != null && _datasourcer.ExternalDataSource.Connected)
            {
                BacktestSeries.AddRange(
                    await _datasourcer
                        .ExternalDataSource
                        .GetBacktestSeries());
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