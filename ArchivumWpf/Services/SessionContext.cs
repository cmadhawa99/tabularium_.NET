using System.IO;
using ArchivumWpf.Models;

namespace ArchivumWpf.Services;

// Holds the currently active connection profile for this app session.

public static class SessionContext
{
    public static Models.ConnectionProfile? ActiveProfile { get; set; }

    public static string ProfileFolder =>
        ActiveProfile != null ? AppPaths.ProfileFolder(ActiveProfile.Id) : DesignTimeFallbackFolder;

    private static string DesignTimeFallbackFolder
    {
        get
        {
            var folder = Path.Combine(Path.GetTempPath(), "ArchivumWpf_DesignTime");
            Directory.CreateDirectory(folder);
            if (!KeyVaultService.VaultExists(folder))
                KeyVaultService.ImportKey(folder, KeyVaultService.GenerateRandomKeyBase64());
            return folder;
        }
    }
}