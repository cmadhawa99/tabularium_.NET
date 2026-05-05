using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchivumWpf.ViewModels;

public partial class ClockViewModel : ObservableObject
{
    // Expose the raw DateTime object
    [ObservableProperty] private DateTime _currentTime;

    private readonly DispatcherTimer _timer;

    public ClockViewModel()
    {
        CurrentTime = DateTime.Now;
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now;
        _timer.Start();
    }
}