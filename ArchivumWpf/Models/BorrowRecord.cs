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
    
    [Column("file_rr_number")]
    public string FileRrNumber { get; set; } = null!;

    [Column("borrower_name")]
    public string BorrowerName { get; set; } = null!;

    [Column("borrowed_date")]
    public DateTime BorrowedDate { get; set; }

    [Column("returned_date")]
    public DateTime? ReturnedDate { get; set; }

    [Column("is_returned")]
    public bool IsReturned { get; set; } = false;
    
    [ForeignKey(nameof(FileRrNumber))]
    public virtual FileRecord File { get; set; } = null!;
}