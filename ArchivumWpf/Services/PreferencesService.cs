using System;
using System.IO;
using System.Text.Json;
using ArchivumWpf.Models;
using Microsoft.Extensions.Configuration;

namespace ArchivumWpf.Services;

public interface IPreferencesService
{
    UserPreferences GetPreferences();
    void SavePreferences(UserPreferences prefs);
}

public class PreferencesService : IPreferencesService
{
    private readonly string _filePath;

    public PreferencesService()
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preferences.json");
    }

    public UserPreferences GetPreferences()
    {
        if (!File.Exists(_filePath))
        {
            var defaultPrefs = new UserPreferences();
            SavePreferences(defaultPrefs);
            return defaultPrefs;
        }
        
        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
    }

    public void SavePreferences(UserPreferences prefs)
    {
        string json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}