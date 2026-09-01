using System.IO;
using ArchivumWpf.Models;
using Microsoft.EntityFrameworkCore;

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
    Task DeleteFileAsync(Guid digitalFileId);
}

public class DocumentService : IDocumentService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly string _storagePath;

    public DocumentService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;

        _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".SecureStore");
        if (!Directory.Exists(_storagePath))
        {
            var di = Directory.CreateDirectory(_storagePath);
            di.Attributes |= FileAttributes.Hidden;
        }
    }

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

        var recordStorageId = root.PhysicalStorageId.Value;

        var fileId = Guid.NewGuid();
        var physicalFileName = $"{fileId}.dat";
        var finalPath = GetPhysicalPath(recordStorageId, physicalFileName);
        var tempPath = finalPath + ".tmp";

        var crypto = GetCrypto();
        string encryptedDek;

        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
        using (var destStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                   FileOptions.WriteThrough))
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
        var sourcePath = GetPhysicalPath(file.RecordStorageId, file.PhysicalFileName);

        var crypto = GetCrypto();
        var memoryStream = new MemoryStream();

        using (var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
        {
            crypto.DecryptFileStream(fileStream, memoryStream, file.EncryptedDEK);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task ExportDecryptedFileAsync(Guid digitalFileId, string destinationPath)
    {
        var file = await GetFileAsync(digitalFileId) ?? throw new FileNotFoundException("Document record not found.");
        var sourcePath = GetPhysicalPath(file.RecordStorageId, file.PhysicalFileName);

        var crypto = GetCrypto();
        using var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var outStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        crypto.DecryptFileStream(fileStream, outStream, file.EncryptedDEK);
    }

    public async Task DeleteFileAsync(Guid digitalFileId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.DigitalFiles.FirstOrDefaultAsync(f => f.Id == digitalFileId);
        if (file == null) return;

        var path = GetPhysicalPath(file.RecordStorageId, file.PhysicalFileName);
        if (File.Exists(path))
            try
            {
                File.Delete(path);
            }
            catch
            {
                /* locked/in-use file - DB row removal still proceeds; leftover .dat is caught by the integrity check */
            }

        context.DigitalFiles.Remove(file);
        await context.SaveChangesAsync();
    }

    private static CryptoService GetCrypto()
    {
        return new CryptoService(KeyVaultService.GetMasterKey());
    }

    private string GetPhysicalPath(Guid recordStorageId, string physicalFileName)
    {
        var folderPath = Path.Combine(_storagePath, recordStorageId.ToString());
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, physicalFileName);
    }


    private static string GuessMimeType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
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
}