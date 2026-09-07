using System.IO;
using System.Text.Json;
using Archivum.Models;

namespace Archivum.Services;

public interface IConnectionsRegistryService
{
    List<ConnectionProfile> GetAll();
    ConnectionProfile Add(ConnectionProfile profile);
    void Remove(Guid id, bool deleteFiles);
    ConnectionProfile? GetActive();
    void SetActive(Guid id);
}

public class ConnectionsRegistryService : IConnectionsRegistryService
{
    private class RegistryData
    {
        public List<ConnectionProfile> Profiles { get; set; } = new();
        public Guid? ActiveProfileId { get; set; }
    }

    private RegistryData Load()
    {
        AppPaths.EnsureRootExists();
        if (!File.Exists(AppPaths.ConnectionsRegistryFile)) return new RegistryData();

        var json = File.ReadAllText(AppPaths.ConnectionsRegistryFile);
        return JsonSerializer.Deserialize<RegistryData>(json) ?? new RegistryData();
    }

    private void Save(RegistryData data)
    {
        AppPaths.EnsureRootExists();
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(AppPaths.ConnectionsRegistryFile, json);
    }

    public List<ConnectionProfile> GetAll() => Load().Profiles;

    public ConnectionProfile Add(ConnectionProfile profile)
    {
        var data = Load();
        data.Profiles.Add(profile);
        Save(data);
        return profile;
    }

    public void Remove(Guid id, bool deleteFiles)
    {
        var data = Load();
        data.Profiles.RemoveAll(p => p.Id == id);
        if (data.ActiveProfileId == id) data.ActiveProfileId = null;
        Save(data);

        if (deleteFiles)
        {
            var folder = AppPaths.ProfileFolder(id);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    public ConnectionProfile? GetActive()
    {
        var data = Load();
        return data.Profiles.FirstOrDefault(p => p.Id == data.ActiveProfileId);
    }

    public void SetActive(Guid id)
    {
        var data = Load();
        if (data.Profiles.Any(p => p.Id == id))
        {
            data.ActiveProfileId = id;
            Save(data);
        }
    }
}