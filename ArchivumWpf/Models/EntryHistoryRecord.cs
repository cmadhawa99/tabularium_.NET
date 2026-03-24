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
    
    public string RrNumber { get; set; }
    public string SubjectNumber { get; set; }
    public string FileName { get; set; }
    public string Sector { get; set; }
    public string Status { get; set; }
    public string ActionType { get; set; }
    public DateTime Timestamp { get; set; } =  DateTime.Now;
}