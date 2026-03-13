using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Models;


namespace ArchivumWpf.Services;

public interface IArchiveService
{
    Task<DashboardStats> GetDashboardStatsAsync();   
    
    Task<(List<FileRecord> Items, int TotalCount)> SearchFilesPaginatedAsync(String searchTerm, int pageNumber, int pageSize);
}

public class ArchiveService : IArchiveService
{
    private readonly AppDbContext _context;

    public ArchiveService(AppDbContext context)
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
}