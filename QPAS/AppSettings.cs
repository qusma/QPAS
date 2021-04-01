using System.Windows;

namespace QPAS
{
    public class AppSettings : IAppSettings
    {
        public string AccountId { get; set; }

        public string StatementSaveLocation { get; set; }
        public string LogLocation { get; set; }
        public double AssumedInterestRate { get; set; } = 0.02;
        public string FlexToken { get; set; }
        public string FlexId { get; set; }
        public bool PreventDuplicateImports { get; set; } = true;
        public string QdmsHost { get; set; } = "127.0.0.1";
        public int QdmsRealTimeRequestPort { get; set; } = 5556;
        public int QdmsRealTimePublishPort { get; set; } = 5557;
        public int QdmsInstrumentServerPort { get; set; } = 5558;
        public int QdmsHistoricalDataPort { get; set; } = 5555;
        public int QdmsHttpPort { get; set; } = 5559;
        public bool QdmsUseSsl { get; set; } = true;
        public string QdmsApiKey { get; set; }

        /// <summary>
        /// Allow querying of external datasources rather than the local db only
        /// </summary>
        public bool QdmsAllowFreshData { get; set; } = false;

        public bool AllowExternalDataSource { get; set; } = false;
        public decimal OptionsCapitalUsageMultiplier { get; set; } = 0.1m;
        public bool TotalCapitalAlwaysUsesAllAccounts { get; set; } = true;
        public string DataGridLayout { get; set; }

        public double Height { get; set; }
        public double Width { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public WindowState WindowState { get; set; }
    }
}