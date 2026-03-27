using System;
using System.Threading.Tasks;
using System.Private.Windows;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class DisposalViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    
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

    public DisposalViewModel (IArchiveService archiveService)
    {
        _archiveService = archiveService;
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

        if (string.IsNullOrWhiteSpace(DisposalReason) || string.IsNullOrWhiteSpace(AuthorizedBy))
        {
            ShowStatus("Reason and Authorization are required for permanent disposal.", "#F44336");
            return;
        }

        var firstWarning = MessageBox.Show(
            $"Are you sure you want to permanently dispose of File '{LoadedRrNumber}'?\n\nThis will remove it from the active vault.",
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

        var result = await _archiveService.DisposeFileAsync(LoadedRrNumber, DisposalReason, AuthorizedBy);

        if (result.Success)
        {
            ShowStatus(result.Message, "#4CAF50");
            
            await SearchFileAsync();
            DisposalReason = string.Empty;
            AuthorizedBy = string.Empty;
        }
        else
        {
            ShowStatus(result.Message, "#F44336"); 
        }
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