using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
    
namespace ArchivumWpf.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private const int PageSize = 50;
    
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty] 
    private ObservableCollection<FileRecord> _searchResults = new();

    [ObservableProperty] 
    private bool _isSearching;
    
    //Pagination
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalResultsCount = 0;

    public SearchViewModel(IArchiveService archiveService)
    {
        _archiveService = archiveService;

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task PerformSearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadDataAsync();
        }
    }


    private async Task LoadDataAsync()
    {
        var result = await _archiveService.SearchFilesPaginatedAsync(SearchQuery, CurrentPage, PageSize);
        
        TotalResultsCount = result.TotalCount;
        TotalPages = (int)Math.Ceiling((double)TotalResultsCount / PageSize);
        if (TotalPages == 0) TotalPages = 1;
        
        SearchResults.Clear();
        foreach (var file in result.Items)
        {
            SearchResults.Add(file);
        }
        
        IsSearching = false;
    }
}