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

    public required string RrNumber { get; set; } = string.Empty;
    public string? SubjectNumber { get; set; }
    public string? FileName { get; set; } = string.Empty;
    public string? Sector { get; set; } = string.Empty;
    public string? Status { get; set; } = string.Empty;

    public string? FileType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TotalPages { get; set; }
    public string? ShelfNumber { get; set; }
    public string? DeckNumber { get; set; }
    public string? FileNumber { get; set; }

    public string ActionType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
}