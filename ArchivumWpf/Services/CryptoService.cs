using System;
using System.Security.Cryptography;
using System.Text;

namespace ArchivumWpf.Services
{
    public class CryptoService
    {
        private readonly byte[] _key;
        
        public CryptoService(string base64Key)
        {
            _key = Convert.FromBase64String(base64Key);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);
            
            byte[] ciphertext = new byte[plainBytes.Length];
            byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];

            using (var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize))
            {
                aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);
            }
            
            byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
            
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherTextBase64)
        {
            if (string.IsNullOrEmpty(cipherTextBase64)) return cipherTextBase64;
            
            byte[] fullCipher = Convert.FromBase64String(cipherTextBase64);
            
            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;
            
            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[tagSize];
            byte[] ciphertext = new byte[fullCipher.Length - nonceSize - tagSize];
            
            Buffer.BlockCopy(fullCipher, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(fullCipher, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(fullCipher, nonceSize + tagSize, ciphertext, 0, ciphertext.Length);

            byte[] plainBytes = new byte[ciphertext.Length];

            using (var aesGcm = new AesGcm(_key, tagSize))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);

        }

        public string GetBlindIndex(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            
            using (var hmac = new HMACSHA256(_key))
            {
                byte[] hasgBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes (plainText.ToLowerInvariant()));
                return Convert.ToBase64String(hasgBytes);
            }
        }
    }
}