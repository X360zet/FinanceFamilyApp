using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceFamilyApp.Converters
{
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                return type == "Доход"
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}