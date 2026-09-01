using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ArchivumWpf.Services;

public class CryptoService
{
    private const int ChunkSize = 4 * 1024 * 1024; //4MB Chunks
    private readonly byte[] _key;

    public CryptoService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using (var aesGcm = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize))
        {
            aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);
        }

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherTextBase64)
    {
        if (string.IsNullOrEmpty(cipherTextBase64)) return cipherTextBase64;

        var fullCipher = Convert.FromBase64String(cipherTextBase64);

        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = new byte[nonceSize];
        var tag = new byte[tagSize];
        var ciphertext = new byte[fullCipher.Length - nonceSize - tagSize];

        Buffer.BlockCopy(fullCipher, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(fullCipher, nonceSize, tag, 0, tagSize);
        Buffer.BlockCopy(fullCipher, nonceSize + tagSize, ciphertext, 0, ciphertext.Length);

        var plainBytes = new byte[ciphertext.Length];

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
            var hasgBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText.ToLowerInvariant()));
            return Convert.ToBase64String(hasgBytes);
        }
    }

    // Encrypts a stream using AES-256-GCM chunking and Envelope Encryption

    public (string EncryptedDek, long TotalFileSize) EncryptFileStream(Stream inputStream, Stream outputStream)
    {
        var dek = new byte[32];
        RandomNumberGenerator.Fill(dek);

        var dekBase64 = Convert.ToBase64String(dek);
        var encryptedDek = Encrypt(dekBase64);

        var buffer = new byte[ChunkSize];
        int bytesRead;
        long totalSize = 0;

        using (var aesGcm = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize))
        {
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                RandomNumberGenerator.Fill(nonce);

                var tag = new byte[AesGcm.TagByteSizes.MaxSize];
                var cipherText = new byte[bytesRead];

                var plainTextSpan = new ReadOnlySpan<byte>(buffer, 0, bytesRead);

                aesGcm.Encrypt(nonce, plainTextSpan, cipherText, tag);

                outputStream.Write(nonce, 0, nonce.Length);
                outputStream.Write(tag, 0, tag.Length);

                var lengthBytes = BitConverter.GetBytes(bytesRead);
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
        var dekBase64 = Decrypt(encryptedDek);
        var dek = Convert.FromBase64String(dekBase64);

        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = new byte[nonceSize];
        var tag = new byte[tagSize];
        var lengthBytes = new byte[4];

        using (var aesGcm = new AesGcm(dek, tagSize))
        {
            while (inputStream.Position < inputStream.Length)
            {
                var readNonce = inputStream.Read(nonce, 0, nonceSize);
                if (readNonce == 0) break;

                inputStream.Read(tag, 0, tagSize);
                inputStream.Read(lengthBytes, 0, lengthBytes.Length);

                var cipherLength = BitConverter.ToInt32(lengthBytes, 0);
                var cipherText = new byte[cipherLength];

                var totalRead = 0;
                while (totalRead < cipherLength)
                {
                    var read = inputStream.Read(cipherText, totalRead, cipherLength - totalRead);
                    if (read == 0) throw new EndOfStreamException("Corrupted file: Unexpected end of stream.");
                    totalRead += read;
                }

                var plainText = new byte[cipherLength];

                aesGcm.Decrypt(nonce, cipherText, tag, plainText);
                outputStream.Write(plainText, 0, plainText.Length);
            }
        }
    }
}