using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArchivumWpf.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IPreferencesService _preferencesService;
    
    [ObservableProperty] private int _totalHoldings;
    [ObservableProperty] private int _activeLoans;
    [ObservableProperty] private int _archivedPurged;

    public ObservableCollection<ActivityLog> RecentActivities { get; } = new();

    [ObservableProperty] private int _pageSize;
    [ObservableProperty] private bool _isActivityLogDialogOpen = false;
    [ObservableProperty] private string _activityLogSearchQuery = string.Empty;

    public ObservableCollection<ActivityLog> AllActivityLogs { get; } = new();


    [ObservableProperty] private int _activityLogCurrentPage = 1;
    [ObservableProperty] private int _activityLogTotalPages = 1;
    [ObservableProperty] private int _activityLogTotalCount = 0;
    
    public DashboardViewModel(IArchiveService archiveService, IPreferencesService preferencesService)
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;
        
        PageSize = _preferencesService.GetPreferences().DefaultPaginationSize;
        
        _ = LoadStatsAsync();
        
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            PageSize = _preferencesService.GetPreferences().DefaultPaginationSize;
            ActivityLogCurrentPage = 1;
            if (IsActivityLogDialogOpen)
            {
                _ = LoadAllActivitiesAsync();
            }
        });
        
    }

    private async Task LoadStatsAsync()
    {
        var stats = await _archiveService.GetDashboardStatsAsync();
        
        TotalHoldings = stats.TotalHoldings;
        ActiveLoans = stats.ActiveLoans;
        ArchivedPurged = stats.ArchivedPurged;
        
        var activitites = await _archiveService.GetRecentActivitiesAsync(15);
        RecentActivities.Clear();
        foreach (var activity in activitites)
        {
            RecentActivities.Add(activity);
        }
    }

    [RelayCommand]
    private async Task OpenActivityLogDialogAsync()
    {
        ActivityLogSearchQuery = string.Empty;
        ActivityLogCurrentPage = 1;
        await LoadAllActivitiesAsync();
        IsActivityLogDialogOpen = true;
    }

    [RelayCommand]
    private void CloseActivityLogDialog()
    {
        IsActivityLogDialogOpen = false;
    }

    partial void OnActivityLogSearchQueryChanged(string value)
    {
        ActivityLogCurrentPage = 1;
        _ = LoadAllActivitiesAsync();
    }

    private async Task LoadAllActivitiesAsync()
    {
        var result = await _archiveService.GetActivityLogsPaginatedAsync(ActivityLogSearchQuery, ActivityLogCurrentPage, PageSize);
        
        
        ActivityLogTotalCount = result.TotalCount;
        ActivityLogTotalPages = (int)Math.Ceiling((double)ActivityLogTotalCount / PageSize);
        if (ActivityLogTotalPages == 0) ActivityLogTotalPages = 1;
        
        AllActivityLogs.Clear();
        foreach (var log in result.Items)
        {
            AllActivityLogs.Add(log);
        }
    }

    [RelayCommand]
    private async Task NextActivityLogPageAsync()
    {
        if (ActivityLogCurrentPage < ActivityLogTotalPages)
        {
            ActivityLogCurrentPage++;
            await LoadAllActivitiesAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousActivityLogPageAsync()
    {
        if (ActivityLogCurrentPage > 1)
        {
            ActivityLogCurrentPage--;
            await LoadAllActivitiesAsync();  
        }
    }
    
    
}