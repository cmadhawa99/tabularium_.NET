using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using ArchivumWpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace ArchivumWpf.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IPreferencesService _preferencesService;


    //Pagination
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private bool _isAvailableActive;
    [ObservableProperty] private bool _isBorrowedActive;
    [ObservableProperty] private bool _isDetailsOpen;

    [ObservableProperty] private bool _isRecentActive;
    [ObservableProperty] private bool _isRemovedActive;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private int _pageSize;

    //UI
    [ObservableProperty] private string _popupBorderColor = "#f2ca50";

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private ObservableCollection<FileRecord> _searchResults = new();
    [ObservableProperty] private FileRecord? _selectedFile;
    [ObservableProperty] private string _selectedMonth = "Any Month";

    [ObservableProperty] private string _selectedSector = "All Sectors";
    [ObservableProperty] private string _selectedYear = "Any Year";
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalResultsCount;


    public SearchViewModel(IArchiveService archiveService, IPreferencesService preferencesService)
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;

        PageSize = _preferencesService.GetPreferences().DefaultPaginationSize;

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            PageSize = _preferencesService.GetPreferences().DefaultPaginationSize;
            CurrentPage = 1;
            _ = LoadDataAsync();
        });

        _ = IntializeViewModelAsync();
    }

    public ObservableCollection<string> AvailableSectors { get; } = new();
    public ObservableCollection<string> AvailableYears { get; } = new();

    public ObservableCollection<string> AvailableMonths { get; } = new()
    {
        "Any Month", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October",
        "November", "December"
    };

    private async Task IntializeViewModelAsync()
    {
        await LoadFiltersAsync();
        await LoadDataAsync();
    }

    public async Task RefreshAsync()
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
        for (var y = DateTime.Now.Year; y >= 2010; y--) AvailableYears.Add(y.ToString());
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnSelectedSectorChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnSelectedYearChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnSelectedMonthChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnIsRecentActiveChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnIsAvailableActiveChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnIsBorrowedActiveChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnIsRemovedActiveChanged(bool value)
    {
        CurrentPage = 1;
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
        if (SelectedYear != "Any Year" && int.TryParse(SelectedYear, out var y)) parsedYear = y;

        int? parsedMonth = null;
        if (SelectedMonth != "Any Month")
            parsedMonth = DateTime.ParseExact(SelectedMonth, "MMMM", CultureInfo.InvariantCulture).Month;


        var result = await _archiveService.SearchFilesPaginatedAsync(
            SearchQuery,
            SelectedSector,
            parsedYear,
            parsedMonth,
            IsRecentActive,
            IsAvailableActive,
            IsBorrowedActive,
            IsRemovedActive,
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

    [RelayCommand]
    private void OpenDocumentManager()
    {
        if (SelectedFile == null) return;

        var app = (App)Application.Current;
        var window = app.Services.GetRequiredService<DocumentManagerWindow>();
        var vm = (DocumentManagerViewModel)window.DataContext;

        _ = vm.InitializeAsync(SelectedFile.SerialNumber, SelectedFile.RrNumber, SelectedFile.FileName, false);

        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }
}