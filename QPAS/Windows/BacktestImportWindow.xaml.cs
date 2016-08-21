// -----------------------------------------------------------------------
// <copyright file="BacktestImportWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for BacktestImportWindow.xaml
    /// </summary>
    public partial class BacktestImportWindow : MetroWindow
    {
        public BacktestImportViewModel ViewModel { get; private set; }

        public bool Canceled { get; private set; }

        public BacktestImportWindow()
        {
            ViewModel = new BacktestImportViewModel(DialogCoordinator.Instance);
            DataContext = ViewModel;
            InitializeComponent();
            Canceled = true;
        }

        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://qusma.com/qpasdocs/index.php/Importing_Backtest_Data"));
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Import())
            {
                Canceled = false;
                Close();
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
