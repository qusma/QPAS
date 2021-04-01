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
                await SelectedPageViewModel.Refresh();
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
                var newData = await StatementHandler.LoadFromWeb(x, progressDialog);
                if (newData == null)
                {
                    await progressDialog.CloseAsync();
                    return;
                }

                await PostStatementLoadProcedures(newData, progressDialog);
            });
            LoadStatementFromFile = ReactiveCommand.CreateFromTask<string>(async x =>
            {
                var progressDialog = await DialogService.ShowProgressAsync(this, "Load Statement from File", "Opening File");
                var newData = await StatementHandler.LoadFromFile(x, progressDialog);
                if (newData == null)
                {
                    await progressDialog.CloseAsync();
                    return;
                }

                await PostStatementLoadProcedures(newData, progressDialog);
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

            //backup db before import
            System.IO.File.Copy("qpas.db", "qpas-backup.db", true);

            //prevent gaps in data
            bool continueImport = await ImportDateCheck(newData, progressDialog);
            if (!continueImport) return;

            //Perform the data import
            try
            {
                foreach (var kvp in newData)
                {
                    await DataImporter.Import(Data, kvp.Value, kvp.Key, _contextFactory, _tradesRepository);
                }
            }
            catch (Exception ex)
            {
                await progressDialog.CloseAsync();
                await DialogService.ShowMessageAsync(this, "Data Import Error", ex.Message);
                _logger.Error(ex, "Data import exception");

                return;
            }

            //Run scripts
            progressDialog.SetProgress(0);
            progressDialog.SetTitle("Running scripts");

            await RunOrderScripts(Data.Orders.Where(y => y.Trade == null).OrderBy(y => y.TradeDate).ToList(), progressDialog);
            await RunTradeScripts(progressDialog);

            await progressDialog.CloseAsync();

            await RefreshCurrentPage();
        }

        /// <summary>
        /// warn if there are missing business days between the last data in the db and first data in the import
        /// </summary>
        /// <param name="newData"></param>
        /// <param name="progressDialog"></param>
        /// <returns>false to abort import</returns>
        private async Task<bool> ImportDateCheck(Dictionary<string, DataContainer> newData, ProgressDialogController progressDialog)
        {
            DateTime? lastDateInDb;
            using (var dbContext = _contextFactory.Get())
            {
                lastDateInDb = dbContext.FXRates.OrderByDescending(x => x.Date).FirstOrDefault()?.Date;
            }

            var fxRateDateEarliestDates = newData.Select(x => x.Value.FXRates.OrderBy(x => x.Date).FirstOrDefault()).Where(x => x != null).ToList();
            DateTime? firstDateInImport = fxRateDateEarliestDates.Count > 0 ? fxRateDateEarliestDates.OrderBy(x => x.Date).First().Date : (DateTime?)null;

            if (lastDateInDb.HasValue && firstDateInImport.HasValue && lastDateInDb.Value < firstDateInImport.Value &&
                Utils.CountBusinessDaysBetween(lastDateInDb.Value, firstDateInImport.Value) > 0)
            {
                var firstMissingBusinessDay = Utils.AddBusinessDays(lastDateInDb.Value, 1);

                var result = await DialogService.ShowMessageAsync(this, "Potential Import Mistake Warning",
                    "There are missing business days between the last data in the db and the first data in the file you are importing. " +
                    $"It is recommended you Cancel the import and load a flex statement starting from {firstMissingBusinessDay:d}.\n\nDo you want to proceed anyway?",
                    MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Negative)
                {
                    await progressDialog.CloseAsync();
                    return false;
                }
            }

            return true;
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
                    await ScriptRunner.RunOrderScript(scripts[i], orders);//todo: this will run subsequent scripts with orders set to a trade...
                    orders = orders.Where(y => y.Trade == null).ToList();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "User script {0} generated an exception: ", scripts[i].Name);
                    await DialogService.ShowMessageAsync(this, "Error", $"User script {scripts[i].Name} generated an exception. See log for more details.");
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
                    _logger.Error(ex, "User script {0} generated an exception: ", scripts[i].Name);
                    await DialogService.ShowMessageAsync(this, "Error", $"User script {scripts[i].Name} generated an exception. See log for more details.");
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
                await DialogService.ShowMessageAsync(this, "Error", "No trades meet the given criteria");
                return;
            }

            var gen = new ReportGenerator();
            ProgressDialogController progressDialog = await DialogService.ShowProgressAsync(this, "Generating Report", "Generating Report");
            var ds = await Task.Run(() => gen.TradeStats(
                tradeIDs,
                PerformanceReportPageViewModel.ReportSettings,
                Settings,
                Datasourcer,
                _contextFactory,
                backtestData: PerformanceReportPageViewModel.BacktestData,
                progressDialog: progressDialog));
            progressDialog.CloseAsync().Forget(); //don't await it!

            var window = new PerformanceReportWindow(ds, PerformanceReportPageViewModel.ReportSettings);
            window.Show();
        }
    }

    public class Order_Script : OrderScriptBase
    {
        private List<string> _symbols = new List<string> { "SPY", "QQQ", "IWM" };

        //Do not change the constructor parameters.
        public Order_Script(DataContainer data, ILogger logger) : base(data, logger)
        {
        }

        public override void ProcessOrders(List<Order> orders)
        {
            //cycle through orders that match our symbol list
            foreach (var order in orders.Where(x => _symbols.Contains(x.Instrument.UnderlyingSymbol) && x.OrderReference.Contains("ETF-Swing")))
            {
                string orderInstrument = order.Instrument.UnderlyingSymbol;

                //look for an open trade that fits the pattern, was opened before this orders, and uses the same instrument
                Trade trade = OpenTrades
                    .Where(x => x.Name.StartsWith("ETF-Swing") &&
                                x.DateOpened < order.TradeDate &&
                                x.Orders.Any(o => o.Instrument.UnderlyingSymbol == orderInstrument))
                    .OrderByDescending(x => x.DateOpened)
                    .FirstOrDefault();

                if (trade != null)
                {
                    //if such a trade exists, add this order to it
                    SetTrade(order, trade);
                }
                else
                {
                    //if the trade does not exist, create it
                    var side = order.BuySell == "BUY" ? "Long" : "Short";
                    trade = CreateTrade($"ETF-Swing {orderInstrument} {side} {order.TradeDate:yyyy--MM-dd}");
                    SetTrade(order, trade);

                    Log($"Created new trade for {orderInstrument}");
                }
            }
        }
    }
}