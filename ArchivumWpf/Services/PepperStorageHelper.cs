using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
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
    
    public static string GetOrCreatePepper(CryptoService cryptoService)
    {
        if (SessionContext.ActiveProfile == null)
            throw new InvalidOperationException("No active profile selected.");

        var profileFolder = AppPaths.ProfileFolder(SessionContext.ActiveProfile.Id);
        var settingsPath = Path.Combine(profileFolder, "appsettings.json");

        JsonObject root;
        if (File.Exists(settingsPath))
        {
            root = JsonNode.Parse(File.ReadAllText(settingsPath)) as JsonObject ?? new JsonObject();
        }
        else
        {
            Directory.CreateDirectory(profileFolder);
            root = new JsonObject();
        }

        var existingEncrypted = root["SecuritySettings"]?["EncryptedPepper"]?.GetValue<string>();

        if (!string.IsNullOrEmpty(existingEncrypted))
        {
            return cryptoService.Decrypt(existingEncrypted);
        }

        var pepperBytes = new byte[32];
        RandomNumberGenerator.Fill(pepperBytes);
        var plainPepper = Convert.ToBase64String(pepperBytes);
        var newEncryptedPepper = cryptoService.Encrypt(plainPepper);

        if (root["SecuritySettings"] is not JsonObject securityNode)
        {
            securityNode = new JsonObject();
            root["SecuritySettings"] = securityNode;
        }

        securityNode["EncryptedPepper"] = newEncryptedPepper;

        File.WriteAllText(settingsPath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return plainPepper;
    }
}