// -----------------------------------------------------------------------
// <copyright file="AssetClassToImageConverter.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System;
using System.Globalization;
using System.Windows.Data;

namespace QPAS
{
    [ValueConversion(typeof(AssetClass), typeof(string))]
    public class AssetClassToImageConverter : IValueConverter
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
            if (value is string && string.IsNullOrEmpty((string)value)) return null;

            AssetClass assetClass;
            try
            {
                assetClass = (AssetClass)value;
            }
            catch
            {
                return null;
            }

            switch (assetClass)
            {
                case AssetClass.Stock:
                    return "../Resources/stocks.png";

                case AssetClass.Bond:
                    return "../Resources/bond.png";

                case AssetClass.CFD:
                    return "../Resources/CFD.png";

                case AssetClass.Cash:
                    return "../Resources/forex.png";

                case AssetClass.FutureOption:
                    return "../Resources/futuresOptions.png";

                case AssetClass.Future:
                    return "../Resources/futures.png";

                case AssetClass.Index:
                    return "../Resources/index.png";

                case AssetClass.Option:
                    return "../Resources/options.png";

                case AssetClass.Warrant:
                    return "../Resources/warrant.png";

                default:
                    return "../Resources/index.png";
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
            return null;
        }
    }
}