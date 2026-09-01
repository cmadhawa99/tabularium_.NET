using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ArchivumWpf.Services;

public static class KeyVaultService
{
    private const string KeyFileName = "avc.dat";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("මසෂවසඕඡඈෂඎඇටඈලඵඟළවලථළෆඍකඒඔළණ");

    public static bool VaultExists()
    {
        var KeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KeyFileName);
        return File.Exists(KeyPath);
    }

    public static string GetMasterKey()
    {
        var KeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KeyFileName);

        if (File.Exists(KeyPath))
        {
            var encryptedKey = File.ReadAllBytes(KeyPath);
            var decryptedKey =
                ProtectedData.Unprotect(encryptedKey, Entropy,
                    DataProtectionScope.CurrentUser); //CurrentUSer or LocalMachine

            return Convert.ToBase64String(decryptedKey);
        }

        throw new FileNotFoundException("Master key vault not found. The system must be initialized or restored.");
    }

    public static string GenerateNewKey()
    {
        var KeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KeyFileName);

        var freshKey = new byte[32];
        RandomNumberGenerator.Fill(freshKey);

        var newEncryptedKey = ProtectedData.Protect(freshKey, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(KeyPath, newEncryptedKey);

        return Convert.ToBase64String(freshKey);
    }

    public static void ImportKey(string base64RecoveryKey)
    {
        var KeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KeyFileName);

        try
        {
            var KeyBytes = Convert.FromBase64String(base64RecoveryKey);

            var newEncryptedKey = ProtectedData.Protect(KeyBytes, Entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(KeyPath, newEncryptedKey);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid recovery key format.");
        }
    }
}