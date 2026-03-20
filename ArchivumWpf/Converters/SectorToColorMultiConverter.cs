using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using ArchivumWpf.Models;

namespace ArchivumWpf.Converters;

public class SectorToColorMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return Brushes.Transparent;

        string sectorName = values[0] as string;
        bool enableColors = values[1] is bool b && b;
        var sectors = values[2] as ObservableCollection<SectorItem>;
        
        if (!enableColors || string.IsNullOrWhiteSpace(sectorName) || sectors == null)
            return Brushes.Transparent;
        
        var match = sectors.FirstOrDefault(s => s.Name == sectorName);
        if (match != null && !string.IsNullOrWhiteSpace(match.ColorHex))
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(match.ColorHex));
            }
            catch
            {
                return Brushes.Transparent;
            }
        }
        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}