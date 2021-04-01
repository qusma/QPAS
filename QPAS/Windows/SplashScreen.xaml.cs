// -----------------------------------------------------------------------
// <copyright file="SplashScreen.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using System.Windows;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : MetroWindow
    {
        public SplashScreen()
        {
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            Topmost = true;

            InitializeComponent();
        }

        public void LoadComplete()
        {
            Dispatcher.InvokeShutdown();
        }
    }
}