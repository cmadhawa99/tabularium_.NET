using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using ArchivumWpf.Models;
using Bogus;

namespace ArchivumWpf.Services;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    
    public DatabaseSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedFileRecordsAsync(int count = 100)
    {
        int startingSerial = _context.FileRecords.Any()
            ? _context.FileRecords.Max(f => f.SerialNumber) + 1
            : 100000;
        
        var sectors = new[] { "පාලන", "සෞඛ්‍ය", "සංවර්ධන", "ආදායම්", "ගිණුම්", "සං‍රක්ෂණ" };
        var fileTypes = new[] { "සාමාන්‍ය (General)", "රහසිගත (Confidential)", "මූල්‍ය (Financial)", "චක්‍රලේඛ (Circular)", "පෞද්ගලික (Personal)" };
        var statuses = new[] { "Available", "Available", "Available", "Available", "Borrowed" };
        
        var sinhalaNames = new[] { 
            "කමල් පෙරේරා", "නිමල් සිල්වා", "සුනිල් ශාන්ත", "චාමර විදානගේ", 
            "අමාලි රත්නායක", "සමන් කුමාර", "දිනුෂා ප්‍රනාන්දු", "රුවන් පතිරණ" 
        };
        
        var disposalReasons = new[] { 
            "කාලසීමාව ඉක්මවා යාම (Expired)", 
            "ලිපිගොනුව හානි වී ඇත (Damaged)", 
            "අනවශ්‍ය ලිපිගොනු (Unnecessary)", 
            "ඩිජිටල්කරණය කරන ලදි (Digitized)" 
        };
        
        var sinhalaFileNames = new[]
        {
            "ප්‍රාදේශීය සභා මාසික රැස්වීම් වාර්තා",
            "2023 වාර්ෂික අයවැය ඇස්තමේන්තු",
            "පරිසර ආරක්ෂණ බලපත්‍ර අයදුම්පත්",
            "වීථි පහන් නඩත්තු කිරීමේ වාර්තා",
            "ගොඩනැගිලි සැලසුම් අනුමත කිරීම්",
            "සේවක නිවාඩු සහ වැටුප් විස්තර",
            "වරිපනම් බදු එකතු කිරීමේ ලේඛන",
            "ඝන අපද්‍රව්‍ය කළමනාකරණ ව්‍යාපෘතිය",
            "පෙරපාසල් සංවර්ධන ආධාර ගොනුව",
            "ප්‍රජා ජල ව්‍යාපෘති ලිපිගොනු"
        };
        
        var subjectPrefixes = new[] { "ADM", "HLT", "DEV", "REV", "ACC", "ARC" };
        
        var allActivityLogs = new List<ActivityLog>();
        var allEntryHistories = new List<EntryHistoryRecord>();
        var allDisposedRecords = new List<DisposedRecord>();
        
        int currentSerial = startingSerial;
        var faker = new Faker();
        
        var fileFaker = new Faker<FileRecord>()
            .RuleFor(f => f.SerialNumber, f => currentSerial++)
            .RuleFor(f => f.RrNumber, (f, u) => $"24/{u.Sector}/{u.SerialNumber}")
            .RuleFor(f => f.Sector, f => f.PickRandom(sectors))
            .RuleFor(f => f.SubjectNumber, f => $"{f.PickRandom(subjectPrefixes)}-{f.Random.Number(100, 999)}")
            .RuleFor(f => f.FileName, f => f.PickRandom(sinhalaFileNames))
            .RuleFor(f => f.FileType, f => f.PickRandom(fileTypes))
            .RuleFor(f => f.StartDate, f => f.Date.Past(3)) 
            .RuleFor(f => f.EndDate, (f, u) => f.Date.Between(u.StartDate.Value, DateTime.Now))
            .RuleFor(f => f.TotalPages, f => f.Random.Number(1, 500))
            .RuleFor(f => f.ShelfNumber, f => f.Random.Number(1, 15).ToString())
            .RuleFor(f => f.DeckNumber, f => $"{f.Random.String2(1, "ABC")}-{f.Random.Number(1, 8)}")
            .RuleFor(f => f.FileNumber, f => f.Random.Number(1, 150).ToString())
            .RuleFor(f => f.CurrentStatus, f => f.PickRandom(statuses))
            .RuleFor(f => f.IsRemoved, f => f.Random.Bool(0.05f)) 
            .RuleFor(f => f.AddedDateTime, f => f.Date.Recent(60));
        
        List<FileRecord> fakeFiles = fileFaker.Generate(count);

        foreach (var file in fakeFiles)
        {
            allEntryHistories.Add(new EntryHistoryRecord
            {
                FileSerialNumber = file.SerialNumber,
                RrNumber = file.RrNumber,
                SubjectNumber = file.SubjectNumber,
                FileName = file.FileName,
                Sector = file.Sector,
                Status = file.CurrentStatus,
                FileType = file.FileType,
                StartDate = file.StartDate,
                EndDate = file.EndDate,
                TotalPages = file.TotalPages,
                ShelfNumber = file.ShelfNumber,
                DeckNumber = file.DeckNumber,
                FileNumber = file.FileNumber,
                ActionType = "Created",
                Timestamp = file.AddedDateTime
            });
            
            allActivityLogs.Add(new ActivityLog
            {
                SerialNumber = file.SerialNumber.ToString(), 
                RrNumber = file.RrNumber,
                ActionType = "Created",
                Timestamp = file.AddedDateTime
            });

            if (file.CurrentStatus == "Borrowed")
            {
                var borrowDate = faker.Date.Recent(15, DateTime.Now);
                
                file.BorrowHistory.Add (new BorrowRecord
                {
                    BorrowerName = faker.PickRandom(sinhalaNames),
                    BorrowedDate = borrowDate,
                    IsReturned = false,
                    ReturnedDate = null,
                    // Snapshots required by model
                    SnapshotRrNumber = file.RrNumber,
                    SnapshotFileName = file.FileName,
                    SnapshotSector = file.Sector,
                    SnapshotSubjectNumber = file.SubjectNumber,
                    SnapshotFileType = file.FileType,
                    SnapshotStartDate = file.StartDate,
                    SnapshotEndDate = file.EndDate,
                    SnapshotTotalPages = file.TotalPages,
                    SnapshotShelfNumber = file.ShelfNumber,
                    SnapshotDeckNumber = file.DeckNumber,
                    SnapshotFileNumber = file.FileNumber
                });
                
                allActivityLogs.Add(new ActivityLog
                {
                    SerialNumber = file.SerialNumber.ToString(),
                    RrNumber = file.RrNumber,
                    ActionType = "Borrowed",
                    Timestamp = borrowDate
                });
            }
            else
            {
                if (faker.Random.Bool(0.3f))
                {
                    var borrowDate = faker.Date.Past(1, DateTime.Now.AddDays(-10));
                    var returnDate = faker.Date.Between(borrowDate, DateTime.Now);

                    file.BorrowHistory.Add(new BorrowRecord
                    {
                        BorrowerName = faker.PickRandom(sinhalaNames),
                        BorrowedDate = borrowDate,
                        IsReturned = true,
                        ReturnedDate = returnDate,
                        SnapshotRrNumber = file.RrNumber,
                        SnapshotFileName = file.FileName,
                        SnapshotSector = file.Sector,
                        SnapshotSubjectNumber = file.SubjectNumber,
                        SnapshotFileType = file.FileType,
                        SnapshotStartDate = file.StartDate,
                        SnapshotEndDate = file.EndDate,
                        SnapshotTotalPages = file.TotalPages,
                        SnapshotShelfNumber = file.ShelfNumber,
                        SnapshotDeckNumber = file.DeckNumber,
                        SnapshotFileNumber = file.FileNumber
                    });
                }
            }

            if (file.IsRemoved)
            {
                file.CurrentStatus = "Disposed";
                file.ToBeRemovedDate = faker.Date.Past(1);
                file.RemovedDate = faker.Date.Between(file.ToBeRemovedDate.Value, DateTime.Now);

                allDisposedRecords.Add(new DisposedRecord
                {
                    FIleSerialNumber = file.SerialNumber,
                    Reason = faker.PickRandom(disposalReasons),
                    AuthorizedBy = faker.PickRandom(sinhalaNames),
                    ToBeRemovedDate = file.ToBeRemovedDate,
                    RemovedDate = file.RemovedDate
                });
                
                allActivityLogs.Add(new ActivityLog
                {
                    SerialNumber = file.SerialNumber.ToString(),
                    RrNumber = file.RrNumber,
                    ActionType = "Disposed",
                    Timestamp = file.RemovedDate.Value
                });
            }
        }
        
        _context.FileRecords.AddRange(fakeFiles);
        
        _context.Set<EntryHistoryRecord>().AddRange(allEntryHistories);
        _context.Set<ActivityLog>().AddRange(allActivityLogs);
        _context.Set<DisposedRecord>().AddRange(allDisposedRecords);

        await _context.SaveChangesAsync();

    }
    
}

