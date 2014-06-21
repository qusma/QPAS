using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;

namespace QPAS
{
    public static class MultiDimensionalScaling
    {
        public static Matrix<double> Scale(Matrix<double> input)
        {
            int n = input.RowCount;

            Matrix<double> p = DenseMatrix.Build.DenseIdentity(n) - DenseMatrix.Create(n,n, (_, __) => 1.0 / n);

            Matrix<double> a = -.5 * input.PointwiseMultiply(input);
            Matrix<double> b = p.Multiply(p.Multiply(a));
            b = (b + b.Transpose()).Divide(2.0);
            var evd = b.Evd();
            Vector<double> E = DenseVector.OfEnumerable(evd.EigenValues.Select(x => x.Real));
            Matrix<double> V = evd.EigenVectors;


            DenseVector i = DenseVector.Create(E.Count, x => x);
            Sorting.Sort(E, i);
            

            var e = DenseVector.OfEnumerable(E.Reverse());
            i = DenseVector.OfEnumerable(i.Reverse());

            Vector keep = DenseVector.Create(e.Count(x => x > 0.000000001), _ => 0);
            int counter = 0;
            for (int j = 0; j < e.Count; j++)
            {
                if (e[j] > 0.000000001)
                {
                    keep[j] = counter;
                    counter++;
                }
            }

            Matrix<double> Y;
            if (e.Count(x => x > 0.000000001) == 0)
            {
                Y = DenseMatrix.Create(n, n, (_, __) => 0);
            }
            else
            {
                Y = DenseMatrix.Create(V.RowCount, keep.Count, (_, __) => 0);
                for (int j = 0; j < keep.Count; j++)
                {
                    Y.SetColumn(j, (V.Column((int)(i[(int)(keep[j] + 0.5)] + 0.5)).ToArray()));
                }
                Y = Y.Multiply(DiagonalMatrix.OfDiagonal(keep.Count, keep.Count, e.Where((x, j) => keep.Contains(j)).Select(Math.Sqrt)));
            }

            //Enforce a sign convention on the solution -- the largest element
            //in each coordinate will have a positive sign.
            List<int> maxIndices = Y.EnumerateColumns().Select(x => x.AbsoluteMaximumIndex()).ToList();
            var colSigns = maxIndices.Select((x, j) => Math.Sign(Y[x, j])).ToList();
            for (int j = 0; j < Y.ColumnCount; j++)
            {
                Y.SetColumn(j, Y.Column(j) * colSigns[j]);
            }

            return Y;
        }
    }
}
