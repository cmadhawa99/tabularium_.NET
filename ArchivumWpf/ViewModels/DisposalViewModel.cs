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

        string cleanSearchKeu = SearchRrNumber.Trim();

        var file = await _archiveService.GetFileByRrNumberAsync(cleanSearchKeu);

        if (file == null)
        {
            ShowStatus($"No file found with RR Number: {cleanSearchKeu}", "#F44336");
            ClearLoadedFile();
            return;
        }
        
        LoadedRrNumber = file.RrNumber;
        LoadedFileName = file.FileName;
        LoadedSector = file.Sector;
        LoadedStatus = file.IsRemoved ? "PERMANENTLY DISPOSED" : file.CurrentStatus;
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

    [RelayCommand]
    private async Task ExecuteDisposalAsync(string rrNumber)
    {
        string targetRr = string.IsNullOrEmpty(rrNumber) ? LoadedRrNumber : rrNumber;

        if (string.IsNullOrEmpty(targetRr)) return;
        
        var firstWarning = MessageBox.Show(
            $"Are you sure you want to permanently dispose of File '{targetRr}'?\n\nThis will remove it from the active vault.",
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
        
        string reason = string.IsNullOrEmpty(DisposalReason) ? "Scheduled Disposal" : DisposalReason;
        string auth = string.IsNullOrEmpty(AuthorizedBy) ? "System/Admin" : AuthorizedBy;
        
        var result = await _archiveService.DisposeFileAsync(targetRr, reason, auth);

        if (result.Success)
        {
            ShowStatus(result.Message, "#4CAF50");
            ClearLoadedFile();
            await LoadTablesAsync();
        }
        else
        {
            ShowStatus(result.Message, "#F44336");
        }
        
    }

    [RelayCommand]
    private async Task RecoverFileAsync(string rrNumber)
    {
        if (string.IsNullOrEmpty(rrNumber)) return;
        
        var result = await _archiveService.RecoverFileAsync(rrNumber);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");
        
        if (result.Success) await LoadTablesAsync();
    }
    
    //pop up 
    [RelayCommand]
    private void OpenPendingDetails(FileRecord file)
    {
        if (file == null) return;
        PopupFileData = file;
        PopupDisposalData = null;
        SetPopupColor(file.Sector);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenDisposedDetails(DisposedRecord record)
    {
        if (record == null) return;
        PopupDisposalData = record;
        PopupFileData = record.File;
        SetPopupColor(record.File.Sector);
        IsDialogOpen = true;
    }

    private void SetPopupColor(string sectorName)
    {
        var prefs = _preferencesService.GetPreferences();
        var sector = prefs.Sectors.FirstOrDefault(s => s.Name == sectorName);
        PopupBorderColor = sector?.ColorHex ?? "#333333";
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
        LoadedRrNumber = string.Empty;
        LoadedFileName = string.Empty;
        LoadedSector = string.Empty;
        LoadedStatus = string.Empty;
        ScheduledDate = null;
        DisposalReason = string.Empty;
        AuthorizedBy = string.Empty;
    }
    
    
    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
    
}