using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using DocumentFormat.OpenXml.Bibliography;

namespace ArchivumWpf.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IPreferencesService  _preferencesService;
    private const int PageSize = 50;
    
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private ObservableCollection<FileRecord> _searchResults = new();
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private FileRecord? _selectedFile;
    [ObservableProperty] private bool _isDetailsOpen;

    public ObservableCollection<string> AvailableSectors { get; } = new();
    public ObservableCollection<string> AvailableYears { get; } = new();
    public ObservableCollection<string> AvailableMonths { get; } = new()
    { "Any Month", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    
    [ObservableProperty] private string _selectedSector = "All Sectors";
    [ObservableProperty] private string _selectedYear = "Any Year";
    [ObservableProperty] private string _selectedMonth = "Any Month";

    [ObservableProperty] private bool _isRecentActive;
    [ObservableProperty] private bool _isAvailableActive;
    [ObservableProperty] private bool _isRemovedActive;
    [ObservableProperty] private bool _isStrictRrSearch;
    
    //Pagination
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalResultsCount = 0;
    
    //UI
    [ObservableProperty] private string _popupBorderColor = "#f2ca50";
    
    

    public SearchViewModel(IArchiveService archiveService, IPreferencesService preferencesService)
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;

        //_ = LoadDataAsync();
        _ = IntializeViewModelAsync();
    }

    private async Task IntializeViewModelAsync()
    {
        await LoadFiltersAsync();
        await LoadDataAsync();
    }

    private async Task LoadFiltersAsync()
    {
        var dbSectors = await _archiveService.GetExistingSectorsAsync();
        AvailableSectors.Clear();
        AvailableSectors.Add("All Sectors");
        foreach (var s in dbSectors) AvailableSectors.Add(s);
        
        AvailableYears.Clear();
        AvailableYears.Add("Any Year");
        for (int y = DateTime.Now.Year; y >= 2010; y--) AvailableYears.Add(y.ToString());
    }
    
    partial void OnSearchQueryChanged(string value) { CurrentPage = 1; _ = LoadDataAsync(); }
    partial void OnSelectedSectorChanged(string value) { CurrentPage = 1; _ = LoadDataAsync(); }
    partial void OnSelectedYearChanged(string value) { CurrentPage = 1; _ = LoadDataAsync(); }
    partial void OnSelectedMonthChanged(string value) { CurrentPage = 1; _ = LoadDataAsync(); }
    partial void OnIsRecentActiveChanged(bool value) { CurrentPage = 1; _ = LoadDataAsync(); }
    partial void OnIsAvailableActiveChanged(bool value) { CurrentPage = 1; _ = LoadDataAsync(); }
    partial void OnIsRemovedActiveChanged(bool value) { CurrentPage = 1; _ = LoadDataAsync(); }
    

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

    [RelayCommand]
    private void OpenDetails()
    {
        if (SelectedFile != null)
        {
            var prefs = _preferencesService.GetPreferences();
            var sector = prefs.Sectors.FirstOrDefault(s => s.Name == SelectedFile.Sector);
            PopupBorderColor = sector?.ColorHex ?? "#f2ca50";
            IsDetailsOpen = true;
        }
    }

    [RelayCommand]
    private void CloseDetails()
    {
        IsDetailsOpen = false;
    }


    private async Task LoadDataAsync()
    {
        IsSearching = true;

        int? parsedYear = null;
        if (SelectedYear != "Any Year" && int.TryParse(SelectedYear, out int y))
        {
            parsedYear = y;
        }

        int? parsedMonth = null;
        if (SelectedMonth != "Any Month")
        {
            parsedMonth = DateTime.ParseExact(SelectedMonth, "MMMM", CultureInfo.InvariantCulture).Month;
        }


        var result = await _archiveService.SearchFilesPaginatedAsync(
            SearchQuery,
            SelectedSector,
            parsedYear,
            parsedMonth,
            IsRecentActive,
            IsAvailableActive,
            IsRemovedActive,
            IsStrictRrSearch,
            CurrentPage,
            PageSize);
        
        TotalResultsCount = result.TotalCount;
        TotalPages = (int)Math.Ceiling((double)TotalResultsCount / PageSize);
        if (TotalPages == 0) TotalPages = 1;
        
        var prefs = _preferencesService.GetPreferences();
        var colorMap = prefs.Sectors.ToDictionary(s => s.Name, s => s.ColorHex);
        
        SearchResults.Clear();
        foreach (var file in result.Items)
        {
            file.SectorColorHex = colorMap.ContainsKey(file.Sector) ? colorMap[file.Sector] : "#8f9bb3";
            SearchResults.Add(file);
        }
        
        IsSearching = false;
    }

    partial void OnIsStrictRrSearchChanged(bool value)
    {
        CurrentPage = 1; _ = LoadDataAsync();
    }
    
}