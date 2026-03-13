using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    
    [ObservableProperty] private int _totalHoldings;
    [ObservableProperty] private int _activeLoans;
    [ObservableProperty] private int _archivedPurged;
    
    public DashboardViewModel(IArchiveService archiveService)
    {
        _archiveService = archiveService;
        
        _ = LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        var stats = await _archiveService.GetDashboardStatsAsync();
        
        TotalHoldings = stats.TotalHoldings;
        ActiveLoans = stats.ActiveLoans;
        ArchivedPurged = stats.ArchivedPurged;
    }
}