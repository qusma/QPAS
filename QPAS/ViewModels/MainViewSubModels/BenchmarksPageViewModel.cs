// -----------------------------------------------------------------------
// <copyright file="BenchmarksPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class BenchmarksPageViewModel : ViewModelBase
    {
        private bool _isExternalClientConnected;

        public CollectionViewSource BenchmarksSource { get; set; }

        private readonly IContextFactory _contextFactory;
        internal IDataSourcer Datasourcer;
        private readonly DataContainer _data;
        private readonly IMainViewModel _mainVm;

        public Benchmark SelectedBenchmark { get; set; }

        public BenchmarkComponent SelectedComponent { get; set; }

        public ObservableCollection<KeyValuePair<string, int?>> ExternalInstruments { get; set; }

        public ICommand DeleteBenchmark { get; set; }

        public ICommand DeleteComponent { get; set; }

        public bool IsExternalClientConnected
        {
            get => _isExternalClientConnected;
            set => this.RaiseAndSetIfChanged(ref _isExternalClientConnected, value);
        }

        public BenchmarksPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, IDataSourcer datasourcer, DataContainer data, IMainViewModel mainVm)
            : base(dialogService)
        {
            _contextFactory = contextFactory;
            Datasourcer = datasourcer;
            _data = data;
            _mainVm = mainVm;

            ExternalInstruments = new ObservableCollection<KeyValuePair<string, int?>>();

            BenchmarksSource = new CollectionViewSource();
            BenchmarksSource.Source = data.Benchmarks;

            CreateCommands();
        }

        private void CreateCommands()
        {
            DeleteBenchmark = ReactiveCommand.CreateFromTask(async _ => await DeleteBench(SelectedBenchmark).ConfigureAwait(true));
            DeleteComponent = ReactiveCommand.CreateFromTask(async _ => await DeleteComp(SelectedComponent).ConfigureAwait(true));
        }

        public override async Task Refresh()
        {
            await PopulateQDMSInstruments().ConfigureAwait(true);

            IsExternalClientConnected = Datasourcer.ExternalDataSource != null && Datasourcer.ExternalDataSource.Connected;
        }

        private async Task PopulateQDMSInstruments()
        {
            //fill qdms instrument combobox
            if (Datasourcer.ExternalDataSource != null && Datasourcer.ExternalDataSource.Connected)
            {
                ExternalInstruments.Clear();
                ExternalInstruments.Add(new KeyValuePair<string, int?>("Auto", null));

                foreach (var kvp in await Datasourcer.ExternalDataSource.GetInstrumentDict().ConfigureAwait(true))
                {
                    ExternalInstruments.Add(new KeyValuePair<string, int?>(kvp.Key, kvp.Value));
                }
            }
        }

        private async Task DeleteComp(BenchmarkComponent component)
        {
            if (component == null) return;

            using (var dbContext = _contextFactory.Get())
            {
                dbContext.BenchmarkComponents.Remove(component);
                await dbContext.SaveChangesAsync().ConfigureAwait(true);
            }
        }

        private async Task DeleteBench(Benchmark benchmark)
        {
            if (benchmark == null) return;

            var result = await DialogService.ShowMessageAsync(_mainVm,
                "Delete Benchmark",
                string.Format("Are you sure you want to delete {0}?", benchmark.Name),
                MessageDialogStyle.AffirmativeAndNegative);

            if (result != MessageDialogResult.Affirmative) return;


            using (var dbContext = _contextFactory.Get())
            {
                dbContext.Benchmarks.Remove(benchmark);
                await dbContext.SaveChangesAsync().ConfigureAwait(true);
                _data.Benchmarks.Remove(benchmark);
            }
        }
    }
}