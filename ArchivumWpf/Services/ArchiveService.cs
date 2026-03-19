
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Models;
using DocumentFormat.OpenXml.Spreadsheet;


namespace ArchivumWpf.Services;

public interface IArchiveService
{
    Task<DashboardStats> GetDashboardStatsAsync();   
    Task<(List<FileRecord> Items, int TotalCount)> SearchFilesPaginatedAsync(string searchTerm, int pageNumber, int pageSize); 
    Task<List<BorrowRecord>> GetActiveLoansAsync();
    Task<(bool Success, string Message)> IssueFileAsync(string rrNumber, string borrowerName);
    Task<(bool Success, string Message)> ReturnFileASync(string rrNumber);
    Task<(bool Success, string Message)> AddNewFileAsync(FileRecord newFile);

    Task<(List<FileRecord> Items, int TotalCount)> GetFilteredPreviewPaginatedAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string filetype, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate,
        int pageNumber, int pageSize);

    Task<List<FileRecord>> GetFullFilteredExportAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate);

    Task<(bool Success, string Message)> BackupDatabaseAsync(string backupPath);

}

public class ArchiveService : IArchiveService
{
    private readonly AppDbContext _context;

    public ArchiveService (AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        int total = await _context.FileRecords.CountAsync();

        int borrowed = await _context.FileRecords.CountAsync(f => f.CurrentStatus == "Borrowed");

        int removed = await _context.FileRecords.CountAsync(f => f.CurrentStatus == "Removed" || f.IsRemoved == true);

        return new DashboardStats
        {
            TotalHoldings = total,
            ActiveLoans = borrowed,
            ArchivedPurged = removed,
        };
    }

    public async Task<(List<FileRecord> Items, int TotalCount)> SearchFilesPaginatedAsync(string searchTerm, int pageNumber, int pageSize)
    {
        var query = _context.FileRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(f => f.RrNumber.ToLower().Contains(searchTerm) || 
                                     f.FileName.ToLower().Contains(searchTerm) ||
                                     f.Sector.ToLower().Contains(searchTerm));
        }

        query = query.OrderByDescending(f => f.SerialNumber);

