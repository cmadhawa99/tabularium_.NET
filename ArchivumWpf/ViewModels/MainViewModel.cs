using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject _currentPageViewModel;
    [ObservableProperty] private bool _isDarkMode = true;
    [ObservableProperty] private bool _hasDisposalAlert = false;
    [ObservableProperty] private string _disposalAlertText = string.Empty;
    
    [ObservableProperty] private string _activePage = "Dashboard";

    private readonly IArchiveService _archiveService;
    private readonly DashboardViewModel _dashboardVm;
    private readonly SearchViewModel _searchVm;
    private readonly CirculationViewModel _circulationVm;
    private readonly EntryViewModel _entryVm;
    private readonly ReportsViewModel _reportsVm;
    private readonly SettingsViewModel _settingsVm;
    private readonly DisposalViewModel _disposalVm;
    private readonly DocumentsSearchViewModel _documentsSearchVm;

    private readonly IPreferencesService _preferencesService;

    public MainViewModel(
        IArchiveService archiveService,
        IPreferencesService preferencesService,
        DashboardViewModel dashboardVm,
        SearchViewModel searchVm,
        CirculationViewModel circulationVm,
        EntryViewModel entryVm,
        ReportsViewModel reportsVm,
        SettingsViewModel settingsVm,
        DisposalViewModel disposalVm,
        DocumentsSearchViewModel documentsSearchVm
        )
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;
        _dashboardVm = dashboardVm;
        _searchVm = searchVm;
        _circulationVm = circulationVm;
        _entryVm = entryVm;
        _reportsVm = reportsVm;
        _settingsVm = settingsVm;
        _disposalVm = disposalVm;
        _documentsSearchVm = documentsSearchVm;
        
        _currentPageViewModel = _dashboardVm;
        _ = CheckDisposalAlertsAsync();
        _ = RunDailyAutoBackupAsync();
    }

    private async Task CheckDisposalAlertsAsync()
    {
        int dueCount = await _archiveService.GetTodayDisposalCountAsync();
        if (dueCount > 0)
        {
            DisposalAlertText = $"⚠️ {dueCount} record(s) are scheduled to be removed today!";
            HasDisposalAlert = true;
        }
    }

    [RelayCommand]
    private async Task GoToDisposalQueueAsync()
    {
        HasDisposalAlert = false;
        _disposalVm.SelectedTabIndex = 1;
        await NavigateToDisposalAsync();
    }

    [RelayCommand]
    private async Task NavigateToDashboardAsync()
    {
        CurrentPageViewModel = _dashboardVm; ActivePage = "Dashboard";
        await _dashboardVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToSearchAsync()
    {
        CurrentPageViewModel = _searchVm; ActivePage = "Search";
        await _searchVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToCirculationAsync()
    {
        CurrentPageViewModel = _circulationVm; ActivePage = "Circulation";
        await _circulationVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToAddFileAsync()
    {
        CurrentPageViewModel = _entryVm; ActivePage = "Entry";
        await _entryVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToReportsAsync()
    {
        CurrentPageViewModel = _reportsVm; ActivePage = "Reports";
        await _reportsVm.RefreshAsync();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPageViewModel = _settingsVm; ActivePage =  "Settings";
    }

    [RelayCommand]
    private async Task NavigateToDisposalAsync()
    {
        CurrentPageViewModel = _disposalVm; ActivePage = "Disposal";
        await _disposalVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToDocumentsAsync()
    {
        CurrentPageViewModel = _documentsSearchVm; ActivePage = "Documents";
        await _documentsSearchVm.RefreshAsync();
    }
    

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        var app = System.Windows.Application.Current;
        var dict = new System.Windows.ResourceDictionary
        {
            Source = new System.Uri(IsDarkMode ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", System.UriKind.Relative)
        };
        
        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(dict);
    }

    private async Task RunDailyAutoBackupAsync()
    {
        try
        {
            var prefs = _preferencesService.GetPreferences();

            if (prefs.AutoBackupEnabled && !string.IsNullOrWhiteSpace(prefs.AutoBackupDirectory) &&
                System.IO.Directory.Exists(prefs.AutoBackupDirectory))
            {
                string todayBackupFileName = $"ArchiveDB_AutoBackup_{DateTime.Now:yyyyMMdd}.backup";
                string fullBackupPath = System.IO.Path.Combine(prefs.AutoBackupDirectory, todayBackupFileName);

                if (!System.IO.File.Exists(fullBackupPath))
                {
                    var result = await _archiveService.BackupDatabaseAsync(fullBackupPath);
                    
                    if (!result.Success)
                    {
                        System.Windows.MessageBox.Show(
                            $"Auto-Backup Failed!\n\nDatabase Error: {result.Message}\n\n", 
                            "Backup Error", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Warning);
                    }
                }
            }
        }

        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Auto-Backup Exception: {ex.Message}", "Backup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}