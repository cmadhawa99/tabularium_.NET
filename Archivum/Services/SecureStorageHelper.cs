using System.IO;

namespace Archivum.Services;

public static class SecureStorageHelper
{
    public class Result
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string FullPath { get; init; } = string.Empty;
    }
    
    public static bool NameIsAvailable(string parentDirectory, string folderName)
    {
        var fullPath = Path.Combine(parentDirectory, folderName);
        return !Directory.Exists(fullPath) && !File.Exists(fullPath);
    }
    
    public static Result Create(string parentDirectory, string folderName)
    {
        var fullPath = Path.Combine(parentDirectory, folderName);

        if (Directory.Exists(fullPath) || File.Exists(fullPath))
            return new Result
            {
                Success = false,
                FullPath = fullPath,
                Message = $"An item named '{folderName}' already exists at this location. Choose a different name or location."
            };

        try
        {
            var di = Directory.CreateDirectory(fullPath);
            di.Attributes |= FileAttributes.Hidden;
            return new Result { Success = true, FullPath = fullPath, Message = "Secure storage created." };
        }
        catch (Exception ex)
        {
            return new Result { Success = false, FullPath = fullPath, Message = $"Failed to create storage folder: {ex.Message}" };
        }
    }
}