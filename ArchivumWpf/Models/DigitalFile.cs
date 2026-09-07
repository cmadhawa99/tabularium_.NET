using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivumWpf.Models;

[Table("digital_files")]
public class DigitalFile
{
    [Key] [Column("id")] public Guid Id { get; set; }

    [Column("folder_id")] public int FolderId { get; set; }

    [Required]
    [Column("original_file_name")]
    public string OriginalFileName { get; set; } = null!;

    [Required]
    [Column("physical_file_name")]
    public string PhysicalFileName { get; set; } = null!;

    [Column("file_size")] public long FileSize { get; set; }

    [Column("mime_type")] public string MimeType { get; set; } = null!;

    [Required] [Column("encrypted_dek")] public string EncryptedDEK { get; set; } = null!;

    [Required] [Column("iv")] public string IV { get; set; } = null!;

    [Column("record_storage_id")] public Guid RecordStorageId { get; set; }

    [NotMapped] public bool IsMissing { get; set; }

    [ForeignKey(nameof(FolderId))] public virtual Folder Folder { get; set; } = null!;
}