// -----------------------------------------------------------------------
// <copyright file="ChangelogWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for ChangelogWindow.xaml
    /// </summary>
    public partial class ChangelogWindow : MetroWindow
    {
        public ChangelogWindow()
        {
            if(string.IsNullOrEmpty(Properties.Resources.CHANGELOG))
            {
                Close();
                return;
            }

            InitializeComponent();
            ChangelogText.Text = Properties.Resources.CHANGELOG;
        }
    }
}
