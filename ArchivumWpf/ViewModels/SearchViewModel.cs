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
    
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty] 
    private ObservableCollection<FileRecord> _searchResults = new();

    [ObservableProperty] 
    private bool _isSearching;

    public SearchViewModel(IArchiveService archiveService)
    {
        _archiveService = archiveService;
    }

    [RelayCommand]
    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }
        
        IsSearching = true;

        var results = await _archiveService.SearchFilesAsync(SearchQuery);
        
        SearchResults.Clear();
        foreach (var file in results)
        {
            SearchResults.Add(file);
        }
        
        IsSearching = false;
    }
}