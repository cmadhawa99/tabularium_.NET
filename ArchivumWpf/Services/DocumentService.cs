using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Models;

namespace ArchivumWpf.Services;

public interface IDocumentService
{
    Task<Folder> GetOrCreateRootFolderAsync(int fileRecordSerial);
    Task<List<Folder>> GetSubFoldersAsync(int folderId);
    Task<List<DigitalFile>> GetFilesAsync(int folderId);
    Task<Folder> CreateFolderAsync(int parentFolderId, int fileRecordSerial, string folderName);
    Task<DigitalFile> ImportFileAsync(string sourceFilePath, int folderId, int fileRecordSerial);
    Task<DigitalFile?> GetFileAsync(Guid digitalFileId); 
    Task<MemoryStream> GetPreviewStreamAsync(Guid digitalFileId);
    Task ExportDecryptedFileAsync(Guid digitalFileId, string destinationPath);
}

public class DocumentService : IDocumentService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly string _storagePath;
    
    public DocumentService (IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        
        _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".SecureStore");
        if (!Directory.Exists(_storagePath))
        {
            var di = Directory.CreateDirectory(_storagePath);
            di.Attributes |= FileAttributes.Hidden;
        }
    }

    private static CryptoService GetCrypto() => new CryptoService(KeyVaultService.GetMasterKey());

    public async Task<Folder> GetOrCreateRootFolderAsync(int fileRecordSerial)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var root = await context.Folders
            .Where(f => f.FileRecordSerialNumber == fileRecordSerial && f.ParentFolderId == null)
            .FirstOrDefaultAsync();
        
        if (root != null) return root;

        root = new Folder
        {
            FileRecordSerialNumber = fileRecordSerial,
            ParentFolderId = null,
            FolderName = "Root",
            PhysicalStorageId = Guid.NewGuid()
        };
        
        context.Folders.Add(root);
        await context.SaveChangesAsync();
        return root;
    }

    private string GetPhysicalPath(Guid recordStorageId, string physicalFileName)
    {
        string folderPath = Path.Combine(_storagePath, recordStorageId.ToString());
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, physicalFileName); 
    }

    public async Task<List<Folder>> GetSubFoldersAsync(int folderId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Folders.Where(f => f.ParentFolderId == folderId).ToListAsync();
    }

    public async Task<List<DigitalFile>> GetFilesAsync(int folderId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var files = await context.DigitalFiles.AsNoTracking().Where(d => d.FolderId == folderId).ToListAsync();

        foreach (var f in files)
            f.IsMissing = !File.Exists(GetPhysicalPath(f.RecordStorageId, f.PhysicalFileName));

        return files;
    }

    public async Task<Folder> CreateFolderAsync(int parentFolderId, int fileRecordSerial, string folderName)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var folder = new Folder
        {
            ParentFolderId = parentFolderId,
            FileRecordSerialNumber = fileRecordSerial,
            FolderName = folderName
        };
        
        context.Folders.Add(folder);
        await context.SaveChangesAsync();
        return folder;
    }

    public async Task<DigitalFile> ImportFileAsync(string sourceFilePath, int folderId, int fileRecordSerial)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var root = await context.Folders
            .FirstOrDefaultAsync(f => f.FileRecordSerialNumber == fileRecordSerial && f.ParentFolderId == null);
        
        if (root?.PhysicalStorageId == null)
            throw new InvalidOperationException("Record storage folder has not been initialized.");

        Guid recordStorageId = root.PhysicalStorageId.Value;
        
        var fileId = Guid.NewGuid();
        string physicalFileName = $"{fileId}.dat";
        string finalPath = GetPhysicalPath(recordStorageId, physicalFileName);
        string tempPath = finalPath + ".tmp";
        
        var crypto = GetCrypto();
        string encryptedDek;
        
        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
        using (var destStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            var result = crypto.EncryptFileStream(sourceStream, destStream);
            encryptedDek = result.EncryptedDek;
            await destStream.FlushAsync();
        }
        File.Move(tempPath, finalPath);

        var digitalFile = new DigitalFile
        {
            Id = fileId,
            FolderId = folderId,
            RecordStorageId = recordStorageId,
            OriginalFileName = Path.GetFileName(sourceFilePath),
            PhysicalFileName = physicalFileName,
            FileSize = new FileInfo(sourceFilePath).Length,
            MimeType = GuessMimeType(sourceFilePath),
            EncryptedDEK = encryptedDek,
            IV = "embedded"
        };

        context.DigitalFiles.Add(digitalFile);
        await context.SaveChangesAsync();
        
        return digitalFile;
    }

    public async Task<DigitalFile?> GetFileAsync(Guid digitalFileId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DigitalFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == digitalFileId);
    }

    public async Task<MemoryStream> GetPreviewStreamAsync(Guid digitalFileId)
    {
        var file = await GetFileAsync(digitalFileId) ?? throw new FileNotFoundException("Document record not found.");
        string sourcePath = GetPhysicalPath(file.RecordStorageId, file.PhysicalFileName);

        var crypto = GetCrypto();
        var memoryStream = new MemoryStream();

        using (var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            crypto.DecryptFileStream(fileStream, memoryStream, file.EncryptedDEK);

        memoryStream.Position = 0;
        return memoryStream;

    }

    public async Task ExportDecryptedFileAsync(Guid digitalFileId, string destinationPath)
    {
        var file = await GetFileAsync(digitalFileId) ?? throw new FileNotFoundException("Document record not found.");
        string sourcePath = GetPhysicalPath(file.RecordStorageId, file.PhysicalFileName);

        var crypto = GetCrypto();
        using var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var outStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        crypto.DecryptFileStream(fileStream, outStream, file.EncryptedDEK);
    }
    
    
    
    

    private static string GuessMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".pdf" => "application/pdf",
        ".doc" or ".docx" => "application/msword",
        ".xls" or ".xlsx" => "application/vnd.ms-excel",
        _ => "application/octet-stream"
    };

}

