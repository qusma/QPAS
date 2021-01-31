// -----------------------------------------------------------------------
// <copyright file="QDMSClientFactory.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QPAS
{
    public static class QDMSClientFactory
    {
        public static QDMSClient.QDMSClient Get(IAppSettings settings)
        {
            return new QDMSClient.QDMSClient(
                "QPASClient",
                settings.QdmsHost,
                settings.QdmsRealTimeRequestPort,
                settings.QdmsRealTimePublishPort,
                settings.QdmsHistoricalDataPort,
                settings.QdmsHttpPort,
                settings.QdmsApiKey,
                useSsl: settings.QdmsUseSsl);
        }
    }
}
