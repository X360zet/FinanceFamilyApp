using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FinanceFamilyApp.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                return (value as string) == (parameter as string) ? Visibility.Visible : Visibility.Collapsed;
            }

            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}