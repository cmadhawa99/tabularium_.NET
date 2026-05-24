using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using ArchivumWpf.Services;

namespace ArchivumWpf.Converters;

public class DynamicTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            var app = (App)Application.Current;
            var preferencesService = app.Services.GetService<IPreferencesService>();
            
            string timePref = preferencesService?.GetPreferences()?.TimeFormat ?? "12-Hour (AM/PM)";
            string format = timePref == "24-Hour" ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd hh:mm tt";
            
            return dt.ToString(format, CultureInfo.InvariantCulture);
        }
        
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    
}