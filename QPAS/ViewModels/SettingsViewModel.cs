// -----------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace QPAS
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IContextFactory _contextFactory;

        public IAppSettings Settings { get; set; }

        //QDMS data source preferences
        public ObservableCollection<DatasourcePreference> DatasourcePreferences { get; set; }

        public ObservableCollection<string> Datasources { get; set; }

        public SettingsViewModel(IAppSettings settings, IContextFactory contextFactory) : base(null)
        {
            Settings = settings;
            _contextFactory = contextFactory;
            using (var context = _contextFactory.Get())
            {
                DatasourcePreferences = new ObservableCollection<DatasourcePreference>(
                    context
                    .DatasourcePreferences
                    .ToList()
                    .OrderBy(x => (int)x.AssetClass));

                Datasources = new ObservableCollection<string>(
                    new[] {
                    "Yahoo",
                    "Interactive Brokers",
                    "Quandl",
                    "FRED",
                    "Google"
                    });
            }
        }

        public void Save()
        {
            using (var context = _contextFactory.Get())
            {
                foreach (var dsp in DatasourcePreferences)
                {
                    context.Entry(dsp).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                context.SaveChanges();
            }
            SettingsUtils.SaveSettings(Settings);
        }
    }
}