namespace Archivum.Models;

public class ConnectionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string DbHost { get; set; } = string.Empty;
    public string DbName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}