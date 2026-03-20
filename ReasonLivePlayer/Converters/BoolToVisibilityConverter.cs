using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReasonLivePlayer.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (bool)value ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => (Visibility)value == Visibility.Visible;
}
