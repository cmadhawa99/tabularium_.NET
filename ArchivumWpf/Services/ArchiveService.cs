
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Models;


namespace ArchivumWpf.Services;

public interface IArchiveService
{
    Task<DashboardStats> GetDashboardStatsAsync();   
    Task<(List<FileRecord> Items, int TotalCount)> SearchFilesPaginatedAsync(string searchTerm, int pageNumber, int pageSize); 
    Task<List<BorrowRecord>> GetActiveLoansAsync();
    Task<(bool Success, string Message)> IssueFileAsync(string rrNumber, string borrowerName);
    Task<(bool Success, string Message)> ReturnFileASync(string rrNumber);
    Task<(bool Success, string Message)> AddNewFileAsync(FileRecord newFile);

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
}
