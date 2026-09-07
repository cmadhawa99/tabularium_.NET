using System.IO;

namespace ArchivumWpf.Services;

public static class AppPaths
{
    public static string RootFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArchivumWpf");

    public static string ProfilesFolder => Path.Combine(RootFolder, "Profiles");
    public static string ConnectionsRegistryFile => Path.Combine(RootFolder, "connections.json");

    public static string ProfileFolder(Guid profileId) => Path.Combine(ProfilesFolder, profileId.ToString());

    public static void EnsureRootExists()
    {
        Directory.CreateDirectory(RootFolder);
        Directory.CreateDirectory(ProfilesFolder);
    }
}