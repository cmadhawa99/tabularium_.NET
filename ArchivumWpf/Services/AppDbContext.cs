using System.IO;
using System.Text.Json.Nodes;
using ArchivumWpf.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArchivumWpf.Services;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

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
    public DbSet<Folder> Folders { get; set; }
    public DbSet<DigitalFile> DigitalFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var masterKey = KeyVaultService.GetMasterKey();
        var cryptoService = new CryptoService(masterKey);

        var stringEncryptionConverter = new ValueConverter<string, string>(
            v => cryptoService.Encrypt(v),
            v => cryptoService.Decrypt(v)
        );
        
        var nullableStringEncryptionConverter = new ValueConverter<string?, string?>(
            v => v == null ? null : cryptoService.Encrypt(v),
            v => v == null ? null : cryptoService.Decrypt(v));


        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .HasConversion(stringEncryptionConverter);

        modelBuilder.Entity<User>()
            .Property(u => u.PasswordHash)
            .HasConversion(stringEncryptionConverter);

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion(stringEncryptionConverter);


        modelBuilder.Entity<FileRecord>()
            .Property(f => f.FileName)
            .HasConversion(stringEncryptionConverter);

        modelBuilder.Entity<FileRecord>()
            .Property(f => f.SubjectNumber)
            .HasConversion(nullableStringEncryptionConverter);

        modelBuilder.Entity<FileRecord>()
            .Property(f => f.FileType)
            .HasConversion(nullableStringEncryptionConverter);

        modelBuilder.Entity<FileRecord>()
            .Property(f => f.FileNumber)
            .HasConversion(nullableStringEncryptionConverter);


        //Borrow

        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.BorrowerName)
            .HasConversion(stringEncryptionConverter);

        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotFileName)
            .HasConversion(stringEncryptionConverter);

        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotSubjectNumber)
            .HasConversion(nullableStringEncryptionConverter);

        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotFileType)
            .HasConversion(nullableStringEncryptionConverter);

        modelBuilder.Entity<BorrowRecord>()
            .Property(b => b.SnapshotFileNumber)
            .HasConversion(nullableStringEncryptionConverter);

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
            .HasConversion(nullableStringEncryptionConverter);

        modelBuilder.Entity<EntryHistoryRecord>()
            .Property(e => e.FileName)
            .HasConversion(nullableStringEncryptionConverter);

        modelBuilder.Entity<EntryHistoryRecord>()
            .Property(e => e.FileType)
            .HasConversion(nullableStringEncryptionConverter);

        modelBuilder.Entity<EntryHistoryRecord>()
            .Property(e => e.FileNumber)
            .HasConversion(nullableStringEncryptionConverter);


        modelBuilder.Entity<Folder>()
            .Property(f => f.FolderName)
            .HasConversion(stringEncryptionConverter);

        modelBuilder.Entity<DigitalFile>()
            .Property(d => d.OriginalFileName)
            .HasConversion(stringEncryptionConverter);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (File.Exists(appSettingsPath))
            {
                var jsonNode = JsonNode.Parse(File.ReadAllText(appSettingsPath));
                var encryptedConnString = jsonNode?["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(encryptedConnString))
                {
                    var masterKey = KeyVaultService.GetMasterKey();
                    var cryptoService = new CryptoService(masterKey);
                    var plainTextConnString = cryptoService.Decrypt(encryptedConnString);

                    optionsBuilder.UseNpgsql(plainTextConnString);
                }
            }
        }
    }
}