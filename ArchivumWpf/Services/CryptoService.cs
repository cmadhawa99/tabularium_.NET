using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ArchivumWpf.Services
{
    public class CryptoService
    {
        private readonly byte[] _key;
        private const int ChunkSize = 4 * 1024 * 1024; //4MB Chunks
        
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
        
        // Encrypts a stream using AES-256-GCM chunking and Envelope Encryption

        public (string EncryptedDek, long TotalFileSize) EncryptFileStream(Stream inputStream, Stream outputStream)
        {
            byte[] dek = new byte[32];
            RandomNumberGenerator.Fill(dek);
            
            string dekBase64 = Convert.ToBase64String(dek);
            string encryptedDek = Encrypt(dekBase64);
            
            byte[] buffer = new byte[ChunkSize];
            int bytesRead;
            long totalSize = 0;

            using (var aesGcm = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize))
            {
                while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                    RandomNumberGenerator.Fill(nonce);
                    
                    byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
                    byte[] cipherText = new byte[bytesRead];
                    
                    ReadOnlySpan<byte> plainTextSpan = new ReadOnlySpan<byte>(buffer, 0, bytesRead);

                    aesGcm.Encrypt(nonce, plainTextSpan, cipherText, tag);
                    
                    outputStream.Write(nonce, 0, nonce.Length);
                    outputStream.Write(tag, 0, tag.Length);

                    byte[] lengthBytes = BitConverter.GetBytes(bytesRead);
                    outputStream.Write(lengthBytes, 0, lengthBytes.Length);
                    
                    outputStream.Write(cipherText, 0, cipherText.Length);
                    
                    totalSize += nonce.Length + tag.Length + lengthBytes.Length + cipherText.Length;
                }
            }
            
            return (encryptedDek, totalSize);
        }
        
        // Decrypts a chunked AES-256-GCM stream back to plaintext on the fly

        public void DecryptFileStream(Stream inputStream, Stream outputStream, string encryptedDek)
        {
            string dekBase64 = Decrypt(encryptedDek);
            byte[] dek = Convert.FromBase64String(dekBase64);
            
            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;
            
            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[tagSize];
            byte[] lengthBytes = new byte[4];

            using (var aesGcm = new AesGcm(dek, tagSize))
            {
                while (inputStream.Position < inputStream.Length)
                {
                    int readNonce = inputStream.Read(nonce, 0, nonceSize);
                    if (readNonce == 0) break;
                    
                    inputStream.Read(tag, 0, tagSize);
                    inputStream.Read(lengthBytes, 0, lengthBytes.Length);
                    
                    int cipherLength = BitConverter.ToInt32(lengthBytes, 0);
                    byte[] cipherText = new byte[cipherLength];

                    int totalRead = 0;
                    while (totalRead < cipherLength)
                    {
                        int read = inputStream.Read(cipherText, totalRead, cipherLength - totalRead);
                        if (read == 0) throw new EndOfStreamException("Corrupted file: Unexpected end of stream.");
                        totalRead += read;
                    }
                    
                    byte[] plainText = new byte[cipherLength];
                    
                    aesGcm.Decrypt(nonce, cipherText, tag, plainText);
                    outputStream.Write(plainText, 0, plainText.Length);
                    
                }
            }
        }
        
    }
}