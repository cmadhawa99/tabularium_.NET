namespace ArchivumWpf.Models;

public class UserPreferences
{
    public string OrganizationName { get; set; } = "Weligepola Divisional Council";
    public string AppTitle { get; set; } = "Archivum";
    public string CurrentUser { get; set; } = "Admin";
    public string Language { get; set; } = "English";
    public string Theme { get; set; } = "Dark";
    public int DefaultPaginationSize { get; set; } = 50;
    public string DefaultExportDirectory { get; set; } = "C:\\";
    
    public bool AutoBackupEnabled { get; set; } = false;
    public string AutoBackupDirectory { get; set; } = "C:\\ArchiveBackups\\";
    
}