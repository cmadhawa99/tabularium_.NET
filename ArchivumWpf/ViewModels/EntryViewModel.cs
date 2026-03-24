using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ArchivumWpf.Models;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class EntryViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IPreferencesService _preferencesService;

    public ObservableCollection<string> AvailableSectors { get; } = new();
    public ObservableCollection<string> AvailableFileTypes { get; } = new();

    public ObservableCollection<EntryHistoryRecord> HistoryRecords { get; } = new();
    
    //Add entry properties
    [ObservableProperty] private string _addRrNumber = string.Empty;
    [ObservableProperty] private string _addSector = string.Empty;
    [ObservableProperty] private string _addSubjectNumber = string.Empty;
    [ObservableProperty] private string _addFileName = string.Empty;
    [ObservableProperty] private string _addFileType = string.Empty;
    [ObservableProperty] private DateTime? _addStartDate;
    [ObservableProperty] private DateTime? _addEndDate;
    [ObservableProperty] private string _addTotalPages = string.Empty; // Using string for UI input, parse to int later
    [ObservableProperty] private string _addShelfNumber = string.Empty;
    [ObservableProperty] private string _addDeckNumber = string.Empty;
    [ObservableProperty] private string _addFileNumber = string.Empty;
    
    //Edit entry properties
    [ObservableProperty] private string _searchRrNumber = string.Empty;
    [ObservableProperty] private bool _isEditFormEnabled = false;
    
    private FileRecord _currentEditingFile;
    
    [ObservableProperty] private string _editSector = string.Empty;
    [ObservableProperty] private string _editSubjectNumber = string.Empty;
    [ObservableProperty] private string _editFileName = string.Empty;
    [ObservableProperty] private string _editFileType = string.Empty;
    [ObservableProperty] private DateTime? _editStartDate;
    [ObservableProperty] private DateTime? _editEndDate;
    [ObservableProperty] private string _editTotalPages = string.Empty;
    [ObservableProperty] private string _editShelfNumber = string.Empty;
    [ObservableProperty] private string _editDeckNumber = string.Empty;
    [ObservableProperty] private string _editFileNumber = string.Empty;
    
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statusColor = "White";

    public EntryViewModel(IArchiveService archiveService, IPreferencesService preferencesService)
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;
        LoadDropdowns();
        _ = LoadHistoryAsync();
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            LoadDropdowns();
        });
        
    }

    private void LoadDropdowns()
    {
        var prefs = _preferencesService.GetPreferences();
        AvailableSectors.Clear();
        foreach (var s in prefs.Sectors) AvailableSectors.Add(s.Name);
        AvailableFileTypes.Clear();
        foreach (var t in prefs.FileTypes) AvailableFileTypes.Add(t);
    }

    private async Task LoadHistoryAsync()
    {
        var records = await _archiveService.GetEntryHistoryAsync();
        HistoryRecords.Clear();
        foreach (var r in records) HistoryRecords.Add(r);
    }
    
    
    //Add tab
    
    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (string.IsNullOrWhiteSpace(AddRrNumber) || string.IsNullOrWhiteSpace(AddFileName) ||
            string.IsNullOrWhiteSpace(AddSector))
        {
            ShowStatus("RR Number, File Name and Sector are required fields", "#F44336");
            return;
        }

        var newRecord = new FileRecord()
        {
            RrNumber = this.AddRrNumber,
            Sector = this.AddSector,
            SubjectNumber = string.IsNullOrWhiteSpace(this.AddSubjectNumber) ? null : this.AddSubjectNumber,
            FileName = this.AddFileName,
            FileType = string.IsNullOrWhiteSpace(this.AddFileType) ? null : this.AddFileType,
            StartDate = this.AddStartDate,
            EndDate = this.AddEndDate,
            
            TotalPages = int.TryParse(this.AddTotalPages, out int tp) ? tp : null,
            ShelfNumber = int.TryParse(this.AddShelfNumber, out int sn) ? sn : null,
            DeckNumber = int.TryParse(this.AddDeckNumber, out int dn) ? dn : null,
            FileNumber = int.TryParse(this.AddFileNumber, out int fn) ? fn : null
        };

        var result = await _archiveService.AddNewFileAsync(newRecord);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");

        if (result.Success)
        {
            ClearAddForm();
            _ = LoadHistoryAsync();
        }

    }

    [RelayCommand]
    private void ClearAddForm()
    {
        AddRrNumber = string.Empty;
        AddSector = string.Empty;
        AddSubjectNumber = string.Empty;
        AddFileName = string.Empty;
        AddFileType = string.Empty;
        AddStartDate = null;
        AddEndDate = null;
        AddTotalPages = string.Empty;
        AddShelfNumber = string.Empty;
        AddDeckNumber = string.Empty;
        AddFileNumber = string.Empty;
    }
    
    
    //Edit tab

    [RelayCommand]
    private async Task SearchFileToEditAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchRrNumber)) return;

        _currentEditingFile = await _archiveService.GetFileByRrNumberAsync(SearchRrNumber);

        if (_currentEditingFile == null)
        {
            ShowStatus($"No file found with RR Number: {SearchRrNumber}", "#F44336");
            IsEditFormEnabled = false;
            return;
        }
        
        EditSector = _currentEditingFile.Sector;
        EditSubjectNumber = _currentEditingFile.SubjectNumber;
        EditFileName = _currentEditingFile.FileName;
        EditFileType = _currentEditingFile.FileType;
        EditStartDate = _currentEditingFile.StartDate;
        EditEndDate = _currentEditingFile.EndDate;
        EditTotalPages = _currentEditingFile.TotalPages?.ToString() ?? "";
        EditShelfNumber = _currentEditingFile.ShelfNumber?.ToString() ?? "";
        EditDeckNumber = _currentEditingFile.DeckNumber?.ToString() ?? "";
        EditFileNumber = _currentEditingFile.FileNumber?.ToString() ?? "";
        
        IsEditFormEnabled = true;
        ShowStatus($"File {SearchRrNumber} loaded. Ready to edit.", "#4FC3F7");
    }

    [RelayCommand]
    private async Task UpdateFileAsync()
    {
        if (_currentEditingFile == null || !IsEditFormEnabled) return;
        if (string.IsNullOrWhiteSpace(EditFileName) || string.IsNullOrWhiteSpace(EditSector))
        {
            ShowStatus("File Name and Sector cannot be empty.", "#F44336");
            return;
        }
        
        _currentEditingFile.Sector = EditSector;
        _currentEditingFile.SubjectNumber = string.IsNullOrWhiteSpace(EditSubjectNumber) ? null : EditSubjectNumber;
        _currentEditingFile.FileName = EditFileName;
        _currentEditingFile.FileType = string.IsNullOrWhiteSpace(EditFileType) ? null : EditFileType;
        _currentEditingFile.StartDate = EditStartDate;
        _currentEditingFile.EndDate = EditEndDate;
        _currentEditingFile.TotalPages = int.TryParse(EditTotalPages, out int tp) ? tp : null;
        _currentEditingFile.ShelfNumber = int.TryParse(EditShelfNumber, out int sn) ? sn : null;
        _currentEditingFile.DeckNumber = int.TryParse(EditDeckNumber, out int dn) ? dn : null;
        _currentEditingFile.FileNumber = int.TryParse(EditFileNumber, out int fn) ? fn : null;
        
        var result = await _archiveService.UpdateFileAsync(_currentEditingFile);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");

        if (result.Success)
        {
            ClearEditForm();
            _ = LoadHistoryAsync();
        }
        
    }

    [RelayCommand]
    private void ClearEditForm()
    {
        SearchRrNumber = string.Empty;
        IsEditFormEnabled = false;
        _currentEditingFile = null;
        EditSector = string.Empty; 
        EditSubjectNumber = string.Empty; 
        EditFileName = string.Empty;
        EditFileType = string.Empty; 
        EditStartDate = null; 
        EditEndDate = null;
        EditTotalPages = string.Empty; 
        EditShelfNumber = string.Empty; 
        EditDeckNumber = string.Empty; 
        EditFileNumber = string.Empty;
    }
    

    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}