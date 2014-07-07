// -----------------------------------------------------------------------
// <copyright file="SettingsWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Forms;
using EntityModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using Application = System.Windows.Application;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        private readonly IDBContext _context;

        public SettingsViewModel ViewModel { get; set; }

        public SettingsWindow(IDBContext context)
        {
            _context = context;
            InitializeComponent();
            ViewModel = new SettingsViewModel(context);
            DataContext = ViewModel;

            //have to load the passwords here
            //because binding securely is basically impossible
            MySqlPasswordBox.Password = DBUtils.Unprotect(Properties.Settings.Default.mySqlPassword);
            SqlitePasswordBox.Password = DBUtils.Unprotect(Properties.Settings.Default.sqlitePassword);
            SqlServerPassword.Password = DBUtils.Unprotect(Properties.Settings.Default.sqlServerPassword);

            //hiding the tab headers
            Style s = new Style();
            s.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
            DbSettingsTabCtrl.ItemContainerStyle = s;
        }

        private void FlexSavePathTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (!String.IsNullOrEmpty(ViewModel.StatementSaveLocation))
            {
                dialog.SelectedPath = ViewModel.StatementSaveLocation;
            }

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.StatementSaveLocation = dialog.SelectedPath;
            }
        }

        private void LogFilePathTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (!String.IsNullOrEmpty(ViewModel.LogLocation))
            {
                dialog.SelectedPath = ViewModel.LogLocation;
            }

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.LogLocation = dialog.SelectedPath;
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //have to save the passwords here
            //because binding securely is basically impossible
            Properties.Settings.Default.mySqlPassword = DBUtils.Protect(MySqlPasswordBox.Password);
            Properties.Settings.Default.sqlitePassword = DBUtils.Protect(SqlitePasswordBox.Password);
            Properties.Settings.Default.sqlServerPassword = DBUtils.Protect(SqlServerPassword.Password);

            Properties.Settings.Default.Save();

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
                if (Properties.Settings.Default.databaseType.ToLower() == "mysql")
                {
                    _context.Database.ExecuteSqlCommand("DROP DATABASE qpas");
                }
                else
                {
                    //very hacky....
                    using (var sqlConnection = DBUtils.CreateSqlServerConnection("master"))
                    {
                        sqlConnection.Open();
                        SqlCommand sqlCmd = new SqlCommand("ALTER DATABASE qpas SET SINGLE_USER WITH ROLLBACK IMMEDIATE", sqlConnection);
                        sqlCmd.ExecuteNonQuery();

                        sqlCmd.CommandText = "DROP DATABASE qpas";
                        sqlCmd.ExecuteNonQuery();
                    }
                }
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
            if((string)btn.Content == "MySQL")
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
