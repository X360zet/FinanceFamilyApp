using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceFamilyApp.Converters
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}