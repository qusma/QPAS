// -----------------------------------------------------------------------
// <copyright file="AreaPoint.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QPAS
{
    public class AreaPoint
    {
        public DateTime X { get; set; }
        public double Y1 { get; set; }
        public double Y2 { get; set; }

        public AreaPoint(DateTime x, double y1, double y2)
        {
            X = x;
            Y1 = y1;
            Y2 = y2;
        }
    }
}
