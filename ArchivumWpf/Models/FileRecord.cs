using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivumWpf.Models;

[Table("file_records")]
public class FileRecord
{
    [Key]
    [Column("rr_number")]
    public string RrNumber { get; set; } = null!;

    [Column("serial_number")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int SerialNumber { get; set; }

    [Column("sector")]
    public string Sector { get; set; } = null!;

    [Column("subject_number")]
    public string? SubjectNumber { get; set; }

    [Column("file_name")]
    public string FileName { get; set; } = null!;

    [Column("file_type")]
    public string? FileType { get; set; }

    [Column("start_date", TypeName = "date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime? EndDate { get; set; }

    [Column("total_pages")]
    public int? TotalPages { get; set; }

    [Column("shelf_number")]
    public int? ShelfNumber { get; set; }

    [Column("deck_number")]
    public int? DeckNumber { get; set; }

    [Column("file_number")]
    public int? FileNumber { get; set; }

    [Column("current_status")]
    public string CurrentStatus { get; set; } = "Available";

    [Column("to_be_removed_date", TypeName =  "date")]
    public DateTime? ToBeRemovedDate { get; set; }

    [Column("removed_date", TypeName ="date")]
    public DateTime? RemovedDate { get; set; }

    [Column("is_removed")]
    public bool IsRemoved { get; set; } = false;
    
    public virtual ICollection<BorrowRecord> BorrowHistory { get; set; } = new List<BorrowRecord>();
}