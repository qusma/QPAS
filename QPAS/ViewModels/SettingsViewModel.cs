// -----------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EntityModel;
using ReactiveUI;

namespace QPAS
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _statementSaveLocation;
        private string _logLocation;

        private IDBContext _context;
        public double AssumedInterestRate { get; set; }

        public string StatementSaveLocation
        {
            get => _statementSaveLocation;
            set => this.RaiseAndSetIfChanged(ref _statementSaveLocation, value);
        }

        public string LogLocation
        {
            get => _logLocation;
            set => this.RaiseAndSetIfChanged(ref _logLocation, value);
        }

        public string FlexToken { get; set; }

        public string FlexID { get; set; }

        public bool PreventDuplicateImports { get; set; }

        public decimal OptionsCapitalUsageMultiplier { get; set; }

        public bool TotalCapitalAlwaysUsesAllAccounts { get; set; }

        //QDMS stuff
        public bool QdmsAllowFreshData { get; set; }

        public string QdmsHost { get; set; }

        public int QdmsRealTimeRequestPort { get; set; }

        public int QdmsRealTimePublishPort { get; set; }

        public int QdmsInstrumentServerPort { get; set; }

        public int QdmsHistoricalDataPort { get; set; }

        public bool AllowExternalDataSource { get; set; }

        //MySQL
        public string MySqlHost { get; set; }
        public string MySqlUsername { get; set; }
        public string MySqlPassword { get; set; }

        //SQL Server
        public bool SqlServerUseWindowsAuthentication { get; set; }
        public string SqlServerHost { get; set; }
        public string SqlServerUsername { get; set; }

        //DB Selection
        public bool MySqlSelected { get; set; }
        public bool SqlServerSelected { get; set; }

        //QDMS data source preferences
        public ObservableCollection<DatasourcePreference> DatasourcePreferences { get; set; }
        public ObservableCollection<string> Datasources { get; set; }

        public SettingsViewModel(IDBContext context) : base(null)
        {
            _context = context;

            AssumedInterestRate = Properties.Settings.Default.assumedInterestRate;
            StatementSaveLocation = Properties.Settings.Default.statementSaveLocation;
            LogLocation = Properties.Settings.Default.logLocation;
            FlexID = Properties.Settings.Default.flexID;
            FlexToken = Properties.Settings.Default.flexToken;
            PreventDuplicateImports = Properties.Settings.Default.preventDuplicateImports;
            OptionsCapitalUsageMultiplier = Properties.Settings.Default.optionsCapitalUsageMultiplier;
            TotalCapitalAlwaysUsesAllAccounts = Properties.Settings.Default.totalCapitalAlwaysUsesAllAccounts;

            //QDMS stuff
            QdmsAllowFreshData = Properties.Settings.Default.qdmsAllowFreshData;
            QdmsHost = Properties.Settings.Default.qdmsHost;
            QdmsRealTimeRequestPort = Properties.Settings.Default.qdmsRealTimeRequestPort;
            QdmsRealTimePublishPort = Properties.Settings.Default.qdmsRealTimePublishPort;
            QdmsInstrumentServerPort = Properties.Settings.Default.qdmsInstrumentServerPort;
            QdmsHistoricalDataPort = Properties.Settings.Default.qdmsHistoricalDataPort;

            AllowExternalDataSource = Properties.Settings.Default.allowExternalDataSource;

            //MySQL
            MySqlHost = Properties.Settings.Default.mySqlHost;
            MySqlUsername = Properties.Settings.Default.mySqlUsername;

            //SQL Server
            SqlServerUseWindowsAuthentication = Properties.Settings.Default.sqlServerUseWindowsAuthentication;
            SqlServerUsername = Properties.Settings.Default.sqlServerUsername;
            SqlServerHost = Properties.Settings.Default.sqlServerHost;

            //db selection
            if(Properties.Settings.Default.databaseType == "MySql")
            {
                MySqlSelected = true;
            }
            else if (Properties.Settings.Default.databaseType == "SqlServer")
            {
                SqlServerSelected = true;
            }

            DatasourcePreferences = new ObservableCollection<DatasourcePreference>(
                context
                .DatasourcePreferences
                .ToList()
                .OrderBy(x => (int)x.AssetClass)
                .Select(x => new DatasourcePreference { AssetClass = x.AssetClass, Datasource = x.Datasource })
            );

            Datasources = new ObservableCollection<string>(
                new[] {
                    "Yahoo",
                    "Interactive Brokers",
                    "Quandl",
                    "FRED",
                    "Google"
                });
        }

        public void Save()
        {
            Properties.Settings.Default.assumedInterestRate = AssumedInterestRate;
            Properties.Settings.Default.statementSaveLocation = StatementSaveLocation;
            Properties.Settings.Default.logLocation = LogLocation;
            Properties.Settings.Default.flexID = FlexID;
            Properties.Settings.Default.flexToken = FlexToken;
            Properties.Settings.Default.preventDuplicateImports = PreventDuplicateImports;
            Properties.Settings.Default.optionsCapitalUsageMultiplier = OptionsCapitalUsageMultiplier;
            Properties.Settings.Default.totalCapitalAlwaysUsesAllAccounts = TotalCapitalAlwaysUsesAllAccounts;

            //QDMS stuff
            Properties.Settings.Default.qdmsAllowFreshData = QdmsAllowFreshData;
            Properties.Settings.Default.qdmsHost = QdmsHost;
            Properties.Settings.Default.qdmsRealTimeRequestPort = QdmsRealTimeRequestPort;
            Properties.Settings.Default.qdmsRealTimePublishPort = QdmsRealTimePublishPort;
            Properties.Settings.Default.qdmsInstrumentServerPort = QdmsInstrumentServerPort;
            Properties.Settings.Default.qdmsHistoricalDataPort = QdmsHistoricalDataPort;

            Properties.Settings.Default.allowExternalDataSource = AllowExternalDataSource;

            //MySQL
            Properties.Settings.Default.mySqlHost = MySqlHost;
            Properties.Settings.Default.mySqlUsername = MySqlUsername;

            //SQL Server
            Properties.Settings.Default.sqlServerUseWindowsAuthentication = SqlServerUseWindowsAuthentication;
            Properties.Settings.Default.sqlServerUsername = SqlServerUsername;
            Properties.Settings.Default.sqlServerHost = SqlServerHost;

            //db selection
            if (MySqlSelected)
            {
                Properties.Settings.Default.databaseType = "MySql";
            }
            else if (SqlServerSelected)
            {
                Properties.Settings.Default.databaseType = "SqlServer";
            }

            foreach (var dsp in DatasourcePreferences)
            {
                AssetClass ac = dsp.AssetClass;
                DatasourcePreference existingDsp = _context.DatasourcePreferences.FirstOrDefault(x => x.AssetClass == ac);
                if (existingDsp == null) continue;
                existingDsp.Datasource = dsp.Datasource;
            }
            _context.SaveChanges();

            Properties.Settings.Default.Save();
        }
    }
}