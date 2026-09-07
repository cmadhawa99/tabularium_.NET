using System.IO;
using System.Text.Json;
using ArchivumWpf.Models;

namespace ArchivumWpf.Services;

public interface IPreferencesService
{
    UserPreferences GetPreferences();
    void SavePreferences(UserPreferences prefs);
}

public class PreferencesService : IPreferencesService
{
    private string FilePath => Path.Combine(SessionContext.ProfileFolder, "userpreferences.json");

    public UserPreferences GetPreferences()
    {
        if (!File.Exists(FilePath))
        {
            var defaultPrefs = new UserPreferences();
            SavePreferences(defaultPrefs);
            return defaultPrefs;
        }

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
    }

    public void SavePreferences(UserPreferences prefs)
    {
        Directory.CreateDirectory(SessionContext.ProfileFolder);
        var json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}