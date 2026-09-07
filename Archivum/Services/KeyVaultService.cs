using System.IO;
using System.Security.Cryptography;

namespace Archivum.Services;

public static class KeyVaultService
{
    private const string KeyFileName = "avc.dat";
    private const string EntropyFileName = "entropy.dat";

    private static byte[] GetOrCreateEntropy(string profileFolder)
    {
        Directory.CreateDirectory(profileFolder);
        var entropyPath = Path.Combine(profileFolder, EntropyFileName);

        if (File.Exists(entropyPath))
            return File.ReadAllBytes(entropyPath);

        var entropy = new byte[32];
        RandomNumberGenerator.Fill(entropy);
        File.WriteAllBytes(entropyPath, entropy);
        return entropy;
    }

    public static bool VaultExists(string profileFolder)
        => File.Exists(Path.Combine(profileFolder, KeyFileName));

    public static string GetMasterKey(string profileFolder)
    {
        var keyPath = Path.Combine(profileFolder, KeyFileName);
        if (!File.Exists(keyPath))
            throw new FileNotFoundException("Master key vault not found for this connection.");

        var entropy = GetOrCreateEntropy(profileFolder);
        var encryptedKey = File.ReadAllBytes(keyPath);
        var decryptedKey = ProtectedData.Unprotect(encryptedKey, entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(decryptedKey);
    }
    
    public static void ImportKey(string profileFolder, string base64Key)
    {
        Directory.CreateDirectory(profileFolder);

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(base64Key);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid key format. The key must be a Base64 string.");
        }

        if (keyBytes.Length != 32)
            throw new ArgumentException("Invalid key length. The key must decode to exactly 32 bytes (AES-256).");

        var entropy = GetOrCreateEntropy(profileFolder);
        var protectedKey = ProtectedData.Protect(keyBytes, entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(Path.Combine(profileFolder, KeyFileName), protectedKey);
    }
    
    public static string GenerateRandomKeyBase64()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}