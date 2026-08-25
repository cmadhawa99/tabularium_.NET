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
    
    public DocumentManagerViewModel (IDocumentService documentService)
    {
        _documentService = documentService;
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

        if (!file.MimeType.StartsWith("image/"))
        {
            ShowStatus("In-app preview is only available for images. Use Export for other file types.", "#FF9800");
            return;
        }

        try
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
            IsPreviewOpen = true;
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to decrypt file for preview: {ex.Message}", "#F44336");
        }
        
    }

    [RelayCommand]
    private void ClosePreview()
    {
        IsPreviewOpen = false;
        PreviewImage = null;
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