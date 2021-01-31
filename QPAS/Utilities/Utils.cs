// -----------------------------------------------------------------------
// <copyright file="Utils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using QDMS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        /// <summary>
        /// Returns true if this is the first time running on this version.
        /// </summary>
        /// <returns></returns>
        public static bool CheckAndSaveVersion()
        {
            var dirPath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(dirPath, "lastversion");
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, currentVersion.ToString());
                return true;
            }

            var contents = File.ReadAllText(filePath);
            if (contents == currentVersion.ToString())
            {
                return false;
            }

            File.WriteAllText(filePath, currentVersion.ToString());
            return true;
        }

        public static TimeSeries TimeSeriesFromFXRates(IEnumerable<FXRate> rates)
        {
            var bars = new List<OHLCBar>();
            foreach (var rate in rates)
            {
                var bar = new OHLCBar
                {
                    Open = rate.Rate,
                    High = rate.Rate,
                    Low = rate.Rate,
                    Close = rate.Rate,
                    AdjOpen = rate.Rate,
                    AdjHigh = rate.Rate,
                    AdjLow = rate.Rate,
                    AdjClose = rate.Rate,
                    DT = rate.Date
                };
                bars.Add(bar);
            }

            return new TimeSeries(bars);
        }

        public static T GetDataFromClipboard<T>() where T : class
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
