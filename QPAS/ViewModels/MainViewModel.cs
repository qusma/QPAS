// -----------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using OxyPlot;
using QPAS.Scripting;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QPAS
{
    public class MainViewModel : ViewModelBase, IMainViewModel
    {
        public IDataSourcer Datasourcer { get; set; }
        public StatementHandler StatementHandler { get; set; }
        public ScriptRunner ScriptRunner { get; private set; }
        public PlotModel InstrumentsChartPlotModel { get; set; }

        //ViewModels for each individual page
        public CashTransactionsPageViewModel CashTransactionsPageViewModel { get; set; }
        public OpenPositionsPageViewModel OpenPositionsPageViewModel { get; set; }
        public InstrumentsPageViewModel InstrumentsPageViewModel { get; set; }
        public StrategiesPageViewModel StrategiesPageViewModel { get; set; }
        public TagsPageViewModel TagsPageViewModel { get; set; }
        public TradesPageViewModel TradesPageViewModel { get; set; }
        public OrdersPageViewModel OrdersPageViewModel { get; set; }
        public BenchmarksPageViewModel BenchmarksPageViewModel { get; set; }
        public PerformanceOverviewPageViewModel PerformanceOverviewPageViewModel { get; set; }
        public PerformanceReportPageViewModel PerformanceReportPageViewModel { get; set; }
        public FXTransactionsPageViewModel FXTransactionsPageViewModel { get; set; }

        public IAppSettings Settings { get; }

        public DataContainer Data { get; } = new DataContainer();

        private readonly TradesRepository _tradesRepository;
        private Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Keeps track of the ViewModel of the page the user is currently viewing.
        /// </summary>
        public ViewModelBase SelectedPageViewModel { get; set; }

        //Commands
        public ICommand GenerateReportFromStrategy { get; set; }
        public ICommand GenerateReportFromTags { get; set; }
        public ICommand GenerateReportFromTrades { get; set; }
        public ICommand LoadStatementFromWeb { get; set; }
        public ReactiveCommand<string, Unit> LoadStatementFromFile { get; set; }

        private readonly IContextFactory _contextFactory;

        public MainViewModel(IContextFactory contextFactory, IDataSourcer datasourcer, IDialogCoordinator dialogService, IAppSettings settings, DataContainer data)
            : base(dialogService)
        {
            Datasourcer = datasourcer;
            Settings = settings;
            Data = data;
            _tradesRepository = new TradesRepository(contextFactory, datasourcer, settings);

            StatementHandler = new StatementHandler(
                dialogService,
                contextFactory,
                settings,
                this);

            this._contextFactory = contextFactory;
            ScriptRunner = new ScriptRunner(contextFactory, _tradesRepository, data);

            CreateCommands();


            CreateSubViewModels();

            SelectedPageViewModel = OpenPositionsPageViewModel;
        }

        public async Task RefreshCurrentPage()
        {
            if (SelectedPageViewModel != null)
                await SelectedPageViewModel.Refresh().ConfigureAwait(true);
        }

        private void CreateSubViewModels()
        {
            CashTransactionsPageViewModel = new CashTransactionsPageViewModel(_contextFactory, Datasourcer, DialogService, Data.CashTransactions, this);
            OpenPositionsPageViewModel = new OpenPositionsPageViewModel(_contextFactory, DialogService);
            InstrumentsPageViewModel = new InstrumentsPageViewModel(_contextFactory, DialogService, Datasourcer, Data, this);
            StrategiesPageViewModel = new StrategiesPageViewModel(_contextFactory, DialogService, Data, this);
            TagsPageViewModel = new TagsPageViewModel(_contextFactory, DialogService, Data, this);
            TradesPageViewModel = new TradesPageViewModel(_contextFactory, DialogService, Datasourcer, Settings, Data, this);
            BenchmarksPageViewModel = new BenchmarksPageViewModel(_contextFactory, DialogService, Datasourcer, Data, this);
            PerformanceOverviewPageViewModel = new PerformanceOverviewPageViewModel(_contextFactory, DialogService, Settings, Data);
            OrdersPageViewModel = new OrdersPageViewModel(_contextFactory, DialogService, Datasourcer, Settings, Data, ScriptRunner, this);
            PerformanceReportPageViewModel = new PerformanceReportPageViewModel(_contextFactory, DialogService, Datasourcer, Data, this);
            FXTransactionsPageViewModel = new FXTransactionsPageViewModel(_contextFactory, Datasourcer, DialogService, Settings, Data, this);
        }

        private void CreateCommands()
        {
            GenerateReportFromStrategy = new RelayCommand<IList>(GenReportFromStrategy);
            GenerateReportFromTags = new RelayCommand<IList>(GenReportFromTags);
            GenerateReportFromTrades = new RelayCommand<IList>(GenReportFromTrades);

            LoadStatementFromWeb = ReactiveCommand.CreateFromTask<string>(async x =>
            {
                var progressDialog = await DialogService.ShowProgressAsync(this, "Load Statement from Web", "Downloading").ConfigureAwait(false);
                var newData = await StatementHandler.LoadFromWeb(x, progressDialog).ConfigureAwait(true);
                if (newData == null)
                {
                    await progressDialog.CloseAsync().ConfigureAwait(true);
                    return;
                }

                await PostStatementLoadProcedures(newData, progressDialog).ConfigureAwait(true);

            });
            LoadStatementFromFile = ReactiveCommand.CreateFromTask<string>(async x =>
            {
                var progressDialog = await DialogService.ShowProgressAsync(this, "Load Statement from File", "Opening File");
                var newData = await StatementHandler.LoadFromFile(x, progressDialog).ConfigureAwait(true);
                if (newData == null)
                {
                    await progressDialog.CloseAsync().ConfigureAwait(true);
                    return;
                }

                await PostStatementLoadProcedures(newData, progressDialog).ConfigureAwait(true);
            });

            LoadStatementFromFile.ThrownExceptions.Subscribe(ex =>
            {
                _logger.Error(ex, "Error on file load");
                DialogService.ShowMessageAsync(this, "Error", ex.Message);
            });
        }

        /// <summary>
        /// Stuff that needs to be done after loading data from a statement.
        /// </summary>
        private async Task PostStatementLoadProcedures(Dictionary<string, DataContainer> newData, ProgressDialogController progressDialog)
        {
            progressDialog.SetProgress(0);
            progressDialog.SetTitle("Importing data");
            progressDialog.SetMessage("Importing data");
            try
            {
                foreach (var kvp in newData)
                {
                    await DataImporter.Import(Data, kvp.Value, kvp.Key, _contextFactory, _tradesRepository);
                }
            }
            catch (Exception ex)
            {
                await progressDialog.CloseAsync().ConfigureAwait(true);
                await DialogService.ShowMessageAsync(this, "Data Import Error", ex.Message);
                _logger.Error(ex, "Data import exception");

                return;
            }

            progressDialog.SetProgress(0);
            progressDialog.SetTitle("Running scripts");
            await RunOrderScripts(Data.Orders.Where(y => y.Trade == null).OrderBy(y => y.TradeDate).ToList(), progressDialog);
            if (!progressDialog.IsOpen)
            {
                //might have been closed in case of error
                progressDialog = await DialogService.ShowProgressAsync(this, "Running scripts", "");
            }
            await RunTradeScripts(progressDialog).ConfigureAwait(true);

            await progressDialog.CloseAsync().ConfigureAwait(true);

            await RefreshCurrentPage().ConfigureAwait(true);
        }

        private async Task RunOrderScripts(List<Order> orders, ProgressDialogController progressDialog)
        {
            List<UserScript> scripts;
            using (var dbContext = _contextFactory.Get())
            {
                scripts = dbContext.UserScripts.Where(x => x.Type == UserScriptType.OrderScript).ToList();
            }

            for (int i = 0; i < scripts.Count; i++)
            {
                progressDialog.SetProgress((double)i / scripts.Count);
                progressDialog.SetMessage("Running script: " + scripts[i].Name);
                try
                {
                    await ScriptRunner.RunOrderScript(scripts[i], orders).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "User script {0} generated an exception: ", scripts[i].Name);
                    _logger.Log(LogLevel.Error, ex);
                    await progressDialog.CloseAsync();
                    await DialogService.ShowMessageAsync(this, "Error", $"User script {scripts[i].Name} generated an exception. See log for more details.");
                    progressDialog = await DialogService.ShowProgressAsync(this, "Running scripts", "");
                }
            }
        }

        private async Task RunTradeScripts(ProgressDialogController progressDialog)
        {
            List<UserScript> scripts;
            using (var dbContext = _contextFactory.Get())
            {
                scripts = dbContext.UserScripts.Where(x => x.Type == UserScriptType.TradeScript).ToList();
            }

            for (int i = 0; i < scripts.Count; i++)
            {
                progressDialog.SetProgress((double)i / scripts.Count);
                progressDialog.SetMessage("Running script: " + scripts[i].Name);

                try
                {
                    await ScriptRunner.RunTradeScript(scripts[i]).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "User script {0} generated an exception: ", scripts[i].Name);
                    _logger.Log(LogLevel.Error, ex);
                    await progressDialog.CloseAsync();
                    await DialogService.ShowMessageAsync(this, "Error", $"User script {scripts[i].Name} generated an exception. See log for more details.");
                    progressDialog = await DialogService.ShowProgressAsync(this, "Running scripts", "");
                }
            }
        }

        private void GenReportFromStrategy(IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;
            var selectedStrategies = selectedItems.Cast<Strategy>().ToList();
            var tradeIDs = TradeFiltering.FilterByStrategies(selectedStrategies, Data.Trades);
            GenerateReport(tradeIDs);
        }

        private void GenReportFromTags(IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;
            var selectedTags = selectedItems.Cast<Tag>().ToList();
            var selectedTrades = TradeFiltering.FilterByTags(selectedTags, Data.Trades);
            GenerateReport(selectedTrades);
        }

        private void GenReportFromTrades(IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;
            var selectedTrades = selectedItems.Cast<Trade>().ToList();
            GenerateReport(selectedTrades);
        }

        private async void GenerateReport(List<Trade> tradeIDs)
        {
            if (tradeIDs == null) throw new NullReferenceException("tradeIDs");
            if (tradeIDs.Count == 0)
            {
                await DialogService.ShowMessageAsync(this, "Error", "No trades meet the given criteria").ConfigureAwait(true);
                return;
            }

            var gen = new ReportGenerator();
            ProgressDialogController progressDialog = await DialogService.ShowProgressAsync(this, "Generating Report", "Generating Report").ConfigureAwait(true);
            var ds = await Task.Run(() => gen.TradeStats(
                tradeIDs, 
                PerformanceReportPageViewModel.ReportSettings, 
                Settings, 
                Datasourcer, 
                _contextFactory, 
                backtestData: PerformanceReportPageViewModel.BacktestData, 
                progressDialog: progressDialog)).ConfigureAwait(true);
            progressDialog.CloseAsync().Forget(); //don't await it!

            var window = new PerformanceReportWindow(ds, PerformanceReportPageViewModel.ReportSettings);
            window.Show();
        }
    }
}