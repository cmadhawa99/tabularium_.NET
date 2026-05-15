using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ArchivumWpf.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
    public DbSet<EntryHistoryRecord> EntryHistoryRecords { get; set; }
    public DbSet<DisposedRecord> DisposedRecords { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<AppSecurityMeta> AppSecurityMetas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var masterKey = KeyVaultService.GetMasterKey();
        var cryptoService = new CryptoService(masterKey);

        var stringEncryptionConverter = new ValueConverter<string, string>(
            v => cryptoService.Encrypt(v),
            v => cryptoService.Decrypt(v)
            );
        
        modelBuilder.Entity<FileRecord>()
            .Property(f => f.FileName)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<FileRecord>()
            .Property(f => f.SubjectNumber)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<FileRecord>()
            .Property(f => f.FileType)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<FileRecord>()
            .Property(f => f.FileNumber)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<User>()
            .Property(u => u.TotpSecret)
            .HasConversion(stringEncryptionConverter);
        
        //Borrow
        
        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.BorrowerName)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotFileName)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotSubjectNumber)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotFileType)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotFileNumber)
            .HasConversion(stringEncryptionConverter);
        
        //Disposed
        
        modelBuilder.Entity<DisposedRecord>()
            .Property(d => d.Reason)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<DisposedRecord>()
            .Property(d => d.AuthorizedBy)
            .HasConversion(stringEncryptionConverter);
        
        //EntryHistory
        
        modelBuilder.Entity<EntryHistoryRecord>()
            .Property(e => e.SubjectNumber)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<EntryHistoryRecord>()
            .Property(e => e.FileName)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<EntryHistoryRecord>()
            .Property(e => e.FileType)
            .HasConversion(stringEncryptionConverter);
        
        modelBuilder.Entity<EntryHistoryRecord>()
            .Property(e => e.FileNumber)
            .HasConversion(stringEncryptionConverter);
        
        
        
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        { 
            string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (File.Exists(appSettingsPath))
            {
                var jsonNode = JsonNode.Parse(File.ReadAllText(appSettingsPath));
                string encryptedConnString = jsonNode?["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(encryptedConnString))
                {
                    var masterKey = KeyVaultService.GetMasterKey();
                    var cryptoService = new CryptoService(masterKey);
                    string plainTextConnString = cryptoService.Decrypt(encryptedConnString);
                    
                    optionsBuilder.UseNpgsql(plainTextConnString);
                }
            }
        }
    }
    
    
}