using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Models;


namespace ArchivumWpf.Services;

public interface IArchiveService
{
    Task<DashboardStats> GetDashboardStatsAsync();   
    Task<List<FileRecord>> SearchFilesAsync(string searchTerm);
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

    public async Task<List<FileRecord>> SearchFilesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<FileRecord>();

        searchTerm = searchTerm.ToLower();
        
        return await  _context.FileRecords
            .Where(f => f.RrNumber.ToLower().Contains(searchTerm) || 
                        f.FileName.ToLower().Contains(searchTerm) ||
                        f.Sector.ToLower().Contains(searchTerm))
            .ToListAsync();
    }
}