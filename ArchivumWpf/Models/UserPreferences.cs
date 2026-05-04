using System.Collections.Generic;

namespace ArchivumWpf.Models;

public class SectorItem
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#FFFFFF";
}

public class UserPreferences
{
    public string OrganizationName { get; set; } = "Weligepola Divisional Council";
    public string CurrentUser { get; set; } = "Admin";
    public string TimeFormat { get; set; } = "12-Hour (AM/PM)";
    public string Language { get; set; } = "English";
    public int DefaultPaginationSize { get; set; } = 50;
    public string WindowMode { get; set; } = "Normal";
    public string DefaultExportDirectory { get; set; } = "C:\\";
    
    public bool AutoBackupEnabled { get; set; } = false;
    public string AutoBackupDirectory { get; set; } = "C:\\ArchiveBackups\\";


    public List<SectorItem> Sectors { get; set; } = new List<SectorItem>
    {
        new SectorItem { Name = "පාලන", ColorHex = "#d99694" },
        new SectorItem { Name = "සෞඛ්‍ය", ColorHex = "#b3a2c7" },
        new SectorItem { Name = "සංවර්ධන", ColorHex = "#e46c0a" },
        new SectorItem { Name = "ආදායම්", ColorHex = "#ffff00" },
        new SectorItem { Name = "ගිණුම්", ColorHex = "#95b3d7" },
        new SectorItem { Name = "සං‍රක්ෂණ", ColorHex = "#FFFFFF" }
    };

    public List<string> FileTypes { get; set; } = new List<string>();

}