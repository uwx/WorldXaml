using System.Globalization;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Yoga;

public class BoolToDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? YgDisplay.Flex : YgDisplay.None;
        }

        return YgDisplay.None;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is YgDisplay d)
        {
            return d == YgDisplay.Flex;
        }
        return false;
    }
}