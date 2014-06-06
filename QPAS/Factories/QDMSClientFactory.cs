// -----------------------------------------------------------------------
// <copyright file="QDMSClientFactory.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QPAS
{
    public static class QDMSClientFactory
    {
        public static QDMSClient.QDMSClient Get()
        {
            return new QDMSClient.QDMSClient(
                "QPASClient", 
                Properties.Settings.Default.qdmsHost,
                Properties.Settings.Default.qdmsRealTimeRequestPort,
                Properties.Settings.Default.qdmsRealTimePublishPort,
                Properties.Settings.Default.qdmsInstrumentServerPort,
                Properties.Settings.Default.qdmsHistoricalDataPort);
        }
    }
}
