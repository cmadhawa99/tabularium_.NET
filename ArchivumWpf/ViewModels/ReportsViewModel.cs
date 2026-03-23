using System;
using System.Private.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ArchivumWpf.Services;
using ArchivumWpf.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;

namespace ArchivumWpf.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    private readonly IPreferencesService _preferencesService;

    public ObservableCollection<string> AvailableSectors { get; } = new();
    public ObservableCollection<string> AvailableFileTypes { get; } = new();
    public ObservableCollection<SectorItem> RawSectors { get; } = new();
    [ObservableProperty] private bool _enableRowColors = false;

    private const int PageSize = 50;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPageCount = 1;
    [ObservableProperty] private int _totalResultCount = 0;

    [ObservableProperty] private ObservableCollection<FileRecord> _previewRecords = new();

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
    [ObservableProperty] private string _currentStatus = string.Empty;
    [ObservableProperty] private bool? _isRemovedFilter = null; // Null means "Show Both"
    [ObservableProperty] private DateTime? _toBeRemovedDate;
    [ObservableProperty] private DateTime? _removedDate;

    //
    [ObservableProperty] private DateTime? _addedDateFrom;
    [ObservableProperty] private DateTime? _addedTimeFrom;
    [ObservableProperty] private DateTime? _addedDateTo;
    [ObservableProperty] private DateTime? _addedTimeTo;

    // exports
    [ObservableProperty] private bool _incAddedDate = true;
    [ObservableProperty] private bool _incSerial = true;
    [ObservableProperty] private bool _incRrNumber = true;
    [ObservableProperty] private bool _incSector = true;
    [ObservableProperty] private bool _incSubject = true;
    [ObservableProperty] private bool _incFileName = true;
    [ObservableProperty] private bool _incFileType = true;
    [ObservableProperty] private bool _incStartDate = true;
    [ObservableProperty] private bool _incEndDate = true;
    [ObservableProperty] private bool _incPages = true;
    [ObservableProperty] private bool _incShelf = true;
    [ObservableProperty] private bool _incDeck = true;
    [ObservableProperty] private bool _incFileNum = true;
    [ObservableProperty] private bool _incStatus = true;
    [ObservableProperty] private bool _incToBeRemovedDate = true;
    [ObservableProperty] private bool _incRemovedDate = true;
    [ObservableProperty] private bool _incIsRemoved = true;

    [ObservableProperty] private string _statusMessage = "Ready. Configure your report below.";
    [ObservableProperty] private string _statusColor = "Gray";
    [ObservableProperty] private bool _isProcessing = false;

    public ReportsViewModel(IArchiveService archiveService, IPreferencesService preferencesService)
    {
        _archiveService = archiveService;
        _preferencesService = preferencesService;
        LoadDropdowns();
        _ = UpdatePreviewAsync();
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this,
            (recipient, message) => { LoadDropdowns(); });
    }

    private void LoadDropdowns()
    {
        var prefs = _preferencesService.GetPreferences();

        AvailableSectors.Clear();
        RawSectors.Clear();
        AvailableSectors.Add("");
        foreach (var s in prefs.Sectors)
        {
            AvailableSectors.Add(s.Name);
            RawSectors.Add(s);
        }

        AvailableFileTypes.Clear();
        AvailableFileTypes.Add("");
        foreach (var t in prefs.FileTypes) AvailableFileTypes.Add(t);
    }



    partial void OnSerialNumberChanged(string value) => TriggerFilterSearch();
    partial void OnRrNumberChanged(string value) => TriggerFilterSearch();
    partial void OnSectorChanged(string value) => TriggerFilterSearch();
    partial void OnSubjectNumberChanged(string value) => TriggerFilterSearch();
    partial void OnFileNameChanged(string value) => TriggerFilterSearch();
    partial void OnFileTypeChanged(string value) => TriggerFilterSearch();
    partial void OnStartDateChanged(DateTime? value) => TriggerFilterSearch();
    partial void OnEndDateChanged(DateTime? value) => TriggerFilterSearch();
    partial void OnTotalPagesChanged(string value) => TriggerFilterSearch();
    partial void OnShelfNumberChanged(string value) => TriggerFilterSearch();
    partial void OnDeckNumberChanged(string value) => TriggerFilterSearch();
    partial void OnFileNumberChanged(string value) => TriggerFilterSearch();
    partial void OnCurrentStatusChanged(string value) => TriggerFilterSearch();
    partial void OnIsRemovedFilterChanged(bool? value) => TriggerFilterSearch();
    partial void OnToBeRemovedDateChanged(DateTime? value) => TriggerFilterSearch();
    partial void OnRemovedDateChanged(DateTime? value) => TriggerFilterSearch();

    //
    partial void OnAddedDateFromChanged(DateTime? value) => TriggerFilterSearch();
    partial void OnAddedTimeFromChanged(DateTime? value) => TriggerFilterSearch();
    partial void OnAddedDateToChanged(DateTime? value) => TriggerFilterSearch();
    partial void OnAddedTimeToChanged(DateTime? value) => TriggerFilterSearch();

    private void TriggerFilterSearch()
    {
        CurrentPage = 1;
        _ = UpdatePreviewAsync();
    }

    private DateTime? CombineDateTime(DateTime? date, DateTime? time)
    {
        if (!date.HasValue) return null;
        if (!time.HasValue) return date.Value.Date;
        return date.Value.Date + time.Value.TimeOfDay;
    }
    
    private async Task UpdatePreviewAsync()
    {

        var combinedFrom = CombineDateTime(AddedDateFrom, AddedTimeFrom);
        var combineTo = CombineDateTime(AddedDateTo, AddedDateTo);
        
        var result = await _archiveService.GetFilteredPreviewPaginatedAsync(
            SerialNumber, RrNumber, Sector, SubjectNumber, FileName, FileType, StartDate, EndDate, 
            TotalPages, ShelfNumber, DeckNumber, FileNumber, CurrentStatus, IsRemovedFilter, ToBeRemovedDate, RemovedDate,
            combinedFrom, combineTo,
            CurrentPage, PageSize);

        TotalResultCount = result.TotalCount;
        TotalPageCount = (int)Math.Ceiling((double)TotalResultCount / PageSize);
        if (TotalPageCount == 0) TotalPageCount = 1;
        
        PreviewRecords.Clear();
        foreach (var file in result.Items) PreviewRecords.Add(file);
        
        ShowStatus($"Previewing page {CurrentPage} of {TotalPageCount}. Total matches: {TotalResultCount}", "Gray");
    }
    
    // paginaton

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPageCount)
        {
            CurrentPage++;
            await UpdatePreviewAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await UpdatePreviewAsync();
        }
    }
    
    // ----
    

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = $"Custom_Report_{DateTime.Now:yyyyMMdd}.csv" };
        if (dialog.ShowDialog() != true) return;

        IsProcessing = true;
        ShowStatus("Generating CSV...", "Yellow");
        
        var combinedFrom = CombineDateTime(AddedDateFrom, AddedTimeFrom);
        var combinedTo = CombineDateTime(AddedDateTo, AddedTimeTo);
        
        var fullData = await _archiveService.GetFullFilteredExportAsync(
            SerialNumber, RrNumber, Sector, SubjectNumber, FileName, FileType, StartDate, EndDate, 
            TotalPages, ShelfNumber, DeckNumber, FileNumber, CurrentStatus, IsRemovedFilter, ToBeRemovedDate, RemovedDate,
            combinedFrom, combinedTo);

        var csv = new StringBuilder();
        var headers = new List<string>();
        
        if(IncAddedDate) headers.Add("Added Date/Time");
        if (IncSerial) headers.Add("Serial Number");
        if (IncRrNumber) headers.Add("RR Number");
        if (IncSector) headers.Add("Sector");
        if (IncSubject) headers.Add("Subject Number");
        if (IncFileName) headers.Add("File Name");
        if (IncFileType) headers.Add("File Type");
        if (IncStartDate) headers.Add("Start Date");
        if (IncEndDate) headers.Add("End Date");
        if (IncPages) headers.Add("Total Pages");
        if (IncShelf) headers.Add("Shelf Number");
        if (IncDeck) headers.Add("Deck Number");
        if (IncFileNum) headers.Add("File Number");
        if (IncStatus) headers.Add("Status");
        if (IncToBeRemovedDate) headers.Add("To Be Removed Date");
        if (IncRemovedDate) headers.Add("Removed Date");
        if (IncIsRemoved) headers.Add("Is Removed");

        csv.AppendLine(string.Join(",", headers));

        foreach (var file in fullData)
        {
            var row = new List<string>();
            string safeName = file.FileName?.Replace(",", " ") ?? ""; 

            if (IncAddedDate) row.Add(file.AddedDateTime.ToString("yyyy-MM-dd HH:mm"));
            if (IncSerial) row.Add(file.SerialNumber.ToString());
            if (IncRrNumber) row.Add(file.RrNumber ?? "");
            if (IncSector) row.Add(file.Sector ?? "");
            if (IncSubject) row.Add(file.SubjectNumber ?? "");
            if (IncFileName) row.Add(safeName);
            if (IncFileType) row.Add(file.FileType ?? "");
            if (IncStartDate) row.Add(file.StartDate?.ToString("yyyy-MM-dd") ?? "");
            if (IncEndDate) row.Add(file.EndDate?.ToString("yyyy-MM-dd") ?? "");
            if (IncPages) row.Add(file.TotalPages?.ToString() ?? "");
            if (IncShelf) row.Add(file.ShelfNumber?.ToString() ?? "");
            if (IncDeck) row.Add(file.DeckNumber?.ToString() ?? "");
            if (IncFileNum) row.Add(file.FileNumber?.ToString() ?? "");
            if (IncStatus) row.Add(file.CurrentStatus ?? "");
            if (IncToBeRemovedDate) row.Add(file.ToBeRemovedDate?.ToString("yyyy-MM-dd") ?? "");
            if (IncRemovedDate) row.Add(file.RemovedDate?.ToString("yyyy-MM-dd") ?? "");
            if (IncIsRemoved) row.Add(file.IsRemoved.ToString());

            csv.AppendLine(string.Join(",", row));
        }

        await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
        ShowStatus($"Successfully exported {fullData.Count} records to CSV.", "#4CAF50");
        IsProcessing = false;
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        var dialog = new SaveFileDialog { Filter = "Excel File (*.xlsx)|*.xlsx", FileName = $"Custom_Report_{DateTime.Now:yyyyMMdd}.xlsx" };
        if (dialog.ShowDialog() != true) return;
        
        // pop up box added
        
        MessageBoxResult result = MessageBox.Show (
            "Do you want to export this file with Sector Color Highlighting applied to the rows?", 
            "Excel Export Options", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        
        if (result == MessageBoxResult.Cancel) return;
        bool useColors = (result == MessageBoxResult.Yes);
        
        IsProcessing = true;
        ShowStatus("Generating Excel file...", "Yellow");

        var combinedFrom = CombineDateTime(AddedDateFrom, AddedTimeFrom);
        var combinedTo = CombineDateTime(AddedDateTo, AddedDateFrom);
        
        var fullData = await _archiveService.GetFullFilteredExportAsync(
            SerialNumber, RrNumber, Sector, SubjectNumber, FileName, FileType, StartDate, EndDate, 
            TotalPages, ShelfNumber, DeckNumber, FileNumber, CurrentStatus, IsRemovedFilter, ToBeRemovedDate, RemovedDate,
            combinedFrom, combinedTo);

        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Vault Records");
            
            string title = $"ලේඛනාගාර ලිපි ගොණු වාර්තාව - {DateTime.Now:yyyy/MM/dd - hh:mm tt}";
            ws.Cell("A1").Value = title;
            ws.Range("A1:K1").Merge();
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Cell("A1").Style.Font.FontColor = XLColor.DarkBlue;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int headerRow = 3;
            int col = 1;

            if (IncAddedDate) ws.Cell(headerRow, col++).Value = "Added Timestamp";
            if (IncSerial) ws.Cell(headerRow, col++).Value = "Serial Number";
            if (IncRrNumber) ws.Cell(headerRow, col++).Value = "RR Number";
            if (IncSector) ws.Cell(headerRow, col++).Value = "Sector";
            if (IncSubject) ws.Cell(headerRow, col++).Value = "Subject Number";
            if (IncFileName) ws.Cell(headerRow, col++).Value = "File Name";
            if (IncFileType) ws.Cell(headerRow, col++).Value = "File Type";
            if (IncStartDate) ws.Cell(headerRow, col++).Value = "Start Date";
            if (IncEndDate) ws.Cell(headerRow, col++).Value = "End Date";
            if (IncPages) ws.Cell(headerRow, col++).Value = "Total Pages";
            if (IncShelf) ws.Cell(headerRow, col++).Value = "Shelf";
            if (IncDeck) ws.Cell(headerRow, col++).Value = "Deck";
            if (IncFileNum) ws.Cell(headerRow, col++).Value = "File Number";
            if (IncStatus) ws.Cell(headerRow, col++).Value = "Status";
            if (IncToBeRemovedDate) ws.Cell(headerRow, col++).Value = "To Be Removed";
            if (IncRemovedDate) ws.Cell(headerRow, col++).Value = "Removed Date";
            if (IncIsRemoved) ws.Cell(headerRow, col++).Value = "Is Removed";

            var headerRange = ws.Range(headerRow, 1, headerRow, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkSlateGray;
            headerRange.Style.Font.FontColor = XLColor.White;

            int startDataRow = 4;
            for (int r = 0; r < fullData.Count; r++)
            {
                int c = 1;
                var file = fullData[r];
                var row = r + startDataRow;

                if (IncAddedDate) ws.Cell(row, c++).Value = file.AddedDateTime.ToString("yyyy-MM-dd HH:mm");
                if (IncSerial) ws.Cell(row, c++).Value = file.SerialNumber;
                if (IncRrNumber) ws.Cell(row, c++).Value = file.RrNumber;
                if (IncSector) ws.Cell(row, c++).Value = file.Sector;
                if (IncSubject) ws.Cell(row, c++).Value = file.SubjectNumber;
                if (IncFileName) ws.Cell(row, c++).Value = file.FileName;
                if (IncFileType) ws.Cell(row, c++).Value = file.FileType;
                if (IncStartDate) ws.Cell(row, c++).Value = file.StartDate?.ToString("yyyy-MM-dd");
                if (IncEndDate) ws.Cell(row, c++).Value = file.EndDate?.ToString("yyyy-MM-dd");
                if (IncPages) ws.Cell(row, c++).Value = file.TotalPages;
                if (IncShelf) ws.Cell(row, c++).Value = file.ShelfNumber;
                if (IncDeck) ws.Cell(row, c++).Value = file.DeckNumber;
                if (IncFileNum) ws.Cell(row, c++).Value = file.FileNumber;
                if (IncStatus) ws.Cell(row, c++).Value = file.CurrentStatus;
                if (IncToBeRemovedDate) ws.Cell(row, c++).Value = file.ToBeRemovedDate?.ToString("yyyy-MM-dd");
                if (IncRemovedDate) ws.Cell(row, c++).Value = file.RemovedDate?.ToString("yyyy-MM-dd");
                if (IncIsRemoved) ws.Cell(row, c++).Value = file.IsRemoved;


                if (useColors && !string.IsNullOrWhiteSpace(file.Sector))
                {
                    var match = RawSectors.FirstOrDefault(s => s.Name == file.Sector);
                    if (match != null && !string.IsNullOrWhiteSpace(match.ColorHex))
                    {
                        try
                        {
                            var xlColor = XLColor.FromHtml(match.ColorHex);
                            ws.Range(row, 1, row, col - 1).Style.Fill.BackgroundColor = xlColor;
                        }
                        catch{}
                    }
                }
                
            }

            ws.Columns().AdjustToContents();

            if (fullData.Count > 0)
            {
                var dataGrid = ws.Range(headerRow, 1, fullData.Count + startDataRow - 1, col - 1);
                dataGrid.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataGrid.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataGrid.Style.Border.InsideBorderColor = XLColor.LightGray;
                dataGrid.Style.Border.OutsideBorderColor = XLColor.Gray;
                
            }
            
            workbook.SaveAs(dialog.FileName);
        }

        ShowStatus($"Successfully exported {fullData.Count} records to Excel.", "#4CAF50");
        IsProcessing = false;
    }

    [RelayCommand]
    private async Task BackupSqlAsync()
    {
        var dialog = new SaveFileDialog { Filter = "SQL Backup File (*.sql)|*.sql", FileName = $"Full_Backup_{DateTime.Now:yyyyMMdd}.sql" };
        if (dialog.ShowDialog() != true) return;

        IsProcessing = true;
        ShowStatus("Running SQL Backup (This backs up the entire database regardless of filters)...", "Yellow");
        var result = await _archiveService.BackupDatabaseAsync(dialog.FileName);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");
        IsProcessing = false;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SerialNumber = string.Empty; RrNumber = string.Empty; Sector = string.Empty;
        SubjectNumber = string.Empty; FileName = string.Empty; FileType = string.Empty;
        StartDate = null; EndDate = null; TotalPages = string.Empty;
        ShelfNumber = string.Empty; DeckNumber = string.Empty; FileNumber = string.Empty;
        CurrentStatus = string.Empty; IsRemovedFilter = null; ToBeRemovedDate = null; RemovedDate = null;

        AddedDateFrom = null; AddedTimeFrom = null;
        AddedDateTo = null; AddedTimeTo = null;
        
        IncSerial = true; IncRrNumber = true; IncSector = true; IncSubject = true;
        IncFileName = true; IncFileType = true; IncStartDate = true; IncEndDate = true;
        IncPages = true; IncShelf = true; IncDeck = true; IncFileNum = true; IncStatus = true;
        IncToBeRemovedDate = true; IncRemovedDate = true; IncIsRemoved = true;
        
        TriggerFilterSearch();
    }

    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}