        int totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);

    }
    
    // Circulation

    public async Task<List<BorrowRecord>> GetActiveLoansAsync()
    {
        return await _context.BorrowRecords
            .Include(b => b.File)
            .Where(b => b.IsReturned == false)
            .OrderByDescending(b => b.BorrowedDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> IssueFileAsync(string rrNumber, string borrowerName)
    {
      var file =  await _context.FileRecords.FirstOrDefaultAsync(f => f.RrNumber == rrNumber);

      if (file == null) return (false, "Error: File not found in the vault");
      if (file.CurrentStatus == "Borrowed") return (false, "Error: File is already borrowed");
      if (file.IsRemoved) return (false, "Error, File has been disposed/archived");

      file.CurrentStatus = "Borrowed";

      var record = new BorrowRecord
      {
          FileRrNumber = rrNumber,
          BorrowerName = borrowerName,
          BorrowedDate = DateTime.Now.Date,
          IsReturned = false
      };

      _context.BorrowRecords.Add(record);
      await _context.SaveChangesAsync();

      return (true, $"Success: File {rrNumber} has been issued to {borrowerName}.");
    }

    public async Task<(bool Success, string Message)> ReturnFileASync(string rrNumber)
    {
        var file = await _context.FileRecords.FirstOrDefaultAsync(f => f.RrNumber == rrNumber);

        if (file == null) return (false, "Error: File not found in the vault.");
        if (file.CurrentStatus != "Borrowed") return (false, "Error File is not currently borrowed.");

        file.CurrentStatus = "Available";

        var activeRecord = await _context.BorrowRecords
            .FirstOrDefaultAsync(b => b.FileRrNumber == rrNumber && b.IsReturned == false);

        if (activeRecord != null)
        {
            activeRecord.IsReturned = true;
            activeRecord.ReturnedDate = DateTime.Now.Date;
        }

        await _context.SaveChangesAsync();

        return (true, $"Success: File {rrNumber} has been returned to the vault.");

    }
    
    // New Entry

    public async Task<(bool Success, string Message)> AddNewFileAsync(FileRecord newFile)
    {
        try
        {
            bool exists = await _context.FileRecords.AnyAsync(f => f.RrNumber == newFile.RrNumber);
            if (exists)
            {
                return (false, $"Error: A file with RR Number '{newFile.RrNumber}' already exists.");
            }

            int maxSerial = 0;
            if (await _context.FileRecords.AnyAsync())
            {
                maxSerial = await _context.FileRecords.MaxAsync(f => f.SerialNumber);
            }
            newFile.SerialNumber = maxSerial + 1;
                
            newFile.CurrentStatus = "Available";
            newFile.IsRemoved = false;

            _context.FileRecords.Add(newFile);
            await _context.SaveChangesAsync();

            return (true, $"Success: File '{newFile.RrNumber}' has been added to the vault.");
        }

        catch (System.Exception ex)
        {
            string exactError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return (false, $"Database Error : {exactError}");
            //return (false, $"Database Error: {ex.Message}");
        }
    }
    
    //Records

    private IQueryable<FileRecord> BuildReportQuery(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate)
    {
        var query = _context.FileRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(serialNumber) && int.TryParse(serialNumber, out int serial))
            query = query.Where(f => f.SerialNumber == serial);
        
        if (!string.IsNullOrWhiteSpace(rrNumber))
            query = query.Where(f => f.RrNumber.ToLower().Contains(rrNumber.ToLower()));
        
        if (!string.IsNullOrWhiteSpace(sector)) 
            query = query.Where(f => f.Sector.ToLower().Contains(sector.ToLower()));

        if (!string.IsNullOrWhiteSpace(subjectNumber))
            query = query.Where(f => f.SubjectNumber != null && f.SubjectNumber.ToLower().Contains(subjectNumber.ToLower()));
        
        if (!string.IsNullOrWhiteSpace(fileName))
            query = query.Where(f => f.FileName.ToLower().Contains(fileName.ToLower()));

        if (!string.IsNullOrWhiteSpace(fileType))
            query = query.Where(f => f.FileType != null && f.FileType.ToLower().Contains(fileType.ToLower()));
        
        if (startDate.HasValue)
            query = query.Where(f => f.StartDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(f => f.EndDate <= endDate.Value);
        
        if (!string.IsNullOrWhiteSpace(totalPages) && int.TryParse(totalPages, out int tp))
            query = query.Where(f => f.TotalPages == tp);
        
        if (!string.IsNullOrWhiteSpace(shelfNumber) && int.TryParse(shelfNumber, out int sn))
            query = query.Where(f => f.ShelfNumber == sn);

        if (!string.IsNullOrWhiteSpace(deckNumber) && int.TryParse(deckNumber, out int dn))
            query = query.Where(f => f.DeckNumber == dn);

        if (!string.IsNullOrWhiteSpace(fileNumber) && int.TryParse(fileNumber, out int fn))
            query = query.Where(f => f.FileNumber == fn);
        
        if (!string.IsNullOrWhiteSpace(currentStatus)) 
            query = query.Where(f => f.CurrentStatus.ToLower().Contains(currentStatus.ToLower()));
        
        if (isRemoved.HasValue) 
            query = query.Where(f => f.IsRemoved == isRemoved.Value);
        
        if (toBeRemovedDate.HasValue) 
            query = query.Where(f => f.ToBeRemovedDate >= toBeRemovedDate.Value);
        
        if (removedDate.HasValue) 
            query = query.Where(f => f.RemovedDate >= removedDate.Value);
        
        return query.OrderBy(f => f.SerialNumber);
    }

    public async Task<(List<FileRecord> Items, int TotalCount)> GetFilteredPreviewPaginatedAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate,
        int pageNumber, int pageSize)
    {
        var query = BuildReportQuery(
            serialNumber, rrNumber, sector, subjectNumber, fileName, fileType, startDate, endDate, 
            totalPages, shelfNumber, deckNumber, fileNumber, currentStatus, isRemoved, toBeRemovedDate, removedDate);
        
        int totalCount = await query.CountAsync();
        
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<FileRecord>> GetFullFilteredExportAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate)
    {
        var query = BuildReportQuery(
            serialNumber, rrNumber, sector, subjectNumber, fileName, fileType, startDate, endDate, 
            totalPages, shelfNumber, deckNumber, fileNumber, currentStatus, isRemoved, toBeRemovedDate, removedDate);

        return await query.ToListAsync();
    }
    

    
    //sql
    
    public async Task<(bool Success, string Message)> BackupDatabaseAsync(string backupPath)
    {
        try
        {
            string dbName = "archive_db";
            string dbUser = "postgres";
            string dbPassword = "su753421#2";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-U {dbUser} -d {dbName} -f \"{backupPath}\" -F p",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            processStartInfo.EnvironmentVariables["PGPASSWORD"] = dbPassword;

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return (true, "Database backup completed successfully.");

            }
            else
            {
                string error = await process.StandardError.ReadToEndAsync();
                return (false, $"Backup failed : {error}");
            }
        }
        catch (System.Exception ex)
        {
            return (false, $"Failed to start backup process. Is PostgresSQL installed and in your PATH? Error: {ex.Message}");
        }
    }
}
