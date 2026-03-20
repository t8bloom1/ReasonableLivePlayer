using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ReasonableLivePlayer.Converters;

/// <summary>
/// Multi-value converter: values[0] = Song.IsActive (bool), values[1] = VM.IsPlaylistActive (bool).
/// Always shows "▶" for the active song. Color changes: green when playing, light grey when paused.
/// </summary>
public class ActiveIndicatorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not bool isActive)
            return "";
        return isActive ? "▶" : "";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ActiveIndicatorColorConverter : IMultiValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new((Color)ColorConverter.ConvertFromString("#00CC66"));
    private static readonly SolidColorBrush GreyBrush = new((Color)ColorConverter.ConvertFromString("#999999"));
    private static readonly SolidColorBrush TransparentBrush = Brushes.Transparent;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not bool isActive)
            return TransparentBrush;
        if (!isActive) return TransparentBrush;
        bool isPlaying = values[1] is true;
        return isPlaying ? GreenBrush : GreyBrush;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
