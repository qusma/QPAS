// -----------------------------------------------------------------------
// <copyright file="Utils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MathNet.Numerics.LinearAlgebra.Double;

namespace QPAS
{
    public static class Utils
    {
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        public static string FormatTimespan(TimeSpan t)
        {
            string text;
            if (t.Days > 0)
            {
                text = string.Format("{0:D2}d:{1:D2}h:{2:D2}m:{3:D2}s",
                                     t.Days,
                                     t.Hours,
                                     t.Minutes,
                                     t.Seconds);
            }
            else if (t.Hours > 0)
            {
                text = string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
                                     t.Hours,
                                     t.Minutes,
                                     t.Seconds);
            }
            else if (t.Minutes > 0)
            {
                text = string.Format("{0:D2}m:{1:D2}s",
                                     t.Minutes,
                                     t.Seconds);
            }
            else
            {
                text = string.Format("{0:D2}s",
                                     t.Seconds);
            }

            return text;
        }

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

        /// <summary>
        /// Multiple linear regression. Do not include intercept column, it's added automatically.
        /// </summary>
        public static void MLR(List<double> y, List<double> x, out double[] b, out double rSquared)
        {
            MLR(y, new List<List<double>> { x }, out b, out rSquared);
        }
    }
}
