using System.IO;

namespace Archivum.Services;

public static class AppPaths
{
    public static string RootFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Archivum");

    public static string ProfilesFolder => Path.Combine(RootFolder, "Profiles");
    public static string ConnectionsRegistryFile => Path.Combine(RootFolder, "connections.json");

    public static string ProfileFolder(Guid profileId) => Path.Combine(ProfilesFolder, profileId.ToString());

    public static void EnsureRootExists()
    {
        Directory.CreateDirectory(RootFolder);
        Directory.CreateDirectory(ProfilesFolder);
    }
}