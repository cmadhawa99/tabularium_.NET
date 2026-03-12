using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivumWpf.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = null!;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("role")]
    public string Role { get; set; } = "Viewer";

    [Column("totp_secret")]
    public string? TotpSecret { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}