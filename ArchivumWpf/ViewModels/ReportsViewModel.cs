using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Services;
using ClosedXML.Excel;

namespace ArchivumWpf.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;

    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _rrNumber = string.Empty;
    [ObservableProperty] private string _sector = string.Empty;
    [ObservableProperty] private string _subjectNumber = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _fileType = string.Empty;
    [ObservableProperty] private DateTime? _startDate;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private string _totalPages = string.Empty;
    [ObservableProperty] private string _shelfNumber = string.Empty;
    [ObservableProperty] private string _deckNumber = string.Empty;
    [ObservableProperty] private string _fileNumber = string.Empty;

    [ObservableProperty] private string _statusMessage = "Leave all fields blank to export the entire database.";
    [ObservableProperty] private string _statusColor = "White";
    [ObservableProperty] private bool _isProcessing = false;
    
    public ReportsViewModel (IArchiveService archiveService)
    {
        _archiveService = archiveService;
    }

    [RelayCommand]
    private async Task ExportCscAsync()
    {
        var dialog = new SaveFileDialog {Filter = "CSV File (*.csv)|*.csv", FileName = $"Vault_Export_{DateTime.Now:yyyyMMdd}.csv"};
        if (dialog.ShowDialog() !=  true) return;
        
        IsProcessing = true;
        ShowStatus("Generating CSV...", "Yellow");
        
        var data = await _archiveService.GetFilteredFilesForExportAsync(SerialNumber, RrNumber, Sector, SubjectNumber, FileName, FileType, StartDate, EndDate, TotalPages, ShelfNumber, DeckNumber, FileNumber);
        
        var csv = new StringBuilder();
        csv.AppendLine(
            "Serial Number,RR Number,Sector,Subject Number,File Name,File Type,Start Date,End Date,Total Pages,Shelf,Deck,File Number,Status");

        foreach (var file in data)
        {
            string cleanName = file.FileName?.Replace(",", "") ?? "";
            
            csv.AppendLine($"{file.SerialNumber},{file.RrNumber},{file.Sector},{file.SubjectNumber},{cleanName},{file.FileType},{file.StartDate:yyyy-MM-dd},{file.EndDate:yyyy-MM-dd},{file.TotalPages},{file.ShelfNumber},{file.DeckNumber},{file.FileNumber},{file.CurrentStatus}");
        }
        
        await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
        ShowStatus($"Successfully exported {data.Count} records to CSV.", "#4CAF50");
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        var dialog = new SaveFileDialog { Filter = "Excel File (*.xlsx)|*.xlsx", FileName = $"Vault_Export_{DateTime.Now:yyyyMMdd}.xlsx" };
        if (dialog.ShowDialog() != true) return;
        
        IsProcessing = true;
        ShowStatus("Generating Excel file...", "Yellow");
        
        var data = await _archiveService.GetFilteredFilesForExportAsync(SerialNumber, RrNumber, Sector, SubjectNumber, FileName, FileType, StartDate, EndDate, TotalPages, ShelfNumber, DeckNumber, FileNumber);


        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Vault Records");
            
            worksheet.Cell(1, 1).Value = "Serial Number";
            worksheet.Cell(1, 2).Value = "RR Number";
            worksheet.Cell(1, 3).Value = "Sector";
            worksheet.Cell(1, 4).Value = "File Name";
            worksheet.Cell(1, 5).Value = "Status";

            for (int i = 0; i < data.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = data[i].SerialNumber;
                worksheet.Cell(row, 2).Value = data[i].RrNumber;
                worksheet.Cell(row, 3).Value = data[i].Sector;
                worksheet.Cell(row, 4).Value = data[i].FileName;
                worksheet.Cell(row, 5).Value = data[i].CurrentStatus;
            }
            
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
        }
        
        ShowStatus($"Successfully exported {data.Count} records to Excel.", "#4CAF50");
        IsProcessing = false;
    }

    [RelayCommand]
    private async Task BackupSqlAsync()
    {
        var dialog = new SaveFileDialog { Filter = "SQL Backup File (*.sql)|*.sql", FileName = $"Full_Backup_{DateTime.Now:yyyyMMdd}.sql" };
        if (dialog.ShowDialog() != true) return;

        IsProcessing = true;
        ShowStatus("Running SQL Backup (This may take a moment)...", "Yellow");
        
        var result = await _archiveService.BackupDatabaseAsync(dialog.FileName);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F4436");
        IsProcessing  = false;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SerialNumber = string.Empty; RrNumber = string.Empty; Sector = string.Empty;
        SubjectNumber = string.Empty; FileName = string.Empty; FileType = string.Empty;
        StartDate = null; EndDate = null; TotalPages = string.Empty;
        ShelfNumber = string.Empty; DeckNumber = string.Empty; FileNumber = string.Empty;
        ShowStatus("Filters cleared. Ready for full database export.", "Gray");
    }
    

    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}    