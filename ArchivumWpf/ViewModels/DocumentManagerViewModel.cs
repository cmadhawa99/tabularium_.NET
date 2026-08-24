using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;

namespace ArchivumWpf.ViewModels
{
    public class DocumentManagerViewModel
    {
        private readonly DocumentService _documentService;
        
        public string RecordTitle { get; set; }
        public int CurrentFolderId { get; set; }
        
        public ObservableCollection<DigitalFile> Files { get; set; } = new ObservableCollection<DigitalFile>();
        
        public ICommand ImportFilesCommand { get; }
        public ICommand ViewDocumentCommand { get; }
        
        public DocumentManagerViewModel(DocumentService documentService)
        {
            _documentService = documentService;
        }

        public async void HandleDrop(string[] filePaths)
        {
            foreach (var path in filePaths)
            {
                if (File.Exists(path))
                {
                    var newFile = await _documentService.ImportFileAsync(path, CurrentFolderId);
                    Files.Add(newFile);
                }
            }
        }

    }
}