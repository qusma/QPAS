// -----------------------------------------------------------------------
// <copyright file="GenericExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public static class GenericExtensions
    {
        public static T TakeAndRemove<T>(this List<T> values, int index)
        {
            T tmp = values[index];
            values.RemoveAt(index);
            return tmp;
        }

        public static bool HasProperty(this object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }

        public static double WeightedAverage<T>(this IEnumerable<T> items, Func<T, double> value, Func<T, double> weight)
        {
            return items.Sum(x => value(x) * weight(x)) / items.Sum(weight);
        }

        public static decimal WeightedAverage<T>(this IEnumerable<T> items, Func<T, decimal> value, Func<T, decimal> weight)
        {
            return items.Sum(x => value(x) * weight(x)) / items.Sum(weight);
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                collection.Add(item);
            }
        }

        public static void AddRange<T>(this DbSet<T> collection, IEnumerable<T> items) where T : class
        {
            foreach (T item in items)
            {
                collection.Add(item);
            }
        }

        public static void RemoveRange<T>(this DbSet<T> collection, IEnumerable<T> items) where T : class
        {
            foreach (T item in items)
            {
                collection.Remove(item);
            }
        }

        /// <summary>
        /// Generates all possible combinations of the UNIQUE values in the list, of a given length.
        /// </summary>
        public static List<List<T>> Combinations<T>(this List<T> values, int length)
        {
            if (length < 1) throw new ArgumentOutOfRangeException("length");

            var result = new List<List<T>>();
            for (int i = 0; i <= values.Count - length; i++)
            {
                for (int j = i + 1; j <= values.Count - length + 1; j++)
                {
                    var newList = new List<T> { values[i] };

                    for (int k = j; k < j + length - 1; k++)
                    {
                        newList.Add(values[k]);
                    }

                    result.Add(newList);
                }
            }

            return result;
        }
    }
}