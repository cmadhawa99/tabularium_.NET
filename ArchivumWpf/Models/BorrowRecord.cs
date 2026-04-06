using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivumWpf.Models;

[Table("borrow_records")]
public class BorrowRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("file_serial_number")]
    public int FileSerialNumber { get; set; }

    [Column("borrower_name")]
    public string BorrowerName { get; set; } = null!;

    [Column("borrowed_date")]
    public DateTime BorrowedDate { get; set; }

    [Column("returned_date")]
    public DateTime? ReturnedDate { get; set; }

    [Column("is_returned")]
    public bool IsReturned { get; set; } = false;
    
    //Snapshots
    
    [Column("snapshot_rr_number")]
    public string SnapshotRrNumber { get; set; } = null!;

    [Column("snapshot_file_name")]
    public string SnapshotFileName { get; set; } = null!;

    [Column("snapshot_sector")]
    public string SnapshotSector { get; set; } = null!;
    
    [Column("snapshot_sector_color")]
    public string? SnapshotSectorColor { get; set; }

    [Column("snapshot_subject_number")]
    public string? SnapshotSubjectNumber { get; set; }

    [Column("snapshot_file_type")]
    public string? SnapshotFileType { get; set; }

    [Column("snapshot_start_date", TypeName = "date")]
    public DateTime? SnapshotStartDate { get; set; }

    [Column("snapshot_end_date", TypeName = "date")]
    public DateTime? SnapshotEndDate { get; set; }

    [Column("snapshot_total_pages")]
    public int? SnapshotTotalPages { get; set; }

    [Column("snapshot_shelf_number")]
    public int? SnapshotShelfNumber { get; set; }

    [Column("snapshot_deck_number")]
    public int? SnapshotDeckNumber { get; set; }

    [Column("snapshot_file_number")]
    public int? SnapshotFileNumber { get; set; }
    
    [ForeignKey(nameof(FileSerialNumber))]
    public virtual FileRecord File { get; set; } = null!;
}