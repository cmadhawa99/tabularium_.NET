using System.IO;
using System.Windows;
using ArchivumWpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Strings = ArchivumWpf.Localization.Strings;

namespace ArchivumWpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly CirculationViewModel _circulationVm;
    private readonly DashboardViewModel _dashboardVm;
    private readonly DisposalViewModel _disposalVm;
    private readonly DocumentsSearchViewModel _documentsSearchVm;
    private readonly EntryViewModel _entryVm;

    private readonly IPreferencesService _preferencesService;
    private readonly ReportsViewModel _reportsVm;
    private readonly SearchViewModel _searchVm;
    private readonly SettingsViewModel _settingsVm;

    [ObservableProperty] private string _activePage = "Dashboard";
    [ObservableProperty] private ObservableObject _currentPageViewModel;
    [ObservableProperty] private string _disposalAlertText = string.Empty;
    [ObservableProperty] private bool _hasDisposalAlert;
    [ObservableProperty] private bool _isDarkMode = true;

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
        var dueCount = await _archiveService.GetTodayDisposalCountAsync();
        if (dueCount > 0)
        {
            DisposalAlertText = $"⚠️ {string.Format(Strings.Main_DisposalAlertFormat, dueCount)}";
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
        CurrentPageViewModel = _dashboardVm;
        ActivePage = "Dashboard";
        await _dashboardVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToSearchAsync()
    {
        CurrentPageViewModel = _searchVm;
        ActivePage = "Search";
        await _searchVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToCirculationAsync()
    {
        CurrentPageViewModel = _circulationVm;
        ActivePage = "Circulation";
        await _circulationVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToAddFileAsync()
    {
        CurrentPageViewModel = _entryVm;
        ActivePage = "Entry";
        await _entryVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToReportsAsync()
    {
        CurrentPageViewModel = _reportsVm;
        ActivePage = "Reports";
        await _reportsVm.RefreshAsync();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPageViewModel = _settingsVm;
        ActivePage = "Settings";
    }

    [RelayCommand]
    private async Task NavigateToDisposalAsync()
    {
        CurrentPageViewModel = _disposalVm;
        ActivePage = "Disposal";
        await _disposalVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task NavigateToDocumentsAsync()
    {
        CurrentPageViewModel = _documentsSearchVm;
        ActivePage = "Documents";
        await _documentsSearchVm.RefreshAsync();
    }


    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        var app = Application.Current;
        var dict = new ResourceDictionary
        {
            Source = new Uri(IsDarkMode ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative)
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
                Directory.Exists(prefs.AutoBackupDirectory))
            {
                var todayBackupFileName = $"ArchiveDB_AutoBackup_{DateTime.Now:yyyyMMdd}.backup";
                var fullBackupPath = Path.Combine(prefs.AutoBackupDirectory, todayBackupFileName);

                if (!File.Exists(fullBackupPath))
                {
                    var result = await _archiveService.BackupDatabaseAsync(fullBackupPath);

                    if (!result.Success)
                        MessageBox.Show(
                            $"Auto-Backup Failed!\n\nDatabase Error: {result.Message}\n\n",
                            "Backup Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                }
            }
        }

        catch (Exception ex)
        {
            MessageBox.Show($"Auto-Backup Exception: {ex.Message}", "Backup Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}