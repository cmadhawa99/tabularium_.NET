using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class CirculationViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IPreferencesService _preferencesService;
    private const int PageSize = 50;
    
    [ObservableProperty] private string _targetRrNumber = string.Empty;
    [ObservableProperty] private FileRecord _loadedFile;
    [ObservableProperty] private string _loadedFileColor = "#f2ca50";
    [ObservableProperty] private bool _isFileLoaded = false;

    [ObservableProperty] private bool _canIssue = false;
    [ObservableProperty] private bool _canReturn = false;
    [ObservableProperty] private string _borrowerName = string.Empty;
    
    public ObservableCollection<BorrowRecord> ActiveLoans { get; } = new();

    public ObservableCollection<BorrowRecord> BorrowHistoryRecords { get; } = new();
    [ObservableProperty] private string _historySearchQuery = string.Empty;
    [ObservableProperty] private int _historyCurrentPage = 1;
    [ObservableProperty] private int _historyTotalPages = 1;
    [ObservableProperty] private int _historyTotalCount = 0;
    
    [ObservableProperty] private BorrowRecord _selectedHistoryRecord;
    [ObservableProperty] private bool _isDialogOpen = false;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _popupBorderColor = "#f2ca50";
    
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statusColor = "White";
    
    
    public CirculationViewModel(IArchiveService archiveService, IPreferencesService preferencesService)
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;

        _ = LoadActiveLoansAsync();
        _ = LoadHistoryAsync();
    }

    private async Task LoadActiveLoansAsync()
    {
        var loans = await _archiveService.GetActiveLoansAsync();
        ActiveLoans.Clear();
        foreach (var loan in loans) ActiveLoans.Add(loan);
    }

    [RelayCommand]
    private async Task CheckFileAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetRrNumber)) return;

        LoadedFile = await _archiveService.GetFileByRrNumberAsync(TargetRrNumber);

        if (LoadedFile == null)
        {
            ShowStatus($"No record found with RR Number: {TargetRrNumber}", "#F44336");
            IsFileLoaded  = false;
            CanIssue = false;
            CanReturn = false;
            return;
        }

        IsFileLoaded = true;
        CanIssue = LoadedFile.CurrentStatus == "Available" && !LoadedFile.IsRemoved;
        CanReturn = LoadedFile.CurrentStatus == "Borrowed";
        
        var prefs = _preferencesService.GetPreferences();
        var sectorInfo = prefs.Sectors.FirstOrDefault(s => s.Name == LoadedFile.Sector);
        LoadedFileColor = sectorInfo?.ColorHex ?? "#f2ca50";
        
        ShowStatus($"Dossier {TargetRrNumber} secured. Awaiting directive.", "#4FC3F7");
    }

    [RelayCommand]
    private async Task ExecuteIssueAsync()
    {
        if (!CanIssue || string.IsNullOrWhiteSpace(BorrowerName))
        {
            ShowStatus("Please provide a valid Borrower Name.", "#F44336");
            return;
        }

        var result = await _archiveService.IssueFileAsync(LoadedFile.RrNumber, BorrowerName);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");

        if (result.Success)
        {
            ClearActionDesk();
            _ = LoadActiveLoansAsync();
            _ = LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task ExecuteReturnAsync()
    {
        if (!CanReturn) return;

        var result = await _archiveService.ReturnFileASync(LoadedFile.RrNumber);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");

        if (result.Success)
        {
            ClearActionDesk();
            _ = LoadActiveLoansAsync();
            _ =  LoadHistoryAsync();
        }
    }
    

    [RelayCommand]
    private void ClearActionDesk()
    {
        TargetRrNumber = string.Empty;
        LoadedFile = null;
        IsFileLoaded = false;
        CanIssue = false;
        CanReturn = false;
        BorrowerName = string.Empty;

    }

    partial void OnHistorySearchQueryChanged(string value)
    {
        HistoryCurrentPage = 1;
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        var result = await _archiveService.GetBorrowHistoryPaginatedAsync(HistorySearchQuery, HistoryCurrentPage, PageSize);
        
        HistoryTotalCount = result.TotalCount;
        HistoryTotalPages = (int)Math.Ceiling((double)HistoryTotalCount/PageSize);
        if (HistoryTotalPages == 0) HistoryTotalPages = 1;
        
        BorrowHistoryRecords.Clear();
        foreach (var record in result.Items)
        {
            BorrowHistoryRecords.Add(record);
        }
    }


    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (HistoryCurrentPage < HistoryTotalPages)
        {
            HistoryCurrentPage++;
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HistoryCurrentPage > 1)
        {
            HistoryCurrentPage--;
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private void ShowHistoryDetails()
    {
        if (SelectedHistoryRecord == null) return;
        
        var prefs = _preferencesService.GetPreferences();
        var sectorInfo = prefs.Sectors.FirstOrDefault(s => s.Name == SelectedHistoryRecord.File?.Sector);
        PopupBorderColor = sectorInfo?.ColorHex ?? "#f2ca50";

        DialogTitle = $"Loan Record: {SelectedHistoryRecord.SnapshotRrNumber}";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen =  false;
    }
    
    
    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }

}