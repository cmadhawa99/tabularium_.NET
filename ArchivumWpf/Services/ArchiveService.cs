using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Models;
using Bogus.DataSets;

namespace ArchivumWpf.Services;

public interface IArchiveService
{
    Task<DashboardStats> GetDashboardStatsAsync();

    Task<(List<FileRecord> Items, int TotalCount)> SearchFilesPaginatedAsync(string searchTerm, string sectorFilter,
        int? yearFilter,
        int? monthFilter, bool isRecentOnly, bool isAvailableOnly, bool isRemovedOnly, bool isStrictRrSearch, int pageNumber, int pageSize);
   
    Task<List<string>> GetExistingSectorsAsync();
    
    Task<List<BorrowRecord>> GetActiveLoansAsync();
    Task<(bool Success, string Message)> IssueFileAsync(string rrNumber, string borrowerName, string sectorColorHex);
    Task<(bool Success, string Message)> ReturnFileASync(string rrNumber);
    Task<(bool Success, string Message)> AddNewFileAsync(FileRecord newFile);

    Task<(List<FileRecord> Items, int TotalCount)> GetFilteredPreviewPaginatedAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string filetype, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate,
        DateTime? addedDateFrom, DateTime? addedDateTo,
        int pageNumber, int pageSize);

    Task<List<FileRecord>> GetFullFilteredExportAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate,
        DateTime? addedDateFrom, DateTime? addedDateTo);

    Task<(bool Success, string Message)> BackupDatabaseAsync(string backupPath);
    
    Task<FileRecord> GetFileByRrNumberAsync(string rrNumber);
    Task<(bool Success, string Message)> UpdateFileAsync(FileRecord updatedFile);
    Task<List<EntryHistoryRecord>> GetEntryHistoryAsync();
    Task<EntryHistoryRecord> GetPreviousHistoryRecordAsync(int fileSerialNumber, DateTime currentTimestamp);
    
    Task<(bool Success, string Message)> UpdateDisposalQueueAsync(string rrNumber, DateTime? toBeRemovedDate);
    Task<(bool Success, string Message)> DisposeFileAsync(string rrNumber, string reason, string authorizedBy);
    Task<(bool Success, string Message)> RecoverFileAsync(string rrNumber);
    Task<List<FileRecord>> GetPendingDisposalsAsync();
    Task<List<DisposedRecord>> GetDisposedHistoryAsync();
    Task<int> GetTodayDisposalCountAsync();
    Task<(List<BorrowRecord> Items, int TotalCount)> GetBorrowHistoryPaginatedAsync (string searchTerm, int pageNumber, int pageSize);
    Task<(List<EntryHistoryRecord> Items, int TotalCount)> GetEntryHistoryPaginatedAsync (string searchTerm, int pageNumber, int pageSize);

    Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int limit = 15);
    Task LogActivityAsync(string serialNumber, string rrNumber, string actionType);
    
}

public class ArchiveService : IArchiveService
{
    // CHANGED to the Factory to prevent memory leaks and tracking collisions
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ArchiveService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        int total = await context.FileRecords.CountAsync();
        int borrowed = await context.FileRecords.CountAsync(f => f.CurrentStatus == "Borrowed");
        int removed = await context.FileRecords.CountAsync(f => f.CurrentStatus == "Removed" || f.IsRemoved == true);

