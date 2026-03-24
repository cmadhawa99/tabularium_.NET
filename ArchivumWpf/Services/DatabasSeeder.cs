// This is an AI generated test code

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        // 1. Figure out the next starting Serial Number
        int startingSerial = _context.FileRecords.Any() 
            ? _context.FileRecords.Max(f => f.SerialNumber) + 1 
            : 100000;

        // 2. Authentic Sinhala Government Administrative Arrays
        var sectors = new[] { "පාලන", "සෞඛ්‍ය", "සංවර්ධන", "ආදායම්", "ගිණුම්", "සං‍රක්ෂණ" };
        var fileTypes = new[] { "සාමාන්‍ය (General)", "රහසිගත (Confidential)", "මූල්‍ය (Financial)", "චක්‍රලේඛ (Circular)", "පෞද්ගලික (Personal)" };
        var statuses = new[] { "Available", "Available", "Available", "Available", "Borrowed" }; // 80% chance of being Available

        // Realistic Sinhala File/Folder Names for a Divisional Council
        var sinhalaFileNames = new[]
        {
            "ප්‍රාදේශීය සභා මාසික රැස්වීම් වාර්තා", // Monthly meeting reports
            "2023 වාර්ෂික අයවැය ඇස්තමේන්තු", // 2023 Annual budget estimates
            "පරිසර ආරක්ෂණ බලපත්‍ර අයදුම්පත්", // Environmental protection license applications
            "වීථි පහන් නඩත්තු කිරීමේ වාර්තා", // Street lamp maintenance reports
            "ගොඩනැගිලි සැලසුම් අනුමත කිරීම්", // Building plan approvals
            "සේවක නිවාඩු සහ වැටුප් විස්තර", // Employee leave and salary details
            "වරිපනම් බදු එකතු කිරීමේ ලේඛන", // Assessment tax collection records
            "ඝන අපද්‍රව්‍ය කළමනාකරණ ව්‍යාපෘතිය", // Solid waste management project
            "පෙරපාසල් සංවර්ධන ආධාර ගොනුව", // Preschool development aids
            "ප්‍රජා ජල ව්‍යාපෘති ලිපිගොනු", // Community water project files
            "මාර්ග සංවර්ධන සහ කොන්ත්‍රාත් වාර්තා", // Road development & contract reports
            "ව්‍යාපාරික බලපත්‍ර නිකුත් කිරීම", // Business license issuances
            "සුනඛ ලියාපදිංචි කිරීමේ ලේඛනය", // Dog registration records
            "ආපදා කළමනාකරණ සහන දීමනා", // Disaster management relief funds
            "පොදු වෙළඳපොළ බදු දීමේ ගිවිසුම්" // Public market leasing agreements
        };

        var subjectPrefixes = new[] { "ADM", "HLT", "DEV", "REV", "ACC", "ARC" };

        // 3. Setup the Bogus Faker Engine
        int currentSerial = startingSerial;
        
        var fileFaker = new Faker<FileRecord>()
            .RuleFor(f => f.SerialNumber, f => currentSerial++)
            .RuleFor(f => f.RrNumber, (f, u) => $"24/{u.Sector}/{u.SerialNumber}")
            .RuleFor(f => f.Sector, f => f.PickRandom(sectors))
            // Formats like "DEV-452"
            .RuleFor(f => f.SubjectNumber, f => $"{f.PickRandom(subjectPrefixes)}-{f.Random.Number(100, 999)}")
            // Pick a realistic Sinhala file name
            .RuleFor(f => f.FileName, f => f.PickRandom(sinhalaFileNames))
            .RuleFor(f => f.FileType, f => f.PickRandom(fileTypes))
            .RuleFor(f => f.StartDate, f => f.Date.Past(3)) // Sometime in the last 3 years
            .RuleFor(f => f.EndDate, (f, u) => f.Date.Between(u.StartDate.Value, DateTime.Now))
            .RuleFor(f => f.TotalPages, f => f.Random.Number(1, 500))
            .RuleFor(f => f.ShelfNumber, f => f.Random.Number(1, 15))
            .RuleFor(f => f.DeckNumber, f => f.Random.Number(1, 8))
            .RuleFor(f => f.FileNumber, f => f.Random.Number(1, 150))
            .RuleFor(f => f.CurrentStatus, f => f.PickRandom(statuses))
            .RuleFor(f => f.IsRemoved, false)
            .RuleFor(f => f.AddedDateTime, f => f.Date.Recent(60)); // Simulates records added over the last 60 days

        // 4. Generate the records
        List<FileRecord> fakeRecords = fileFaker.Generate(count);

        // 5. Save to the database
        _context.FileRecords.AddRange(fakeRecords);
        await _context.SaveChangesAsync();
    }
}