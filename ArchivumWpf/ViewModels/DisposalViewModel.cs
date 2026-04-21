using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Private.Windows;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class DisposalViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IPreferencesService _preferencesService;
    
    [ObservableProperty] private string _searchRrNumber = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statusColor = "White";

    [ObservableProperty] private bool _isFileLoaded = false;
    [ObservableProperty] private string _loadedRrNumber = string.Empty;
    [ObservableProperty] private string _loadedFileName = string.Empty;
    [ObservableProperty] private string _loadedSector = string.Empty;
    [ObservableProperty] private string _loadedStatus = string.Empty;

    [ObservableProperty] private DateTime? _scheduledDate;
    
    [ObservableProperty] private string _disposalReason = string.Empty;
    [ObservableProperty] private string _authorizedBy = string.Empty;
    [ObservableProperty] private bool _canDispose = false;
    
    public ObservableCollection<FileRecord> PendingRecords { get; } = new();
    public ObservableCollection<DisposedRecord> DisposedHistory { get; } = new();

    [ObservableProperty] private bool _isDialogOpen = false;
    [ObservableProperty] private FileRecord _popupFileData;
    [ObservableProperty] private DisposedRecord _popupDisposalData;
    [ObservableProperty] private string _popupBorderColor = "#333333";
    [ObservableProperty] private int _selectedTabIndex = 0;

    [ObservableProperty] private bool _isDisposalPromptOpen = false;
    [ObservableProperty] private string _pendingDisposalRrNumber = string.Empty;

    [ObservableProperty] private FileRecord _loadedFile;
    [ObservableProperty] private string _loadedFileColor = "#8f9bb3";

    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private bool _isPendingPopup = false;
    [ObservableProperty] private bool _isDisposedPopup = false;

    private const int PageSize = 50;
    [ObservableProperty] private int _historyCurrentPage = 1;
    [ObservableProperty] private int _historyTotalPages = 1;
    [ObservableProperty] private int _historyTotalCount = 0;
    
    

    public DisposalViewModel (IArchiveService archiveService, IPreferencesService preferencesService)
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;
        _ = LoadTablesAsync();
    }

    private async Task LoadTablesAsync()
    {
        var pending = await _archiveService.GetPendingDisposalsAsync();
        PendingRecords.Clear();
        foreach (var p in pending) PendingRecords.Add(p);

        var history = await _archiveService.GetDisposedHistoryAsync();
        DisposedHistory.Clear();
        foreach (var h in history) DisposedHistory.Add(h);
    }
    

    [RelayCommand]
    private async Task SearchFileAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchRrNumber)) return;

        string cleanSearchKey = SearchRrNumber.Trim();

        var file = await _archiveService.GetFileByRrNumberAsync(cleanSearchKey);

        if (file == null)
        {
            ShowStatus($"No file found with RR Number: {cleanSearchKey}", "#F44336");
            ClearLoadedFile();
            return;
        }

        LoadedFile = file;
        LoadedFileColor = GetSectorColor(file.Sector);
        
        LoadedRrNumber = file.RrNumber;
        LoadedFileName = file.FileName;
        LoadedSector = file.Sector;
        ScheduledDate = file.ToBeRemovedDate;
        
        IsFileLoaded = true;

        if (file.IsRemoved)
        {
            ShowStatus("This file has already been disposed and is locked", "#E57373");
            CanDispose = false;
        }
        else if (file.CurrentStatus == "Borrowed")
        {
            ShowStatus("File is currently borrowed. It must be returned before disposal.", "#FF9800");
            CanDispose = false;
        }
        else
        {
            ShowStatus("File loaded. Ready for scheduling or disposal.", "#4FC3F7");
            CanDispose = true;
        }

    }

    [RelayCommand]
    private async Task UpdateQueueAsync()
    {
        if (!IsFileLoaded || !CanDispose) return;

        var result = await _archiveService.UpdateDisposalQueueAsync(LoadedRrNumber, ScheduledDate);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");
        
        await LoadTablesAsync();
    }
    
    // Replaced ExecuteDisposalAsync method with following three methods

    [RelayCommand]
    private void PromptDisposal(string rrNumber)
    {
        string targetRr = string.IsNullOrEmpty(rrNumber) ? LoadedRrNumber : rrNumber;
        if (string.IsNullOrEmpty(targetRr)) return;
        
        PendingDisposalRrNumber = targetRr;
        DisposalReason = string.Empty;
        AuthorizedBy = string.Empty;
        IsDisposalPromptOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDisposalAsync()
    {
        if (string.IsNullOrWhiteSpace(DisposalReason) || string.IsNullOrWhiteSpace(AuthorizedBy))
        {
            ShowStatus("Reason and Authorization are strictly required!", "#F44336");
            return;
        }
        
        var firstWarning = MessageBox.Show(
            $"Are you sure you want to permanently dispose of File '{PendingDisposalRrNumber}'?\n\nThis will remove it from the active vault.",
            "Confirm Disposal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (firstWarning != MessageBoxResult.Yes) return;
        
        var secondWarning = MessageBox.Show(
            "CRITICAL WARNING:\n\nDisposing this file will permanently lock it. You will no longer be able to edit its contents, issue it to borrowers, or undo this action.\n\nDo you want to proceed?", 
            "PERMANENT LOCK WARNING",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);
        
        if (secondWarning != MessageBoxResult.Yes) return;
        
        var result = await _archiveService.DisposeFileAsync(PendingDisposalRrNumber, DisposalReason, AuthorizedBy);

        if (result.Success)
        {
            ShowStatus(result.Message, "#4CAF50");
            IsDisposalPromptOpen = false;
            ClearLoadedFile();
            await LoadTablesAsync();
        }
        else
        {
            ShowStatus(result.Message, "#F44336");
        }
        
    }

    [RelayCommand]
    private void CancelDisposalPrompt()
    {
        IsDisposalPromptOpen = false;
        PendingDisposalRrNumber = string.Empty;
        DisposalReason = string.Empty;
        AuthorizedBy = string.Empty;
    }
    

    [RelayCommand]
    private async Task RecoverFileAsync(string rrNumber)
    {
        if (string.IsNullOrEmpty(rrNumber)) return;
        
        var result = await _archiveService.RecoverFileAsync(rrNumber);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");
        
        if (result.Success) await LoadTablesAsync();
    }
    
    //alert pop ups
    [RelayCommand]
    private void OpenPendingDetails(FileRecord file)
    {
        if (file == null) return;
        PopupFileData = file;
        PopupDisposalData = null;
        SetPopupColor(file.Sector);
        
        DialogTitle = $"To Be Removed Record - {file.RrNumber}";
        IsPendingPopup =  true;
        IsDisposedPopup = false;
        
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenDisposedDetails(DisposedRecord record)
    {
        if (record == null) return;
        PopupDisposalData = record;
        PopupFileData = record.File;
        SetPopupColor(record.File.Sector);
        
        DialogTitle = $"Disposed Record #{record.Id}";
        IsPendingPopup = false;
        IsDisposedPopup = true;
        
        IsDialogOpen = true;
    }

    private void SetPopupColor(string sectorName)
    {
        var prefs = _preferencesService.GetPreferences();
        var sector = prefs.Sectors.FirstOrDefault(s => s.Name == sectorName);
        PopupBorderColor = sector?.ColorHex ?? "#333333";
    }

    private string GetSectorColor(string sectorName)
    {
        var prefs = _preferencesService.GetPreferences();
        var sector = prefs.Sectors.FirstOrDefault(s => s.Name == sectorName);
        return sector?.ColorHex ?? "#8f9bb3";
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen = false;
    }
    
    


    [RelayCommand]
    private void ClearLoadedFile()
    {
        IsFileLoaded = false;
        CanDispose = false;
        LoadedFile = null;
        LoadedFileColor = "#8f9bb3";
        LoadedRrNumber = string.Empty;
        LoadedFileName = string.Empty;
        LoadedSector = string.Empty;
        LoadedStatus = string.Empty;
        ScheduledDate = null;
        DisposalReason = string.Empty;
        AuthorizedBy = string.Empty;
        SearchRrNumber = string.Empty;
    }
    
    
    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
    
}