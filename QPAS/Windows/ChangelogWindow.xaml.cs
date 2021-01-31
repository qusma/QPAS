// -----------------------------------------------------------------------
// <copyright file="ChangelogWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using System.IO;
using System.Reflection;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for ChangelogWindow.xaml
    /// </summary>
    public partial class ChangelogWindow : MetroWindow
    {
        public ChangelogWindow()
        {
            string changelog;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QPAS.Resources.CHANGELOG.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                changelog = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(changelog))
            {
                Close();
                return;
            }

            InitializeComponent();
            ChangelogText.Text = changelog;
        }
    }
}
