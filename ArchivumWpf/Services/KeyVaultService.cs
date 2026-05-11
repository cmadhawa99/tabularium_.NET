//REMOVE THE KEY WHEN DEPLOY!

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ArchivumWpf.Services;

public static class KeyVaultService
{
    private const string KeyFileName = "arch_vault.dat";

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("මසෂවසඕඡඈෂඎඇටඈලඵඟළවලථළෆඍකඒඔළණ");

    public static string GetMasterKey()
    {
        string KeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KeyFileName);

        if (File.Exists(KeyPath))
        {
            byte[] encryptedKey = File.ReadAllBytes(KeyPath);

            byte[] decryptedKey = ProtectedData.Unprotect(encryptedKey, Entropy, DataProtectionScope.LocalMachine);
            
            return Convert.ToBase64String(decryptedKey);
        }
        
        string currentKey = "W5bZnVXXs+eq9GLHdLTU6btIYmpHEQ9NLfxZjWAb4mI=";
        byte[] keyBytes = Convert.FromBase64String(currentKey);
        
        byte[] newEncryptedKey = ProtectedData.Protect(keyBytes, Entropy, DataProtectionScope.LocalMachine);
        
        File.WriteAllBytes(KeyPath, newEncryptedKey);
        
        return currentKey;

    }
    
}