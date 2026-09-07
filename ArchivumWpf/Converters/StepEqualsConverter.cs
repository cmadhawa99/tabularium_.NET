using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ArchivumWpf.Converters;

/// <summary>
/// Compares a bound int (e.g. a wizard's CurrentStep) against ConverterParameter.
/// Returns Visible when equal, Collapsed otherwise. Used to show/hide wizard steps.
/// </summary>
public class StepEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter != null &&
            int.TryParse(parameter.ToString(), out var targetStep))
        {
            return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}