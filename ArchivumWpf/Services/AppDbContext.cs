using ArchivumWpf.Models;
using Microsoft.EntityFrameworkCore;

namespace ArchivumWpf.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<FileRecord> FileRecords { get; set; }
    public DbSet<BorrowRecord> BorrowRecords { get; set; }
    public DbSet<User> Users { get; set; }
    
}