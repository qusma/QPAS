namespace QPAS
{
    public interface IAppSettings
    {
        string AccountId { get; set; }
        bool AllowExternalDataSource { get; set; }
        double AssumedInterestRate { get; set; }
        string DataGridLayout { get; set; }
        string FlexId { get; set; }
        string FlexToken { get; set; }
        string LogLocation { get; set; }
        decimal OptionsCapitalUsageMultiplier { get; set; }
        bool PreventDuplicateImports { get; set; }
        bool QdmsAllowFreshData { get; set; }
        string QdmsApiKey { get; set; }
        int QdmsHistoricalDataPort { get; set; }
        string QdmsHost { get; set; }
        int QdmsHttpPort { get; set; }
        int QdmsInstrumentServerPort { get; set; }
        int QdmsRealTimePublishPort { get; set; }
        int QdmsRealTimeRequestPort { get; set; }
        bool QdmsUseSsl { get; set; }
        string StatementSaveLocation { get; set; }

        bool TotalCapitalAlwaysUsesAllAccounts { get; set; }
    }
}