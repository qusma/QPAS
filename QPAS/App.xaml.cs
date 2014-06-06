// -----------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
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


        protected override void OnStartup(StartupEventArgs e)
        {
            // ManualResetEvent acts as a block. It waits for a signal to be set.
            ResetSplashCreated = new ManualResetEvent(false);

            // Create a new thread for the splash screen to run on
            SplashThread = new Thread(ShowSplash);
            SplashThread.SetApartmentState(ApartmentState.STA);
            SplashThread.IsBackground = true;
            SplashThread.Name = "QPAS Splash Screen";
            SplashThread.Start();

            // Wait for the blocker to be signaled before continuing. This is essentially the same as: while(ResetSplashCreated.NotSet) {}
            ResetSplashCreated.WaitOne();
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
