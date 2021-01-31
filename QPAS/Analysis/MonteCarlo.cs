// -----------------------------------------------------------------------
// <copyright file="MonteCarlo.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QPAS
{
    public static class MonteCarlo
    {
        /// <summary>
        /// Given a list of gross returns, generates a list of equity curves and drawdown curves using bootstrap sampling. Returns true if successful.
        /// </summary>
        /// <param name="periods">Number of periods in each run.</param>
        /// <param name="runs">Number of times to run the simulation.</param>
        /// <param name="clusterSize">Helps to capture volatility clustering by picking a range of continuous returns instead of single values.</param>
        /// <param name="replacement">Sample with replacement.</param>
        /// <param name="returns">List of net returns.</param>
        /// <param name="equityCurves">List of equity curves.</param>
        /// <param name="drawdownCurves">List of drawdown curves</param>
        public static bool Bootstrap(int periods, int runs, int clusterSize, bool replacement, List<double> returns, out List<List<double>> equityCurves, out List<List<double>> drawdownCurves)
        {
            equityCurves = new List<List<double>>(runs);
            drawdownCurves = new List<List<double>>(runs);
            if (!replacement && periods > returns.Count) return false; //not enough data to do a run without replacement
            if (clusterSize > periods) return false; //cluster larger than periods, makes no sense
            if (clusterSize > returns.Count) return false; //cluster larger than data, doesn't work
            if (clusterSize < 1) return false; //gotta be at least 1, anything else is nonsensical

            var rand = new Random();
            int loopsNeeded = (int)Math.Ceiling((double)periods / clusterSize);
            int maxRand = returns.Count - clusterSize; //subtract cluster size so that we don't try to access data beyond the bounds of the list

            List<double> tmpReturns = new List<double>(returns);
            for (int i = 0; i < runs; i++)
            {
                equityCurves.Add(new List<double>(periods + 1));
                drawdownCurves.Add(new List<double>(periods + 1));
                equityCurves[i].Add(100); //starting value is arbitrary, 100 just makes it easy to read things
                drawdownCurves[i].Add(0);

                if (replacement)
                {
                    tmpReturns = new List<double>(returns);
                }

                double maxEquity = 100;

                for (int j = 0; j < loopsNeeded; j++)
                {
                    int startingIndex = rand.Next(maxRand);
                    int baseIndex = j * clusterSize;
                    for (int k = 0; k < clusterSize; k++)
                    {
                        equityCurves[i].Add(replacement
                                                ? equityCurves[i][baseIndex + k] * (1 + tmpReturns[startingIndex + k])
                                                : equityCurves[i][baseIndex + k] * (1 + tmpReturns.TakeAndRemove(startingIndex))); //returns the value at the selected index then removes it
                        maxEquity = Math.Max(equityCurves[i][baseIndex + 1 + k], maxEquity);
                        drawdownCurves[i].Add(equityCurves[i][baseIndex + 1 + k] / maxEquity - 1);
                        if (baseIndex + k + 1 == periods) break;
                    }
                }
            }

            return true;
        }
    }
}