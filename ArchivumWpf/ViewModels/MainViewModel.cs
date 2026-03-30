using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject _currentPageViewModel;
    [ObservableProperty] private bool _hasDisposalAlert = false;
    [ObservableProperty] private string _disposalAlertText = string.Empty;

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
    private void NavigateToDashboard() => CurrentPageViewModel = _dashboardVm;

    [RelayCommand]
    private void NavigateToSearch() => CurrentPageViewModel = _searchVm;

    [RelayCommand]
    private void NavigateToCirculation() => CurrentPageViewModel = _circulationVm;

    [RelayCommand]
    private void NavigateToAddFile() => CurrentPageViewModel = _entryVm;

    [RelayCommand]
    private void NavigateToReports() => CurrentPageViewModel = _reportsVm;

    [RelayCommand]
    private void NavigateToSettings() => CurrentPageViewModel = _settingsVm;

    [RelayCommand]
    private void NavigateToDisposal() => CurrentPageViewModel = _disposalVm;
}