// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Reflection;
using EntityModel;
using MahApps.Metro.Controls;
using NLog;

namespace QPAS.UserScriptEditor
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public ScriptingViewModel ViewModel { get; set; }

        public MainWindow()
        {
            string[] args = Environment.GetCommandLineArgs();
            if(args.Length < 3)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Log(LogLevel.Error, "Tried to launch script editor without providing connection string details.");
                Close();
                return;
            }

            SetConnectingString(args[1], args[2]);

            var context = new DBContext();

            InitializeComponent();
            
            ViewModel = new ScriptingViewModel(context, new DialogService(this));
            DataContext = ViewModel;
        }

        private void SetConnectingString(string connString, string provider)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings conSettings = config.ConnectionStrings.ConnectionStrings["qpasEntities"];

            //this is an extremely dirty hack that allows us to change the connection string at runtime
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi.SetValue(conSettings, false);

            conSettings.ConnectionString = connString;
            conSettings.ProviderName = provider;

            config.Save();

            ConfigurationManager.RefreshSection("connectionStrings");
        }
    }
}
