using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivumWpf.Models;

[Table("folders")]
public class Folder
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("parent_folder_id")] public int? ParentFolderId { get; set; }

    [Column("folder_name")] public string FolderName { get; set; } = null!;

    [Column("file_record_serial")] public int FileRecordSerialNumber { get; set; }

    [Column("physical_storage_id")] public Guid? PhysicalStorageId { get; set; }

    [ForeignKey(nameof(ParentFolderId))] public virtual Folder? ParentFolder { get; set; }

    public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
}