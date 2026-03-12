using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArchivumWpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject _currentPageViewModel;

    private readonly DashboardViewModel _dashboardVm = new();
    private readonly SearchViewModel _searchVm = new();
    private readonly CirculationViewModel _circulationVm = new();
    private readonly AddFileViewModel _addFileVm = new();
    private readonly ReportsViewModel _reportsVm = new();
    private readonly SettingsViewModel _settingsVm = new();
    private readonly DisposalViewModel _disposalVm = new();

    public MainViewModel()
    {
        _currentPageViewModel = _dashboardVm;
    }
    
    [RelayCommand]
    private void NavigateToDashboard() => CurrentPageViewModel = _dashboardVm;

    [RelayCommand]
    private void NavigateToSearch() => CurrentPageViewModel = _searchVm;

    [RelayCommand]
    private void NavigateToCirculation() => CurrentPageViewModel = _circulationVm;

    [RelayCommand]
    private void NavigateToAddFile() => CurrentPageViewModel = _addFileVm;

    [RelayCommand]
    private void NavigateToReports() => CurrentPageViewModel = _reportsVm;

    [RelayCommand]
    private void NavigateToSettings() => CurrentPageViewModel = _settingsVm;

    [RelayCommand]
    private void NavigateToDisposal() => CurrentPageViewModel = _disposalVm;
}