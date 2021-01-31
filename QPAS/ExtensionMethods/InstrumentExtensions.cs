// -----------------------------------------------------------------------
// <copyright file="InstrumentExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public static class InstrumentExtensions
    {
        /// <summary>
        /// Given a QPAS instrument, find the closest matching QDMS instrument.
        /// </summary>
        public static QDMS.Instrument GetQDMSInstrument(this Instrument localInst, List<QDMS.Instrument> instrumentsList, List<DatasourcePreference> preferences)
        {
            if (localInst.QDMSInstrumentID != 0 && instrumentsList.Any(x => x.ID == localInst.QDMSInstrumentID))
            {
                return instrumentsList.FirstOrDefault(x => x.ID == localInst.QDMSInstrumentID);
            }

            //Locate an insturment based on similar stuff
            return instrumentsList.FirstOrDefault(x =>
                (int)x.Type == (int)localInst.AssetCategory &&
                x.Symbol == localInst.Symbol &&
                x.Datasource.Name == PreferredDataSource(localInst.AssetCategory, preferences));
        }

        private static string PreferredDataSource(AssetClass ac, List<DatasourcePreference> datasourcePreferences)
        {
            var preferredSource = datasourcePreferences.FirstOrDefault(x => x.AssetClass == ac);
            if (preferredSource == null) return "Interactive Brokers";
            return preferredSource.Datasource;
        }
    }
}