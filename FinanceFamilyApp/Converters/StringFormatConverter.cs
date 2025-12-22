using System;
using System.Globalization;
using System.Windows.Data;

namespace FinanceFamilyApp.Converters
{
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string format)
            {
                var parts = format.Split(';');
                if (value is bool boolValue && parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
                return format;
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}