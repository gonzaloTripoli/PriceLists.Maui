using System.Globalization;
using Microsoft.Maui.Controls;

namespace PriceLists.Maui.Converters;

public class DecimalToCurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            decimal decimalValue => decimalValue.ToString("C", culture),
            double doubleValue => doubleValue.ToString("C", culture),
            float floatValue => floatValue.ToString("C", culture),
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
