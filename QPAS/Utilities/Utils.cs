// -----------------------------------------------------------------------
// <copyright file="Utils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
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

        public static T GetDataFromClipboard<T>() where T: class
        {
            IDataObject dataObject = Clipboard.GetDataObject();
            if (dataObject != null)
            {
                string dataFormat = typeof(T).FullName;
                if (dataObject.GetDataPresent(dataFormat))
                {
                    // Retrieve slides from the clipboard
                    T data = dataObject.GetData(dataFormat) as T;
                    if (data != null)
                    {
                        return data;
                    }
                }
            }

            return null;
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
    }
}
