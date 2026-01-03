using System.Globalization;
using Microsoft.Maui.Controls;

namespace PriceLists.Maui.Converters;

public class DecimalToArsCurrencyConverter : IValueConverter
{
    private static readonly CultureInfo ArsCulture = CreateCulture();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            decimal decimalValue => FormatAmount(decimalValue),
            double doubleValue => FormatAmount((decimal)doubleValue),
            float floatValue => FormatAmount((decimal)floatValue),
            _ => FormatAmount(0)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatAmount(decimal amount)
    {
        var cultureInfo = (CultureInfo)ArsCulture.Clone();
        return amount.ToString("C2", cultureInfo);
    }

    private static CultureInfo CreateCulture()
    {
        var culture = new CultureInfo("es-AR");
        culture.NumberFormat.CurrencySymbol = "$";
        culture.NumberFormat.CurrencyPositivePattern = 2; // "$ n"
        culture.NumberFormat.CurrencyNegativePattern = 9; // "-$ n"
        return culture;
    }
}
