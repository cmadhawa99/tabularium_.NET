using System.ComponentModel.DataAnnotations;

namespace Archivum.Models;

public class AppSecurityMeta
{
    [Key] public int Id { get; set; }

    public string EncryptedCanary { get; set; } = string.Empty;
}