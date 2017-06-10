// -----------------------------------------------------------------------
// <copyright file="BenchmarksPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ReactiveUI;

namespace QPAS
{
    public class BenchmarksPageViewModel : ViewModelBase
    {
        private bool _isExternalClientConnected;

        public CollectionViewSource BenchmarksSource { get; set; }

        internal IDataSourcer Datasourcer;
        private readonly IMainViewModel _mainVm;
        internal IDBContext Context;

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

        public BenchmarksPageViewModel(IDBContext context, IDialogCoordinator dialogService, IDataSourcer datasourcer, IMainViewModel mainVm)
            : base(dialogService)
        {
            Context = context;
            Datasourcer = datasourcer;
            _mainVm = mainVm;

            ExternalInstruments = new ObservableCollection<KeyValuePair<string, int?>>();

            BenchmarksSource = new CollectionViewSource();
            BenchmarksSource.Source = Context.Benchmarks.Local;
            Context.Tags.Load();

            CreateCommands();
        }

        private void CreateCommands()
        {
            DeleteBenchmark = ReactiveCommand.CreateFromTask(async _ => await DeleteBench(SelectedBenchmark).ConfigureAwait(true));
            DeleteComponent = ReactiveCommand.CreateFromTask(async _ => await DeleteComp(SelectedComponent).ConfigureAwait(true));
        }

        public override async Task Refresh()
        {
            Context.Benchmarks.Include(x => x.Components).Load();

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

            Context.BenchmarkComponents.Remove(component);
            await Context.SaveChangesAsync().ConfigureAwait(true);
        }

        private async Task DeleteBench(Benchmark benchmark)
        {
            if (benchmark == null) return;

            var result = await DialogService.ShowMessageAsync(_mainVm,
                "Delete Benchmark",
                string.Format("Are you sure you want to delete {0}?", benchmark.Name),
                MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Affirmative)
            {
                Context.Benchmarks.Remove(benchmark);
                await Context.SaveChangesAsync().ConfigureAwait(true);
            }
        }
    }
}