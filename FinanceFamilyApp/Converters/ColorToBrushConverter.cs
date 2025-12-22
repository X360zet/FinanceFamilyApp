using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceFamilyApp.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorHex)
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
                }
                catch
                {
                    return new SolidColorBrush(Colors.White);
                }
            }

            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}