// -----------------------------------------------------------------------
// <copyright file="HistogramExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;

namespace QPAS
{
    public static class HistogramExtensions
    {
        public static List<Tuple<string, double>> GetBuckets(this Histogram histogram, string format = "0.00")
        {
            var buckets = new List<Tuple<string, double>>();
            string formatString = @"{0:" + format + @"} - {1:" + format + @"}";
            for (int i = 0; i < histogram.BucketCount; i++)
            {
                buckets.Add(new Tuple<string, double>(string.Format(formatString, histogram[i].LowerBound, histogram[i].UpperBound), histogram[i].Count));
            }
            return buckets;
        }
    }
}
