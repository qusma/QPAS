// -----------------------------------------------------------------------
// <copyright file="LinkedListExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public static class LinkedListExtensions
    {
        public static T Dequeue<T>(this LinkedList<T> values)
        {
            var result = values.First();
            values.RemoveFirst();
            return result;
        }
    }
}