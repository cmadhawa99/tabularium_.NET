using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ArchivumWpf.Models;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels;

public partial class DocumentManagerViewModel : ObservableObject
{
    private readonly IDocumentService _documentService;
    private readonly IPdfRenderService _pdfRenderService;
    private byte[]? _currentPdfBytes;
    
    [ObservableProperty] private int _fileRecordSerial;
    [ObservableProperty] private string _recordTitle = string.Empty;
    [ObservableProperty] private Folder? _currentFolder;

    public ObservableCollection<DigitalFile> Files { get; } = new();

    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statusColor = "Gray";

    [ObservableProperty] private bool _isPreviewOpen = false;
    [ObservableProperty] private BitmapImage? _previewImage;
    [ObservableProperty] private string _previewFileName  = string.Empty;
    
    [ObservableProperty] private bool _isPdfMode = false;
    [ObservableProperty] private bool _isPdfLoading = false;
    [ObservableProperty] private BitmapImage? _pdfPageImage;
    [ObservableProperty] private int _pdfCurrentPage = 1;
    [ObservableProperty] private int _pdfTotalPages = 1;
    [ObservableProperty] private double _pdfZoom = 1.5;
    public ObservableCollection<BitmapImage?> PdfThumbnails { get; } = new();
    
    public DocumentManagerViewModel (IDocumentService documentService, IPdfRenderService pdfRenderService)
    {
        _documentService = documentService;
        _pdfRenderService = pdfRenderService;
    }

    public async Task InitializeAsync(int fileRecordSerial, string rrNumber, string fileName)
    {
        FileRecordSerial = fileRecordSerial;
        RecordTitle = $"{rrNumber} — {fileName}";

        CurrentFolder = await _documentService.GetOrCreateRootFolderAsync(fileRecordSerial);
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (CurrentFolder == null) return;

        var files = await _documentService.GetFilesAsync(CurrentFolder.Id);
        Files.Clear();
        foreach (var f in files) Files.Add(f);
    }

    [RelayCommand]
    private async Task ImportFilesAsync()
    {
        var dialog = new OpenFileDialog {Multiselect = true, Title = "Select files to import and encrypt" };
        if (dialog.ShowDialog() != true) return;
        
        await ImportPathsAsync (dialog.FileNames);
    }

    public async Task ImportPathsAsync(string[] paths)
    {
        if (CurrentFolder == null) return;

        IsBusy = true;
        int successCount = 0;

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            try
            {
                await _documentService.ImportFileAsync(path, CurrentFolder.Id, FileRecordSerial);
                successCount++;
            }

            catch (Exception ex)
            {
                ShowStatus($"Failed to import '{Path.GetFileName(path)}': {ex.Message}", "#F44336");
            }
        }
        
        await RefreshAsync();
        IsBusy = false;
        
        if (successCount == 0) 
            ShowStatus($"Successfully imported and encrypted {successCount} file(s).", "#4CAF50");
    }

    [RelayCommand]
    private async Task ViewDocumentAsync(DigitalFile file)
    {
        if (file == null) return;

        try
        {
            if (file.MimeType == "application/pdf")
            {
                await OpenPdfPreviewAsync (file);
            }
            
            else if (file.MimeType.StartsWith ("image/"))
            {
                using var ms = await _documentService.GetPreviewStreamAsync(file.Id);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                PreviewImage = bitmap;
                PreviewFileName = file.OriginalFileName;
                IsPdfMode = false;
                IsPreviewOpen = true;
            }
            else
            {
                ShowStatus("In-app preview is only available for images and PDFs. Use Export for other file types.", "#FF9800");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to decrypt file for preview: {ex.Message}", "#F44336");
        }
        
    }

    private async Task OpenPdfPreviewAsync(DigitalFile file)
    {
        IsPdfLoading = true;
        IsPdfMode = true;
        IsPreviewOpen = true;
        PreviewFileName = file.OriginalFileName;
        PdfThumbnails.Clear();

        using (var ms = await _documentService.GetPreviewStreamAsync(file.Id))
        {
            _currentPdfBytes = ms.ToArray();
        }

        PdfTotalPages = await _pdfRenderService.GetPageCountAsync(_currentPdfBytes);
        PdfCurrentPage = 1;

        await RenderCurrentPdfPageAsync();
        IsPdfLoading = false;

        _ = LoadPdfThumbnailsAsync();
    }

    private async Task RenderCurrentPdfPageAsync()
    {
        if (_currentPdfBytes == null) return;
        PdfPageImage = await _pdfRenderService.RenderPageAsync(_currentPdfBytes, PdfCurrentPage - 1, PdfZoom);
    }

    private async Task LoadPdfThumbnailsAsync()
    {
        if (_currentPdfBytes == null) return;
        for (int i = 0; i < PdfTotalPages; i++)
        {
            if (_currentPdfBytes == null) return;
            var thumb = await _pdfRenderService.RenderPageAsync(_currentPdfBytes, i, scale: 0.4);
            PdfThumbnails.Add(thumb);
        }
    }

    [RelayCommand]
    private async Task NextPdfPageAsync()
    {
        if (PdfCurrentPage >= PdfTotalPages) return;
        PdfCurrentPage++;
        await RenderCurrentPdfPageAsync();
    }

    [RelayCommand]
    private async Task PreviousPdfPageAsync()
    {
        if (PdfCurrentPage <= 1) return;
        PdfCurrentPage--;
        await RenderCurrentPdfPageAsync();
    }

    [RelayCommand]
    private async Task JumpToPdfPageAsync(BitmapImage thumbnail)
    {
        int index = PdfThumbnails.IndexOf(thumbnail);
        if (index < 0) return;
        PdfCurrentPage = index + 1;
        await RenderCurrentPdfPageAsync();
    }

    [RelayCommand]
    private async Task ZoomInPdfAsync()
    {
        if (PdfZoom >= 4.0) return;
        PdfZoom += 0.5;
        await RenderCurrentPdfPageAsync();
    }

    [RelayCommand]
    private async Task ZoomOutPdfAsync()
    {
        if (PdfZoom <= 0.5) return;
        PdfZoom -= 0.5;
        await RenderCurrentPdfPageAsync();
    }

    [RelayCommand]
    private void ClosePreview()
    {
        IsPreviewOpen = false;
        PreviewImage = null;
        PdfPageImage = null;
        PdfThumbnails.Clear();
        _currentPdfBytes = null;
        IsPdfMode = false;
    }

    [RelayCommand]
    private async Task ExportDocumentAsync(DigitalFile file)
    {
        if (file == null) return;
        
        var dialog = new SaveFileDialog {FileName = file.OriginalFileName, Filter = "All Files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            await _documentService.ExportDecryptedFileAsync(file.Id, dialog.FileName);
            ShowStatus($"'{file.OriginalFileName}' decrypted and saved successfully.", "#4CAF50");
        }
        catch (Exception ex)
        {
            ShowStatus($"Export failed: {ex.Message}", "#F44336");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
    
    
    
}