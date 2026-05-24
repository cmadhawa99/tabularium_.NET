using System;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ArchivumWpf.Models;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class ClockViewModel : ObservableObject
{
    
    private readonly IPreferencesService _preferencesService;
    private readonly DispatcherTimer _timer;
    private string _timeFormatPref = "12-Hour (AM/PM)";
    
    [ObservableProperty] private DateTime _currentTime;
    [ObservableProperty] private string _formattedTime =  string.Empty;
    
    public ClockViewModel(IPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
        UpdatePreferences();
        
        CurrentTime = DateTime.Now;
        UpdateFormattedTime();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => 
        {
            CurrentTime = DateTime.Now;
            UpdateFormattedTime();
        };
        _timer.Start();
        
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (r, m) =>
        {
            UpdatePreferences();
            UpdateFormattedTime();
        });
    }

    private void UpdatePreferences()
    {
        _timeFormatPref = _preferencesService.GetPreferences().TimeFormat ?? "12-Hour (AM/PM)";
    }

    private void UpdateFormattedTime()
    {
        if (_timeFormatPref == "24-Hour")
        {
            FormattedTime = CurrentTime.ToString("HH:mm:ss");
        }
        else
        {
            FormattedTime = CurrentTime.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture);
        }
    }
}