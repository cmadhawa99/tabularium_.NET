using System;
using System.Security.Cryptography;

namespace ArchivumWpf.Services;

public static class PasswordHasher
{
    public static string Hash(string password, string pepper)
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        
        string passwordWithPepper = password + pepper;
        
        var hash = Rfc2898DeriveBytes.Pbkdf2(passwordWithPepper, salt, 1_200_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash, string pepper)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        
        string passwordWithPepper = password + pepper;
        
        var actual = Rfc2898DeriveBytes.Pbkdf2(passwordWithPepper, salt, 1_200_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}