        return new DashboardStats
        {
            TotalHoldings = total,
            ActiveLoans = borrowed,
            ArchivedPurged = removed,
        };
    }
    
    
    //Dashboard

    public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int limit = 15)
    {
        
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.ActivityLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task LogActivityAsync(string serialNumber, string rrNumber, string actionType)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var log = new ActivityLog
        {
            SerialNumber = serialNumber ?? "SYS=GEN",
            RrNumber = rrNumber ?? "N/A",
            ActionType = actionType,
            Timestamp = DateTime.Now
        };
        
        context.ActivityLogs.Add(log);
        await context.SaveChangesAsync();

    }


    //Search

    public async Task<(List<FileRecord> Items, int TotalCount)> SearchFilesPaginatedAsync(string searchTerm, string sectorFilter, int? yearFilter,
    int? monthFilter, bool isRecentOnly, bool isAvailableOnly, bool isRemovedOnly, bool isStrictRrSearch, int pageNumber, int pageSize)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.FileRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower().Trim();

            if (isStrictRrSearch)
            {
                query = query.Where(f => f.RrNumber.ToLower() == searchTerm);
            }

            else
            {
                query = query.Where(f => f.RrNumber.ToLower().Contains(searchTerm) || 
                                         f.FileName.ToLower().Contains(searchTerm) ||
                                         f.Sector.ToLower().Contains(searchTerm));
            }
        }

        if (!string.IsNullOrEmpty(sectorFilter) && sectorFilter != "All Sectors")
        {
            query = query.Where(f => f.Sector == sectorFilter);
        }

        if (yearFilter.HasValue)
        {
            query = query.Where(f => f.AddedDateTime.Year == yearFilter.Value);
        }

        if (monthFilter.HasValue)
        {
            query = query.Where(f => f.AddedDateTime.Month == monthFilter.Value);
        }

        if (isRecentOnly)
        {
            var yesterday = DateTime.Now.AddDays(-1);
            query = query.Where(f => f.AddedDateTime >= yesterday);
        }

        if (isAvailableOnly)
        {
            query = query.Where(f => f.CurrentStatus == "Available" && !f.IsRemoved);
        }

        if (isRemovedOnly)
        {
            query = query.Where(f => f.IsRemoved);
        }
        

        query = query.OrderByDescending(f => f.SerialNumber);

        int totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<string>> GetExistingSectorsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.FileRecords
            .Select(f => f.Sector)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }
    
    
    // Circulation
    public async Task<List<BorrowRecord>> GetActiveLoansAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BorrowRecords
            .Include(b => b.File)
            .Where(b => b.IsReturned == false)
            .OrderByDescending(b => b.BorrowedDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> IssueFileAsync(string rrNumber, string borrowerName, string sectorColorHex)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.FileRecords.FirstOrDefaultAsync(f => f.RrNumber == rrNumber);

        if (file == null) return (false, "Error: File not found in the vault");
        if (file.CurrentStatus == "Borrowed") return (false, "Error: File is already borrowed");
        if (file.IsRemoved) return (false, "Error, File has been disposed/archived");

        file.CurrentStatus = "Borrowed";

        var record = new BorrowRecord
        {
            FileSerialNumber = file.SerialNumber,
            BorrowerName = borrowerName,
            BorrowedDate = DateTime.Now.Date,
            IsReturned = false,
            
            SnapshotRrNumber =  file.RrNumber,
            SnapshotFileName = file.FileName,
            SnapshotSector = file.Sector,
            SnapshotSectorColor = sectorColorHex,
            SnapshotSubjectNumber =  file.SubjectNumber,
            SnapshotFileType =  file.FileType,
            SnapshotStartDate =  file.StartDate,
            SnapshotEndDate =  file.EndDate,
            SnapshotTotalPages =   file.TotalPages,
            SnapshotShelfNumber =   file.ShelfNumber,
            SnapshotDeckNumber =   file.DeckNumber,
            SnapshotFileNumber = file.FileNumber
        };

        context.BorrowRecords.Add(record);
        await context.SaveChangesAsync();
        await LogActivityAsync(file.SerialNumber.ToString(), file.RrNumber, "Lend");

        return (true, $"Success: File {rrNumber} has been issued to {borrowerName}.");
    }

    public async Task<(bool Success, string Message)> ReturnFileASync(string rrNumber)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.FileRecords.FirstOrDefaultAsync(f => f.RrNumber == rrNumber);

        if (file == null) return (false, "Error: File not found in the vault.");
        if (file.CurrentStatus != "Borrowed") return (false, "Error File is not currently borrowed.");

        file.CurrentStatus = "Available";

        var activeRecord = await context.BorrowRecords
            .FirstOrDefaultAsync(b => b.FileSerialNumber == file.SerialNumber && b.IsReturned == false);

        if (activeRecord != null)
        {
            activeRecord.IsReturned = true;
            activeRecord.ReturnedDate = DateTime.Now.Date;
        }

        await context.SaveChangesAsync();
        await LogActivityAsync(file.SerialNumber.ToString(), file.RrNumber, "Receive");

        return (true, $"Success: File {rrNumber} has been returned to the vault.");
    }
    

    public async Task<(List<BorrowRecord> Items, int TotalCount)> GetBorrowHistoryPaginatedAsync(string searchTerm,
        int pageNumber, int pageSize)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.BorrowRecords.Include(b => b.File).AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(b=> b.SnapshotRrNumber.ToLower().Contains(searchTerm));
        }
        
        query = query.OrderByDescending(b => b.Id);
        
        int totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        
        return (items, totalCount);
    }
    
    // Entry
    public async Task<(bool Success, string Message)> AddNewFileAsync(FileRecord newFile)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            bool exists = await context.FileRecords.AnyAsync(f => f.RrNumber == newFile.RrNumber);
            if (exists)
            {
                return (false, $"Error: A file with RR Number '{newFile.RrNumber}' already exists.");
            }

            int maxSerial = 0;
            if (await context.FileRecords.AnyAsync())
            {
                maxSerial = await context.FileRecords.MaxAsync(f => f.SerialNumber);
            }
            
            newFile.SerialNumber = maxSerial + 1;
            newFile.CurrentStatus = "Available";
            newFile.IsRemoved = false;
            newFile.AddedDateTime = DateTime.Now;

            context.FileRecords.Add(newFile);

            var history = new EntryHistoryRecord
            {
                FileSerialNumber = newFile.SerialNumber,
                RrNumber = newFile.RrNumber,
                SubjectNumber = newFile.SubjectNumber,
                FileName = newFile.FileName,
                Sector = newFile.Sector,
                Status = newFile.CurrentStatus,
                
                FileType = newFile.FileType,
                StartDate = newFile.StartDate,
                EndDate = newFile.EndDate,
                TotalPages =  newFile.TotalPages,
                ShelfNumber = newFile.ShelfNumber,
                DeckNumber = newFile.DeckNumber,
                FileNumber = newFile.FileNumber,
                
                
                ActionType = "Created",
                Timestamp = DateTime.Now
            };
            context.EntryHistoryRecords.Add(history);
            
            await context.SaveChangesAsync();
            
            // Dashboard export
            
            await LogActivityAsync(newFile.SerialNumber.ToString(), newFile.RrNumber, "Entry");

            return (true, $"Success: File '{newFile.RrNumber}' has been added to the vault.");
        }
        catch (System.Exception ex)
        {
            string exactError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return (false, $"Database Error : {exactError}");
        }
        
    }

    public async Task<FileRecord> GetFileByRrNumberAsync(string rrNumber)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.FileRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.RrNumber == rrNumber);
    }

    public async Task<(bool Success, string Message)> UpdateFileAsync(FileRecord updatedFile)
    {
        try
        {
            if (updatedFile.IsRemoved)
            {
                return (false,
                    "Error: This file has been disposed and it permanently locked. Modifications are forbidden");
            }
            
            using var context = await _contextFactory.CreateDbContextAsync();

            var oldRecord = await context.FileRecords.AsNoTracking().FirstOrDefaultAsync(f => f.SerialNumber == updatedFile.SerialNumber);
            string oldRr = oldRecord?.RrNumber ?? updatedFile.RrNumber;
            
            context.FileRecords.Update(updatedFile);

            var history = new EntryHistoryRecord
            {
                FileSerialNumber =  updatedFile.SerialNumber,
                RrNumber = updatedFile.RrNumber,
                SubjectNumber = updatedFile.SubjectNumber,
                FileName = updatedFile.FileName,
                Sector = updatedFile.Sector,
                Status = updatedFile.CurrentStatus,
                
                FileType = updatedFile.FileType,
                StartDate = updatedFile.StartDate,
                EndDate = updatedFile.EndDate,
                TotalPages = updatedFile.TotalPages,
                ShelfNumber = updatedFile.ShelfNumber,
                DeckNumber = updatedFile.DeckNumber,
                FileNumber = updatedFile.FileNumber,
                
                ActionType = "Edited",
                Timestamp = DateTime.Now
            };
            
            context.EntryHistoryRecords.Add(history);

            await context.SaveChangesAsync();
            
            string formattedRr = oldRr == updatedFile.RrNumber
                ? updatedFile.RrNumber
                : $"{oldRr} → {updatedFile.RrNumber}";

            await LogActivityAsync(updatedFile.SerialNumber.ToString(), formattedRr, "Edit/Amend");
            
            return (true, $"Success: File '{updatedFile.RrNumber}' has been updated.");
        }
        catch (System.Exception ex)
        {
            return (false, $"Database Error : {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}");
        }
    }

    public async Task<List<EntryHistoryRecord>> GetEntryHistoryAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EntryHistoryRecords
            .OrderByDescending(h => h.Timestamp)
            .Take(100)
            .ToListAsync();
    }

    public async Task<EntryHistoryRecord> GetPreviousHistoryRecordAsync(int fileSerialNumber, DateTime currentTimestamp)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.EntryHistoryRecords
            .Where (h => h.FileSerialNumber == fileSerialNumber && h.Timestamp < currentTimestamp)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<(List<EntryHistoryRecord> Items, int TotalCount)> GetEntryHistoryPaginatedAsync(string searchTerm, int pageNumber, int pageSize)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.EntryHistoryRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(h => h.RrNumber.ToLower().Contains(searchTerm));
        }
        
        query = query.OrderByDescending(h => h.Timestamp);
        
        int totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(); 
        return (items, totalCount);
    }
    
    // Records
    private IQueryable<FileRecord> BuildReportQuery(
        IQueryable<FileRecord> query,
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate,
        DateTime? addedDateFrom, DateTime? addedDateTo)
    {
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
        
        if (addedDateFrom.HasValue) query = query.Where(f => f.AddedDateTime >= addedDateFrom.Value);
        if (addedDateTo.HasValue) query = query.Where(f => f.AddedDateTime <= addedDateTo.Value);
        
        return query.OrderBy(f => f.SerialNumber);
    }

    public async Task<(List<FileRecord> Items, int TotalCount)> GetFilteredPreviewPaginatedAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate,
        DateTime? addedDateFrom, DateTime? addedDateTo,
        int pageNumber, int pageSize)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = BuildReportQuery(
            context.FileRecords.AsQueryable(),
            serialNumber, rrNumber, sector, subjectNumber, fileName, fileType, startDate, endDate, 
            totalPages, shelfNumber, deckNumber, fileNumber, currentStatus, isRemoved, toBeRemovedDate, removedDate,
            addedDateFrom, addedDateTo);
        
        int totalCount = await query.CountAsync();
        
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<FileRecord>> GetFullFilteredExportAsync(
        string serialNumber, string rrNumber, string sector, string subjectNumber,
        string fileName, string fileType, DateTime? startDate, DateTime? endDate,
        string totalPages, string shelfNumber, string deckNumber, string fileNumber,
        string currentStatus, bool? isRemoved, DateTime? toBeRemovedDate, DateTime? removedDate,
        DateTime? addedDateFrom, DateTime? addedDateTo)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = BuildReportQuery(
            context.FileRecords.AsQueryable(),
            serialNumber, rrNumber, sector, subjectNumber, fileName, fileType, startDate, endDate, 
            totalPages, shelfNumber, deckNumber, fileNumber, currentStatus, isRemoved, toBeRemovedDate, removedDate,
            addedDateFrom, addedDateTo);

        return await query.ToListAsync();
    }
    
    // SQL Backup
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
    
    
    // ---- Disposal ----

    public async Task<(bool Success, string Message)> UpdateDisposalQueueAsync(string rrNumber,
        DateTime? toBeRemovedDate)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.FileRecords.FirstOrDefaultAsync(f => f.RrNumber == rrNumber);

        if (file == null) return (false, "File not found.");
        if (file.IsRemoved) return (false, "File is already disposed and locked.");

        file.ToBeRemovedDate = toBeRemovedDate;
        await context.SaveChangesAsync();

        if (toBeRemovedDate.HasValue)
        {
            await LogActivityAsync(file.SerialNumber.ToString(), file.RrNumber, "Added To be removed queue");
        }
        
        string msg = toBeRemovedDate.HasValue
            ? $"File scheduled for disposal on {toBeRemovedDate.Value:yyyy-MM-dd}." 
            : "File removed from disposal queue.";
        
        return  (true, msg);

    }


    public async Task<(bool Success, string Message)> DisposeFileAsync(string rrNumber, string reason,
        string authorizedBy)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.FileRecords.FirstOrDefaultAsync(f => f.RrNumber == rrNumber);

        if (file == null) return (false, "File not found");
        if (file.IsRemoved) return (false, "FIle is already disposed.");
        if (file.CurrentStatus == "Borrowed") return (false, "Cannot dispose a file that is currently borrowed.");

        if (file.ToBeRemovedDate == null)
        {
            file.ToBeRemovedDate = DateTime.Now.Date;
        }

        file.IsRemoved = true;
        file.CurrentStatus = "Removed";
        file.RemovedDate = DateTime.Now.Date;

        var disposalRecord = new DisposedRecord
        {
            FIleSerialNumber = file.SerialNumber,
            Reason = reason,
            AuthorizedBy = authorizedBy,
            ToBeRemovedDate = file.ToBeRemovedDate,
            RemovedDate = file.RemovedDate.Value
        };
        context.DisposedRecords.Add(disposalRecord);

        var history = new EntryHistoryRecord
        {
            FileSerialNumber = file.SerialNumber,
            RrNumber = file.RrNumber,
            SubjectNumber = file.SubjectNumber,
            FileName = file.FileName,
            Sector = file.Sector,
            Status = "Removed",
            ActionType = "Disposed",
            Timestamp = DateTime.Now
        };
        await context.SaveChangesAsync();
        
        await LogActivityAsync(file.SerialNumber.ToString(), file.RrNumber, "Dispose");
        
        return (true, $"File {rrNumber} has been permanently locked and disposed.");
    }

    public async Task<List<FileRecord>> GetPendingDisposalsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FileRecords
            .Where(f => f.ToBeRemovedDate != null && f.IsRemoved == false)
            .OrderBy(f => f.ToBeRemovedDate)
            .ToListAsync();
    }

    public async Task<List<DisposedRecord>> GetDisposedHistoryAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DisposedRecords
            .Include(d => d.File)
            .OrderByDescending(d => d.RemovedDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> RecoverFileAsync(string rrNumber)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var file = await context.FileRecords.FirstOrDefaultAsync(f => f.RrNumber == rrNumber);
        
        if (file == null) return (false, "File not found.");
        if (file.IsRemoved) return (false, "Cannot recover a permanently disposed file.");

        file.ToBeRemovedDate = null;
        await context.SaveChangesAsync();
        
        await LogActivityAsync(file.SerialNumber.ToString(), file.RrNumber, "taken back from the removed queue");
        
        return (true, $"File {rrNumber} recovered back to active vault.");
    }

    public async Task<int> GetTodayDisposalCountAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Now.Date;
        return await context.FileRecords
            .CountAsync(f => f.ToBeRemovedDate != null && f.ToBeRemovedDate <= today && f.IsRemoved == false);
    }
    
}