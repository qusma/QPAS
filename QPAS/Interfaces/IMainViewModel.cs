using OxyPlot;
using QPAS.Scripting;
using ReactiveUI;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QPAS
{
    public interface IMainViewModel
    {
        IDataSourcer Datasourcer { get; set; }
        StatementHandler StatementHandler { get; set; }
        ScriptRunner ScriptRunner { get; }
        PlotModel InstrumentsChartPlotModel { get; set; }
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
        ReactiveCommand<string, Unit> LoadStatementFromFile { get; set; }

        Task RefreshCurrentPage();

        Task Refresh();

        event PropertyChangedEventHandler PropertyChanged;
    }
}