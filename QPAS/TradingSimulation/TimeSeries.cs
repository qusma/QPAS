// -----------------------------------------------------------------------
// <copyright file="TimeSeries.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using System;
using System.Collections.Generic;

namespace QPAS
{
    public class TimeSeries
    {
        public List<OHLCBar> Series { get; set; }

        public int CurrentBar { get; private set; }

        public TimeSeries(List<OHLCBar> data)
        {
            Series = data;
            CurrentBar = -1;
        }

        public void ProgressTo(DateTime date)
        {
            if (Series == null) return;

            while (CurrentBar < Series.Count - 1 && Series[CurrentBar + 1].DT.Date <= date)
            {
                CurrentBar++;
            }
        }

        public OHLCBar this[int index]
        {
            get
            {
                return Series[CurrentBar - index];
            }
        }
    }
}