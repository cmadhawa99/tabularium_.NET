using System.ComponentModel.DataAnnotations;

namespace ArchivumWpf.Models;

public class AppSecurityMeta
{
    [Key]
    public int Id { get; set; }
    
    public string EncryptedCanary { get; set; } = string.Empty;
}