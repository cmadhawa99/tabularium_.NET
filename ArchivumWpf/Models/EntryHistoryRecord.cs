using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivumWpf.Models;

[Table("entry_history_record")]

public class EntryHistoryRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int FileSerialNumber { get; set; }
    
    public string RrNumber { get; set; }
    public string SubjectNumber { get; set; }
    public string FileName { get; set; }
    public string Sector { get; set; }
    public string Status { get; set; }
    
    public string FileType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TotalPages { get; set; }
    public int? ShelfNumber { get; set; }
    public int? DeckNumber { get; set; }
    public int? FileNumber { get; set; }
    
    public string ActionType { get; set; }
    public DateTime Timestamp { get; set; } =  DateTime.Now;
}