using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;
using ArchivumWpf.Models;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;

namespace ArchivumWpf
{
    public partial class MainWindow : Window
    {
        private readonly IPreferencesService _preferencesService;
        
        public MainWindow(ClockViewModel clockViewModel, IPreferencesService preferencesService)
        {
            InitializeComponent();
            ClockPanel.DataContext = clockViewModel;
            _preferencesService = preferencesService;

            Loaded += (s, e) => ApplyWindowMode();
            
            WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (r, m) =>
            {
                Application.Current.Dispatcher.Invoke(() => ApplyWindowMode());
            });


        }

        private void ApplyWindowMode()
        {
            var prefs = _preferencesService.GetPreferences();

            if (prefs.WindowMode == "Full Screen")
            {
                if (this.WindowStyle != WindowStyle.None)
                {
                    this.WindowStyle = WindowStyle.None;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.WindowState = WindowState.Maximized;
                }
            }
            else
            {
                if (this.WindowStyle == WindowStyle.None)
                {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.ResizeMode = ResizeMode.CanResize;
                    this.WindowState = WindowState.Normal;
                }
            }
        }
    }
}