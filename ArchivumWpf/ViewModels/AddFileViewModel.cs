using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class AddFileViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    
    [ObservableProperty] private string _rrNumber = string.Empty;
    [ObservableProperty] private string _sector = string.Empty;
    [ObservableProperty] private string _subjectNumber = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _fileType = string.Empty;

    [ObservableProperty] private DateTime? _startDate;
    [ObservableProperty] private DateTime? _endDate;
    
    [ObservableProperty] private string _totalPages = string.Empty; // Using string for UI input, parse to int later
    
    [ObservableProperty] private string _shelfNumber = string.Empty;
    [ObservableProperty] private string _deckNumber = string.Empty;
    [ObservableProperty] private string _fileNumber = string.Empty;
    
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statusColor = "White";

    public AddFileViewModel(IArchiveService archiveService)
    {
        _archiveService = archiveService;
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (string.IsNullOrWhiteSpace(RrNumber) || string.IsNullOrWhiteSpace(FileName) ||
            string.IsNullOrWhiteSpace(Sector))
        {
            ShowStatus("RR Number, File Name and Sector are required fields", "#F44336");
            return;
        }

        var newRecord = new FileRecord()
        {
            RrNumber = this.RrNumber,
            Sector = this.Sector,
            SubjectNumber = string.IsNullOrWhiteSpace(this.SubjectNumber) ? null : this.SubjectNumber,
            FileName = this.FileName,
            FileType = string.IsNullOrWhiteSpace(this.FileType) ? null : this.FileType,
            StartDate = this.StartDate,
            EndDate = this.EndDate,

            TotalPages = int.TryParse(this.TotalPages, out int tp) ? tp : null,
            ShelfNumber = int.TryParse(this.ShelfNumber, out int sn) ? sn : null,
            DeckNumber = int.TryParse(this.DeckNumber, out int dn) ? dn : null,
            FileNumber = int.TryParse(this.FileNumber, out int fn) ? fn : null
        };

        var result = await _archiveService.AddNewFileAsync(newRecord);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");

        if (result.Success)
        {
            ClearForm();
        }

    }

    [RelayCommand]
    private void ClearForm()
    {
        RrNumber = string.Empty;
        Sector = string.Empty;
        SubjectNumber = string.Empty;
        FileName = string.Empty;
        FileType  = string.Empty;
        StartDate = null;
        EndDate = null;
        TotalPages = string.Empty;
        ShelfNumber  = string.Empty;
        DeckNumber  = string.Empty;
        FileNumber  = string.Empty;
    }

    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}