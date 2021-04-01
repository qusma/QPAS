// -----------------------------------------------------------------------
// <copyright file="AboutWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows.Navigation;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class AboutWindow : MetroWindow
    {
        public AboutWindow()
        {
            InitializeComponent();

            VersionLabel.Content = string.Format("Version: {0}",
                GetVersion());
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/qusma/QPAS") { UseShellExecute = true });
            e.Handled = true;
        }

        private string GetVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }
    }
}