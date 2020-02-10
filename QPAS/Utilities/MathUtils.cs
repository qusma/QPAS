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
using MathNet.Numerics.LinearAlgebra;
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
        public static void MLR(IEnumerable<double> y, List<IEnumerable<double>> x, out double[] b, out double rSquared)
        {
            if (y.Count() <= 1)
            {
                b = Enumerable.Range(1, x.Count + 1).Select(z => 0.0).ToArray();
                rSquared = 0;
                return;
            }
            DenseMatrix yMatrix = DenseMatrix.OfColumns(y.Count(), 1, new List<IEnumerable<double>> { y });

            //insert a list of zeroes, the intercept
            x.Insert(0, Enumerable.Range(0, x[0].Count()).Select(z => 1.0).ToList());

            DenseMatrix xMatrix = DenseMatrix.OfColumns(x[0].Count(), x.Count, x);

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
                    if (i < j)
                    {
                        correlM[i, j] = Correlation.Pearson(rowx, rowy);
                    }
                    if (i > j)
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
        public static void MLR(IEnumerable<double> y, IEnumerable<double> x, out double[] b, out double rSquared)
        {
            MLR(y, new List<IEnumerable<double>> { x }, out b, out rSquared);
        }

        public static List<double> AutoCorr(List<double> input, int n)
        {
            double avg = input.Average();
            Complex[] inputAsComplex = input.Select(x => new Complex(x - avg,0)).ToArray();
            Fourier.Forward(inputAsComplex, FourierOptions.Matlab);
            var conjugate = inputAsComplex.Select(Complex.Conjugate);
            var S = conjugate.Select((x, i) => x * inputAsComplex[i]).ToArray();
            Fourier.Inverse(S, FourierOptions.Matlab);
            double first = S[0].Real;

            return S.Take(n).Select(x => x.Real / first).ToList();
        }

        public static List<double> PartialAutoCorr(List<double> input, int lagCount)
        {
            int n = input.Count;

            DenseVector v = DenseVector.OfEnumerable(input);
            List<double> coeffs = new List<double>();
            coeffs.Add(1);

            var lagMatrix = DenseMatrix.Create(input.Count, n, (_, __) => 0);
            for (int i = 0; i < lagCount; i++)
            {
                lagMatrix.SetColumn(i, Zeros(i).Concat(input.Take(n - i)).ToArray());
            }

            for (int i = 1; i < lagCount; i++)
            {
                var tmpMatrix = lagMatrix.SubMatrix(i - 1, n - i, 0, i);
                tmpMatrix = tmpMatrix.InsertColumn(0, DenseVector.Create(n - i, _ => 1));
                var qr = tmpMatrix.QR();
                var R = qr.R.SubMatrix(0, i + 1, 0, i + 1);
                var Q = qr.Q.SubMatrix(0, n - i, 0, i + 1);
                var b = R.Inverse().Multiply((Q.Transpose() * v.SubVector(i,n - i)));
                coeffs.Add(b.Last());
            }
            return coeffs;
        }

        public static void PCA(DenseMatrix input, out MathNet.Numerics.LinearAlgebra.Vector<double> latent, out Matrix<double> score, out Matrix<double> coeff)
        {
            int n = input.RowCount;
            int p = input.ColumnCount;

            //de-mean input
            var tmpInput = DenseMatrix.OfMatrix(input);
            for (int i = 0; i < tmpInput.ColumnCount; i++)
            {
                double avg = tmpInput.Column(i).Average();
                tmpInput.SetColumn(i, tmpInput.Column(i).Subtract(avg));
            }

            var svd = tmpInput.Svd(true);
            var sigma = svd.S;
            var tmpCoeff = svd.VT.Transpose();

            score = DenseMatrix.Create(n, p, (_, __) => 0);
            var U = svd.U.SubMatrix(0, n, 0, p);
            for (int i = 0; i < U.RowCount; i++)
            {
                score.SetRow(i, U.Row(i).PointwiseMultiply(sigma));
            }

            sigma = sigma.Divide(Math.Sqrt(n - 1));
            latent = sigma.PointwiseMultiply(sigma);

            //give the largest absolute value in each column a positive sign
            var maxIndices = tmpCoeff.EnumerateColumns().Select(x => x.AbsoluteMaximumIndex());
            var colSigns = maxIndices.Select((x, j) => Math.Sign(tmpCoeff[x, j])).ToList();
            for (int j = 0; j < tmpCoeff.ColumnCount; j++)
            {
                tmpCoeff.SetColumn(j, tmpCoeff.Column(j) * colSigns[j]);
                score.SetColumn(j, score.Column(j) * colSigns[j]);
            }

            coeff = tmpCoeff;
        }

        private static IEnumerable<double> Zeros(int n)
        {
            return Enumerable.Range(0, n).Select(x => 0.0);
        }
    }
}
