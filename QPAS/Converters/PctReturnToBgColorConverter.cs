// -----------------------------------------------------------------------
// <copyright file="ProfitToBgColorConverter.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QPAS
{
    [ValueConversion(typeof(string), typeof(Brush))]
    public class PctReturnToBgColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] pieces = ((string)value).Split('%');
            if (pieces.Length > 2 || pieces.Length == 1 || !string.IsNullOrEmpty(pieces[1]) || string.IsNullOrEmpty(pieces[0]))
            {
                return new System.Drawing.SolidBrush(System.Drawing.Color.White);
            }
            double ret = Double.Parse(pieces[0]);

            if (Math.Abs(ret) < 0.0000001)
            {
                return new System.Drawing.SolidBrush(System.Drawing.Color.White);
            }
            else if (ret > 0)
            {
                return new SolidColorBrush(Color.FromRgb(0xC0, 0xFF, 0xC0));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0xC0));
            }
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
