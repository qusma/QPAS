// -----------------------------------------------------------------------
// <copyright file="InstrumentExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EntityModel;

namespace QPAS
{
    public static class InstrumentExtensions
    {
        /// <summary>
        /// Given a QPAS instrument, find the closest matching QDMS instrument.
        /// </summary>
        public static QDMS.Instrument GetQDMSInstrument(this Instrument localInst, List<QDMS.Instrument> instrumentsList)
        {
            if (localInst.QDMSInstrumentID != 0 && instrumentsList.Any(x => x.ID == localInst.QDMSInstrumentID))
            {
                return instrumentsList.FirstOrDefault(x => x.ID == localInst.QDMSInstrumentID);
            }

            //Locate an insturment based on similar stuff
            return instrumentsList.FirstOrDefault(x =>
                (int)x.Type == (int)localInst.AssetCategory &&
                x.Symbol == localInst.Symbol &&
                x.Datasource.Name == PreferredDataSource(localInst.AssetCategory));
        }

        private static string PreferredDataSource(AssetClass ac)
        {
            using(var context = new DBContext())
            {
                var preferredSource = context.DatasourcePreferences.FirstOrDefault(x => x.AssetClass == ac);
                if (preferredSource == null) return "Interactive Brokers";
                return preferredSource.Datasource;
            }
        }
    }
}