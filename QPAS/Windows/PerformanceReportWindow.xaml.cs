// -----------------------------------------------------------------------
// <copyright file="PerformanceReportWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using QPAS.DataSets;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for PortfolioReportWindow.xaml
    /// </summary>
    public partial class PerformanceReportWindow : MetroWindow
    {
        public PerformanceReportViewModel ViewModel { get; set; }

        internal IDialogService DialogService { get; set; }

        public PerformanceReportWindow(filterReportDS data, ReportSettings settings)
        {
            InitializeComponent();

            //hiding the tab headers
            Style s = new Style();
            s.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
            MainTabCtrl.ItemContainerStyle = s;
            DialogService = new DialogService(this);

            ViewModel = new PerformanceReportViewModel(data, settings, DialogService);
            DataContext = ViewModel;

            //give instrument pnl chart 30 pixels height for every bar, there's no better way of fitting it to the contents
            RealizedPLByInstrumentChart.Height = 50 + ViewModel.Data.pnlByInstrument.Rows.Count * 30;
            ROACByInstrumentChart.Height = 50 +  ViewModel.Data.instrumentROAC.Rows.Count * 30;
            TotalPLByTagChart.Height = 50 + ViewModel.Data.PLByTag.Rows.Count * 30;
            AvgPLByTagChart.Height = 50 + ViewModel.Data.PLByTag.Rows.Count * 30;
        }

        private void BtnExit_ItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void NavigationMenu_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (NavigationMenu.SelectedItem == null) return;

            var pages = new List<string>
            {
                "CumulativePL",
                "CumulativeReturns",
                "CapitalUsage",
                "PerTradeStats",
                "PortfolioStats",
                "PLByMonth",
                "PnLByTags",
                "StrategyCorrelations",
                "Benchmarking",
                "TradeRetDistributions",
                "MAEMFE",
                "AvgCumulativeRets",
                "DailyRetDistributions",
                "TradeLengths",
                "LengthVsReturns",
                "SizingVsReturns",
                "RealizedPLByInstrument",
                "ROACByInstrument",
                "GrossMovementCapture",
                "CashTransactions",
                "ACFPACF",
                "Risk",
                "MonteCarlo"
            };
            var title = (string)((TreeViewItem)NavigationMenu.SelectedItem).Tag;
            MainTabCtrl.SelectedIndex = pages.IndexOf(title);
        }

        private void TradeStatsByStrategyGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if ((string) e.Column.Header == "stat") e.Cancel = true;
        }

        private void StrategyCorrelationsGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if((string)e.Column.Header == "Name")
            {
                e.Cancel = true;
            }
            else
            {
                ((DataGridTextColumn)e.Column).Binding.StringFormat = "0.00";
            }
        }
    }
}
