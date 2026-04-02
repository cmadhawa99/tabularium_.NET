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

    public MainViewModel(
        IArchiveService archiveService,
        DashboardViewModel dashboardVm,
        SearchViewModel searchVm,
        CirculationViewModel circulationVm,
        EntryViewModel entryVm,
        ReportsViewModel reportsVm,
        SettingsViewModel settingsVm,
        DisposalViewModel disposalVm
        )
    {
        _archiveService = archiveService;
        _dashboardVm = dashboardVm;
        _searchVm = searchVm;
        _circulationVm = circulationVm;
        _entryVm = entryVm;
        _reportsVm = reportsVm;
        _settingsVm = settingsVm;
        _disposalVm = disposalVm;
        
        _currentPageViewModel = _dashboardVm;
        _ = CheckDisposalAlertsAsync();
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
    private void GoToDisposalQueue()
    {
        HasDisposalAlert = false;
        _disposalVm.SelectedTabIndex = 1;
        NavigateToDisposal();
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentPageViewModel = _dashboardVm; ActivePage = "Dashboard";
    }

    [RelayCommand]
    private void NavigateToSearch()
    {
        CurrentPageViewModel =  _searchVm; ActivePage =  "Search";
    }

    [RelayCommand]
    private void NavigateToCirculation()
    {
        CurrentPageViewModel =  _circulationVm; ActivePage =  "Circulation";
    }

    [RelayCommand]
    private void NavigateToAddFile()
    {
        CurrentPageViewModel = _entryVm; ActivePage = "Entry";
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        CurrentPageViewModel = _reportsVm; ActivePage =  "Reports";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPageViewModel = _settingsVm; ActivePage =  "Settings";
    }

    [RelayCommand]
    private void NavigateToDisposal()
    {
        CurrentPageViewModel = _disposalVm; ActivePage =  "Disposal";
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
}