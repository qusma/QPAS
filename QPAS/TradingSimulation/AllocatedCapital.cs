// -----------------------------------------------------------------------
// <copyright file="AllocatedCapital.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace QPAS
{
    /// <summary>
    /// Holds data on allocated capital.
    /// </summary>
    public class AllocatedCapital
    {
        public List<decimal> Long { get; private set; }
        public List<decimal> Short { get; private set; }
        public List<decimal> Gross { get; private set; }
        public List<decimal> Net { get; private set; }

        public decimal TodaysCapitalLong { get; private set; }
        public decimal TodaysCapitalShort { get; private set; }
        public decimal TodaysCapitalGross { get { return TodaysCapitalLong + TodaysCapitalShort; } }
        public decimal TodaysCapitalNet { get { return TodaysCapitalLong - TodaysCapitalShort; } }

        public AllocatedCapital()
        {
            Long = new List<decimal>();
            Short = new List<decimal>();
            Gross = new List<decimal>();
            Net = new List<decimal>();
        }

        public void AddLong(decimal capital)
        {
            TodaysCapitalLong += capital;
        }

        public void AddShort(decimal capital)
        {
            TodaysCapitalShort += capital;
        }

        /// <summary>
        /// Call at end of day to move todays values to the lists.
        /// </summary>
        public void EndOfDay()
        {
            Add(TodaysCapitalLong, TodaysCapitalShort);
            TodaysCapitalLong = 0;
            TodaysCapitalShort = 0;
        }

        private void Add(decimal capitalLong, decimal capitalShort)
        {
            Long.Add(capitalLong);
            Short.Add(capitalShort);
            Gross.Add(capitalLong + capitalShort);
            Net.Add(capitalLong - capitalShort);
        }
    }
}