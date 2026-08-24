using System;
using System.IO;
using System.Threading.Tasks;
using ArchivumWpf.Models;

namespace ArchivumWpf.Services
{
    public class DocumentService
    {
        private readonly AppDbContext _dbContext;
        private readonly CryptoService _cryptoService;
        private readonly string _storagePath;

        public DocumentService(AppDbContext dbContext, CryptoService cryptoService)
        {
            _dbContext = dbContext;
            _cryptoService = cryptoService;
            
            _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".SecureStore"); 
            if (!Directory.Exists(_storagePath))
            {
                DirectoryInfo di = Directory.CreateDirectory(_storagePath);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
        }

        public async Task<DigitalFile> ImportFileAsync(string sourceFilePath, int folderId)
        {
            var fileId = Guid.NewGuid();
            string physicalFileName = $"{fileId}.dat";
            string destinationPath = Path.Combine(_storagePath, physicalFileName);

            string encryptedDek;
            long fileSize;
            
            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                var result = _cryptoService.EncryptFileStream(sourceStream, destStream);
                encryptedDek = result.EncryptedDek;
                fileSize = result.TotalFileSize;
            }

            var digitalFile = new DigitalFile
            {
                Id = fileId,
                FolderId = folderId,
                OriginalFileName = Path.GetFileName(sourceFilePath),
                PhysicalFileName = physicalFileName,
                FileSize = fileSize,
                MimeType = "application/octet-stream",
                EncryptedDEK = encryptedDek,
                IV = "embedded"
            };
            
            _dbContext.DigitalFiles.Add(digitalFile);
            await _dbContext.SaveChangesAsync();
            
            return digitalFile;

        }

        public MemoryStream GetDecryptedMemoryStream(DigitalFile file)
        {
            string sourcePath = Path.Combine(_storagePath, file.PhysicalFileName);
            var memoryStream = new MemoryStream();

            using (var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            {
                _cryptoService.DecryptFileStream(fileStream, memoryStream, file.EncryptedDEK);
            }
            
            memoryStream.Position = 0;
            return memoryStream;
        }
        
    }
}