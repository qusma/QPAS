// -----------------------------------------------------------------------
// <copyright file="SettingsWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsViewModel ViewModel { get; set; }

        public SettingsWindow(IAppSettings settings, IContextFactory contextFactory)
        {
            InitializeComponent();
            ViewModel = new SettingsViewModel(settings, contextFactory);
            DataContext = ViewModel;

            //hiding the tab headers
            Style s = new Style();
            s.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
            DbSettingsTabCtrl.ItemContainerStyle = s;
        }

        private void FlexSavePathTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (!String.IsNullOrEmpty(ViewModel.Settings.StatementSaveLocation))
            {
                dialog.SelectedPath = ViewModel.Settings.StatementSaveLocation;
            }

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.Settings.StatementSaveLocation = dialog.SelectedPath;
            }
        }

        private void LogFilePathTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (!String.IsNullOrEmpty(ViewModel.Settings.LogLocation))
            {
                dialog.SelectedPath = ViewModel.Settings.LogLocation;
            }

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.Settings.LogLocation = dialog.SelectedPath;
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();

            await this.ShowMessageAsync("Restarting", "Settings saved. Restarting application.");

            System.Windows.Forms.Application.Restart();
            Environment.Exit(0);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Simply drops the database.
        /// </summary>
        private async void ClearDataBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult res = await this.ShowMessageAsync(
                "Are you sure?",
                "You are about to delete all the data, are you sure?",
                MessageDialogStyle.AffirmativeAndNegative);
            if (res == MessageDialogResult.Negative) return;

            MessageDialogResult res2 = await this.ShowMessageAsync(
                "Are you absolutely sure?",
                "Checking twice to be safe, are you sure you want to delete everything?",
                MessageDialogStyle.AffirmativeAndNegative);
            if (res2 == MessageDialogResult.Negative) return;

            try
            {
                var dbContext = new QpasDbContext();
                dbContext.Database.ExecuteSqlRaw(@"
                        PRAGMA writable_schema = 1;
                        delete from sqlite_master where type in ('table', 'index', 'trigger');
                        PRAGMA writable_schema = 0;
                        ");
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(
                    "Error",
                    "Could not clear data. Error: " + ex.Message).Forget();
                var logger = LogManager.GetCurrentClassLogger();
                logger.Log(LogLevel.Error, ex);
                return;
            }


            await this.ShowMessageAsync(
                "Done",
                "Data deleted. The application will now shut down.");
            Application.Current.Shutdown();
        }

        private void DbRadioBtnCheckChange(object sender, RoutedEventArgs e)
        {
            var btn = (System.Windows.Controls.RadioButton)sender;
            if ((string)btn.Content == "MySQL")
            {
                DbSettingsTabCtrl.SelectedIndex = 0;
            }
            else if ((string)btn.Content == "SQL Server")
            {
                DbSettingsTabCtrl.SelectedIndex = 1;
            }
            else
            {
                DbSettingsTabCtrl.SelectedIndex = 2;
            }
        }
    }
}
