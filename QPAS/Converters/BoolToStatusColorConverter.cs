using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QPAS
{
    public class BoolToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return new SolidColorBrush(Color.FromRgb(0, 153, 0));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(255, 173, 179));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}