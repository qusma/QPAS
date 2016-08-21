// -----------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using OxyPlot;
using QPAS.Scripting;

namespace QPAS
{
    public class MainViewModel : ViewModelBase
    {
        internal IDBContext Context;

        public IDataSourcer Datasourcer { get; set; }
        public StatementHandler StatementHandler { get; set; }
        public ScriptRunner ScriptRunner { get; private set; }
        public PlotModel InstrumentsChartPlotModel { get; set; }
        public ITradesRepository TradesRepository { get; private set; }
        
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
        
        /// <summary>
        /// Keeps track of the ViewModel of the page the user is currently viewing.
        /// </summary>
        public ViewModelBase SelectedPageViewModel { get; set; }

        //Commands
        public ICommand GenerateReportFromStrategy { get; set; }
        public ICommand GenerateReportFromTags { get; set; }
        public ICommand GenerateReportFromTrades { get; set; }
        public ICommand LoadStatementFromWeb { get; set; }
        public ICommand LoadStatementFromFile { get; set; }

        public MainViewModel(IDBContext context, IDataSourcer datasourcer, IDialogService dialogService)
            : base(dialogService)
        {
            Context = context;
            Datasourcer = datasourcer;
            TradesRepository = new TradesRepository(context, datasourcer, Properties.Settings.Default.optionsCapitalUsageMultiplier);

            StatementHandler = new StatementHandler(
                context,
                dialogService,
                datasourcer,
                TradesRepository);

            CreateSubViewModels();

            SelectedPageViewModel = OpenPositionsPageViewModel;

            CreateCommands();

            ScriptRunner = new ScriptRunner(TradesRepository);
        }

        public void RefreshCurrentPage()
        {
            if(SelectedPageViewModel != null)
                SelectedPageViewModel.Refresh();
        }

        private void CreateSubViewModels()
        {
            CashTransactionsPageViewModel = new CashTransactionsPageViewModel(Context, Datasourcer, DialogService, this);
            OpenPositionsPageViewModel = new OpenPositionsPageViewModel(Context, DialogService);
            InstrumentsPageViewModel = new InstrumentsPageViewModel(Context, DialogService, Datasourcer);
            StrategiesPageViewModel = new StrategiesPageViewModel(Context, DialogService, this);
            TagsPageViewModel = new TagsPageViewModel(Context, DialogService, this);
            TradesPageViewModel = new TradesPageViewModel(Context, DialogService, Datasourcer, this);
            BenchmarksPageViewModel = new BenchmarksPageViewModel(Context, DialogService, Datasourcer);
            PerformanceOverviewPageViewModel = new PerformanceOverviewPageViewModel(Context, DialogService);
            OrdersPageViewModel = new OrdersPageViewModel(Context, DialogService, Datasourcer, this);
            PerformanceReportPageViewModel = new PerformanceReportPageViewModel(Context, DialogService, this, Datasourcer);
            FXTransactionsPageViewModel = new FXTransactionsPageViewModel(Context, Datasourcer, DialogService, this);
        }

        private void CreateCommands()
        {
            GenerateReportFromStrategy = new RelayCommand<IList>(GenReportFromStrategy);
            GenerateReportFromTags = new RelayCommand<IList>(GenReportFromTags);
            GenerateReportFromTrades = new RelayCommand<IList>(GenReportFromTrades);

            LoadStatementFromWeb = new RelayCommand<string>(async x =>
            { 
                await StatementHandler.LoadFromWeb(x);
                PostStatementLoadProcedures();

            });
            LoadStatementFromFile = new RelayCommand<string>(async x => 
            {
                await StatementHandler.LoadFromFile(x);
                PostStatementLoadProcedures();
            });
        }

        /// <summary>
        /// Stuff that needs to be done after loading data from a statement.
        /// </summary>
        private void PostStatementLoadProcedures()
        {
            RefreshCurrentPage();
            ScriptRunner.RunOrderScripts(Context.Orders.Where(y => y.Trade == null).OrderBy(y => y.TradeDate).ToList(), Context);
            ScriptRunner.RunTradeScripts(Context.Trades.Where(y => y.Open).ToList(), Context.Strategies.ToList(), Context.Tags.ToList(), Context);
        }

        private void GenReportFromStrategy(IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;
            var selectedStrategies = selectedItems.Cast<Strategy>().ToList();
            var tradeIDs = TradeFiltering.FilterByStrategies(selectedStrategies, Context);
            GenerateReport(tradeIDs);
        }

        private void GenReportFromTags(IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;
            var selectedTags = selectedItems.Cast<Tag>().ToList();
            var selectedTrades = TradeFiltering.FilterByTags(selectedTags, Context);
            GenerateReport(selectedTrades);
        }

        private void GenReportFromTrades(IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;
            var selectedTrades = selectedItems.Cast<Trade>().Select(x => x.ID).ToList();
            GenerateReport(selectedTrades);
        }

        private async void GenerateReport(List<int> tradeIDs)
        {
            if (tradeIDs == null) throw new NullReferenceException("tradeIDs");
            if (tradeIDs.Count == 0)
            {
                await DialogService.ShowMessageAsync("Error", "No trades meet the given criteria");
                return;
            }

            var gen = new ReportGenerator();
            ProgressDialogController progressDialog = await DialogService.ShowProgressAsync("Generating Report", "Generating Report");
            var ds = await Task.Run(() => gen.TradeStats(tradeIDs, PerformanceReportPageViewModel.ReportSettings, Datasourcer, progressDialog));
            progressDialog.CloseAsync().Forget(); //don't await it!

            var window = new PerformanceReportWindow(ds, PerformanceReportPageViewModel.ReportSettings);
            window.Show();
        }
    }
}