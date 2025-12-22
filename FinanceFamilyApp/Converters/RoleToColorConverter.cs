using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceFamilyApp.Converters
{
    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                return role switch
                {
                    "Администратор" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"))
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
