using System.Windows;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace ArchivumWpf;

public partial class MainWindow : Window
{
    private readonly IPreferencesService _preferencesService;

    public MainWindow(ClockViewModel clockViewModel, IPreferencesService preferencesService)
    {
        InitializeComponent();
        ClockPanel.DataContext = clockViewModel;
        _preferencesService = preferencesService;

        Loaded += (s, e) => ApplyWindowMode();

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this,
            (r, m) => { Application.Current.Dispatcher.Invoke(() => ApplyWindowMode()); });
    }

    private void ApplyWindowMode()
    {
        var prefs = _preferencesService.GetPreferences();

        if (prefs.WindowMode == "Full Screen")
        {
            if (WindowStyle != WindowStyle.None)
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
            }
        }
        else
        {
            if (WindowStyle == WindowStyle.None)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = WindowState.Normal;
            }
        }
    }
}