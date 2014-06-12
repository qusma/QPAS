// -----------------------------------------------------------------------
// <copyright file="MathUtils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.Statistics;

namespace QPAS
{
    public static class MathUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="percentile"></param>
        /// <param name="order"></param>
        /// <returns>Set to false if the values are already ordered</returns>
        public static double Percentile(this List<double> values, double percentile, bool order = true)
        {
            if (!values.Any()) return 1;
            int index = (int)Math.Round(percentile * values.Count());

            if (order)
                return values.OrderBy(x => x).ElementAt(index);
            else
                return values.ElementAt(index);
        }

        /// <summary>
        /// Multiple linear regression. Do not include intercept column, it's added automatically.
        /// </summary>
        public static void MLR(List<double> y, List<List<double>> x, out double[] b, out double rSquared)
        {
            if (y.Count <= 1)
            {
                b = Enumerable.Range(1, x.Count + 1).Select(z => 0.0).ToArray();
                rSquared = 0;
                return;
            }
            DenseMatrix yMatrix = DenseMatrix.OfColumns(y.Count, 1, new List<List<double>> { y });

            //insert a list of zeroes, the intercept
            x.Insert(0, Enumerable.Range(0, x[0].Count).Select(z => 1.0).ToList());

            DenseMatrix xMatrix = DenseMatrix.OfColumns(x[0].Count, x.Count, x);

            var p = xMatrix.QR().Solve(yMatrix);

            double yAvg = y.Average();
            double ssReg = (xMatrix * p).Column(0).Sum(f => (f - yAvg) * (f - yAvg));
            double ssTotal = y.Sum(yVal => (yVal - yAvg) * (yVal - yAvg));

            rSquared = ssReg / ssTotal;
            b = p.Column(0).ToArray();
        }

        public static Matrix<double> CorrelationMatrix(List<List<double>> x)
        {
            Matrix<double> correlM = new DenseMatrix(x.Count, x.Count);

            //off diagonal elements
            for (int i = 0; i < x.Count; i++)
            {
                var rowx = x[i];
                for (int j = 0; j < x.Count; j++)
                {
                    var rowy = x[j];
                    if (i > j)
                    {
                        correlM[i, j] = Correlation.Pearson(rowx, rowy);
                    }
                    if (i < j)
                    {
                        correlM[i, j] = correlM[j, i];
                    }
                }
            }
            //Diagonal elements
            for (int i = 0; i < x.Count; i++)
            {
                correlM[i, i] = 1;
            }

            return correlM;
        }

        /// <summary>
        /// Multiple linear regression. Do not include intercept column, it's added automatically.
        /// </summary>
        public static void MLR(List<double> y, List<double> x, out double[] b, out double rSquared)
        {
            MLR(y, new List<List<double>> { x }, out b, out rSquared);
        }

        public static List<double> AutoCorr(List<double> input, int n)
        {
            double avg = input.Average();
            Complex[] inputAsComplex = input.Select(x => new Complex(x - avg,0)).ToArray();
            Transform.FourierForward(inputAsComplex, FourierOptions.Matlab);
            var conjugate = inputAsComplex.Select(Complex.Conjugate);
            var S = conjugate.Select((x, i) => x * inputAsComplex[i]).ToArray();
            Transform.FourierInverse(S, FourierOptions.Matlab);
            double first = S[0].Real;

            return S.Take(n).Select(x => x.Real / first).ToList();
        }

        public static List<double> PartialAutoCorr(List<double> input, int n)
        {
            return new List<double>();
        }
    }
}
