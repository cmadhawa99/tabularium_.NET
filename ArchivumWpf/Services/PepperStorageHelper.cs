using System;
using System.IO;
using System.Text.Json.Nodes;

namespace ArchivumWpf.Services;

public static class PepperStorageHelper
{
    public static string GetPepper(CryptoService cryptoService)
    {
        if (SessionContext.ActiveProfile == null)
            throw new InvalidOperationException("No active profile selected.");

        var profileFolder = AppPaths.ProfileFolder(SessionContext.ActiveProfile.Id);
        var settingsPath = Path.Combine(profileFolder, "appsettings.json");

        if (!File.Exists(settingsPath))
            throw new FileNotFoundException("appsettings.json is missing.");

        var jsonNode = JsonNode.Parse(File.ReadAllText(settingsPath));
        var encryptedPepper = jsonNode?["SecuritySettings"]?["EncryptedPepper"]?.GetValue<string>();

        if (string.IsNullOrEmpty(encryptedPepper))
            throw new InvalidOperationException("Pepper is missing in appsettings.");

        return cryptoService.Decrypt(encryptedPepper);
    }
}