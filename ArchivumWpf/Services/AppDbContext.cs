using System;
using System.IO;
using System.Text.Json.Nodes;
using ArchivumWpf.Models;
using Microsoft.EntityFrameworkCore;

namespace ArchivumWpf.Services;

public class AppDbContext : DbContext
{
    public AppDbContext() {}
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<FileRecord> FileRecords { get; set; }
    public DbSet<BorrowRecord> BorrowRecords { get; set; }
    public DbSet<User> Users { get; set; }
    //public DbSet<DisposedRecord> DisposedRecords { get; set; }
    
    public DbSet<EntryHistoryRecord> EntryHistoryRecords { get; set; }
    
    public DbSet<DisposedRecord> DisposedRecords { get; set; }
    
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (File.Exists(appSettingsPath))
            {
                var jsonNode = JsonNode.Parse(File.ReadAllText(appSettingsPath));
                string connString = jsonNode?["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(connString))
                {
                    optionsBuilder.UseNpgsql(connString);
                }
            }
        }
    }
}