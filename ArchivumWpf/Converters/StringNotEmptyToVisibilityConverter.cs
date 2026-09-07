using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ArchivumWpf.Converters;

/// <summary>
/// Visible when the bound string is non-null/non-whitespace. Used for error message banners.
/// </summary>
public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;
        return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}