// -----------------------------------------------------------------------
// <copyright file="ExecutionReportWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for ExecutionReportWindow.xaml
    /// </summary>
    public partial class ExecutionReportWindow : MetroWindow
    {
        public ExecutionReportViewModel ViewModel { get; set; }
        public ExecutionReportWindow(ExecutionStatsGenerator statsGen)
        {
            var dialogService = new DialogService(this);
            InitializeComponent();
            ViewModel = new ExecutionReportViewModel(statsGen, dialogService);
            DataContext = ViewModel;
        }

        private void GenerateExecutionReportBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RunAnalysis.Execute(null);
            TabCtrl.SelectedIndex = 1;
        }
    }
}
