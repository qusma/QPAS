using System.ComponentModel;
using System.Windows.Input;
using OxyPlot;
using QPAS.Scripting;

namespace QPAS
{
    public interface IMainViewModel
    {
        IDataSourcer Datasourcer { get; set; }
        StatementHandler StatementHandler { get; set; }
        ScriptRunner ScriptRunner { get; }
        PlotModel InstrumentsChartPlotModel { get; set; }
        ITradesRepository TradesRepository { get; }
        CashTransactionsPageViewModel CashTransactionsPageViewModel { get; set; }
        OpenPositionsPageViewModel OpenPositionsPageViewModel { get; set; }
        InstrumentsPageViewModel InstrumentsPageViewModel { get; set; }
        StrategiesPageViewModel StrategiesPageViewModel { get; set; }
        TagsPageViewModel TagsPageViewModel { get; set; }
        TradesPageViewModel TradesPageViewModel { get; set; }
        OrdersPageViewModel OrdersPageViewModel { get; set; }
        BenchmarksPageViewModel BenchmarksPageViewModel { get; set; }
        PerformanceOverviewPageViewModel PerformanceOverviewPageViewModel { get; set; }
        PerformanceReportPageViewModel PerformanceReportPageViewModel { get; set; }
        FXTransactionsPageViewModel FXTransactionsPageViewModel { get; set; }

        /// <summary>
        /// Keeps track of the ViewModel of the page the user is currently viewing.
        /// </summary>
        ViewModelBase SelectedPageViewModel { get; set; }

        ICommand GenerateReportFromStrategy { get; set; }
        ICommand GenerateReportFromTags { get; set; }
        ICommand GenerateReportFromTrades { get; set; }
        ICommand LoadStatementFromWeb { get; set; }
        ICommand LoadStatementFromFile { get; set; }
        void RefreshCurrentPage();
        void Refresh();
        event PropertyChangedEventHandler PropertyChanged;
    }
}