using System.Collections.ObjectModel;
using System.Windows;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using ArchivumWpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ArchivumWpf.ViewModels;

public partial class DocumentsSearchViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;

    [ObservableProperty] private string _searchQuery = string.Empty;

    public DocumentsSearchViewModel(IArchiveService archiveService)
    {
        _archiveService = archiveService;
        _ = SearchAsync();
    }

    public ObservableCollection<FileRecord> Results { get; } = new();

    [RelayCommand]
    private async Task SearchAsync()
    {
        var (items, _) = await _archiveService.SearchFilesPaginatedAsync(
            SearchQuery, string.Empty, null, null, false, false, false, false, 1, 100);

        Results.Clear();
        foreach (var f in items) Results.Add(f);
    }

    public async Task RefreshAsync()
    {
        await SearchAsync();
    }

    [RelayCommand]
    private void OpenDocumentManager(FileRecord record)
    {
        if (record == null) return;

        var app = (App)Application.Current;
        var window = app.Services.GetRequiredService<DocumentManagerWindow>();
        var vm = (DocumentManagerViewModel)window.DataContext;

        _ = vm.InitializeAsync(record.SerialNumber, record.RrNumber, record.FileName, true);

        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }
}