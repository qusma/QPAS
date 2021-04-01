// -----------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Targets;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SplashScreen Splash;

        private ManualResetEvent ResetSplashCreated;
        private Thread SplashThread;

        public App()
            : base()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            FrameworkElement.LanguageProperty.OverrideMetadata(
              typeof(FrameworkElement),
              new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            QuickConverter.EquationTokenizer.AddNamespace(typeof(object));
            QuickConverter.EquationTokenizer.AddNamespace(typeof(System.Windows.Visibility));
            QuickConverter.EquationTokenizer.AddNamespace(typeof(Brush));
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            //set up sqlite
            SQLitePCL.Batteries.Init();

            //Load settings
            var settings = SettingsUtils.LoadSettings();

            //initialize logging
            InitializeLogging(settings);

            //Log unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;

            //db
            var contextFactory = new DbContextFactory(() => new QpasDbContext());

            InitializeDb(contextFactory);

            //check for empty account fields and load preferences
            using (var dbContext = contextFactory.Get())
            {
                if (dbContext.EquitySummaries.Any(x => x.AccountID == null))
                {
                    App.Splash.LoadComplete();
                    var accountMigrationWindow = new AccountMigrationWindow();
                    accountMigrationWindow.ShowDialog();
                }
            }

            var data = await DataLoader.LoadData(contextFactory);

            var qdmsSource = new ExternalDataSources.QDMS(settings, data.DatasourcePreferences.ToList());
            var ds = new DataSourcer(contextFactory, qdmsSource, data, settings.AllowExternalDataSource);

            var window = new MainWindow(data, settings, contextFactory, ds);
        }

        private void InitializeDb(IContextFactory contextFactory)
        {
            using (var dbContext = contextFactory.Get())
            {
                //create db if it doesn't exist
                dbContext.Database.Migrate();

                //seed the db with initial values if nothing is found
                Seed.DoSeed(dbContext);
            }
        }

        private void InitializeLogging(IAppSettings settings)
        {
            if (String.IsNullOrEmpty(settings.LogLocation))
            {
                LogManager.Configuration.LoggingRules.Remove(LogManager.Configuration.LoggingRules[0]);
            }
            else
            {
                var target = (FileTarget)LogManager.Configuration.FindTargetByName("default");
                target.FileName = string.Format("{0}/{1}", settings.LogLocation, "qpaslog.log");
                target.ArchiveFileName = string.Format("{0}/{1}", settings.LogLocation, @"${shortdate}.{##}.log");
#if DEBUG
                var rule = LogManager.Configuration.LoggingRules[0];
                rule.EnableLoggingForLevel(LogLevel.Trace);
#endif
            }
            LogManager.Configuration.Reload();
            LogManager.ReconfigExistingLoggers();
        }

        private void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Error((Exception)e.ExceptionObject, "Unhandled exception");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //// ManualResetEvent acts as a block. It waits for a signal to be set.
            //ResetSplashCreated = new ManualResetEvent(false);

            //// Create a new thread for the splash screen to run on
            //SplashThread = new Thread(ShowSplash);
            //SplashThread.SetApartmentState(ApartmentState.STA);
            //SplashThread.IsBackground = true;
            //SplashThread.Name = "QPAS Splash Screen";
            //SplashThread.Start();

            //// Wait for the blocker to be signaled before continuing. This is essentially the same as: while(ResetSplashCreated.NotSet) {}
            //ResetSplashCreated.WaitOne();
            base.OnStartup(e);
        }

        private void ShowSplash()
        {
            // Create the window
            Splash = new SplashScreen();

            // Show it
            Splash.Show();

            // Now that the window is created, allow the rest of the startup to run
            ResetSplashCreated.Set();
            System.Windows.Threading.Dispatcher.Run();
        }
    }
}