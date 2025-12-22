using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceFamilyApp.Converters
{
    public class BalanceToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal balance)
            {
                return balance >= 0
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C9"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCDD2"));
            }

            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}