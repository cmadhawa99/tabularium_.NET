using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivumWpf.Models;

[Table("disposed_records")]
public class DisposedRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Column("file_serial_number")]
    public int FIleSerialNumber { get; set; }
    
    [Column("reason_for_disposal")]
    public string Reason { get; set; }
    
    [Column("authorized_by")]
    public string AuthorizedBy { get; set; }
    
    [Column("to_be_removed_date", TypeName = "date")]
    public DateTime? ToBeRemovedDate { get; set; }

    [Column("removed_date", TypeName = "date")]
    public DateTime? RemovedDate { get; set; } = DateTime.Now;
    
    [ForeignKey(nameof(FIleSerialNumber))]
    public virtual FileRecord File { get; set; }

}