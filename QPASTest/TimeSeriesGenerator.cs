// -----------------------------------------------------------------------
// <copyright file="TimeSeriesGenerator.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using System;
using System.Collections.Generic;
using TimeSeries = QPAS.TimeSeries;

namespace QPASTest
{
    internal static class TimeSeriesGenerator
    {
        public static TimeSeries GenerateData(DateTime from, DateTime to, decimal price)
        {
            var data = new List<OHLCBar>();
            while (from <= to)
            {
                data.Add(new OHLCBar
                {
                    Open = price,
                    High = price,
                    Low = price,
                    Close = price,
                    DT = from
                });
                from = from.AddDays(1);
            }

            return new TimeSeries(data);
        }
    }
}