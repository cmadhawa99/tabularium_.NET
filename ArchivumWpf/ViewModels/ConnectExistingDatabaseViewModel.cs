using System.IO;
using System.Text.Json.Nodes;
using System.Windows;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Npgsql;

namespace ArchivumWpf.ViewModels;

public partial class ConnectExistingDatabaseViewModel : ObservableObject
{
    private readonly IConnectionsRegistryService _registryService;
    private readonly SchemaValidationService _schemaValidationService = new();

    [ObservableProperty] private int _currentStep = 1; 
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isProcessing;
    
    [ObservableProperty] private string _encryptionKeyInput = string.Empty;
    
    [ObservableProperty] private string _dbHost = string.Empty;
    [ObservableProperty] private string _dbName = string.Empty;
    [ObservableProperty] private string _dbUser = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    private string _validatedConnectionString = string.Empty;
    
    [ObservableProperty] private bool _isCreatingNewStorage;
    
    [ObservableProperty] private string _storageDirectory = string.Empty;

    [ObservableProperty] private string _newStorageParentDirectory = string.Empty;
    [ObservableProperty] private string _newStorageFolderName = ".Secure";

    public ConnectExistingDatabaseViewModel(IConnectionsRegistryService registryService)
    {
        _registryService = registryService;
    }

    [RelayCommand]
    private void NextFromKeyStep()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EncryptionKeyInput))
        {
            ErrorMessage = "Please paste the AES-256-GCM key this database was originally created with.";
            return;
        }

        try
        {
            var bytes = Convert.FromBase64String(EncryptionKeyInput.Trim());
            if (bytes.Length != 32)
            {
                ErrorMessage = "The key must decode to exactly 32 bytes (AES-256).";
                return;
            }
        }
        catch (FormatException)
        {
            ErrorMessage = "Invalid Base64 key format.";
            return;
        }

        CurrentStep = 2;
    }

    [RelayCommand]
    private async Task ValidateAndNextFromDbStepAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DbHost) || string.IsNullOrWhiteSpace(DbName) ||
            string.IsNullOrWhiteSpace(DbUser) || string.IsNullOrWhiteSpace(DbPassword))
        {
            ErrorMessage = "Please fill in all database connection fields.";
            return;
        }

        IsProcessing = true;
        try
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = DbHost,
                Database = DbName,
                Username = DbUser,
                Password = DbPassword,
                Timeout = 5
            };
            var connString = builder.ToString();

            var schemaResult = await _schemaValidationService.ValidateAsync(connString);
            if (!schemaResult.IsValid)
            {
                ErrorMessage = schemaResult.Message;
                return;
            }

            await using var connection = new NpgsqlConnection(connString);
            await connection.OpenAsync();

            await using var canaryCmd = new NpgsqlCommand(
                "SELECT \"EncryptedCanary\" FROM \"AppSecurityMetas\" LIMIT 1;", connection);
            var encryptedCanary = await canaryCmd.ExecuteScalarAsync() as string;

            if (string.IsNullOrEmpty(encryptedCanary))
            {
                ErrorMessage = "This database has no security canary to verify against, so it cannot be safely attached.";
                return;
            }

            try
            {
                var crypto = new CryptoService(EncryptionKeyInput.Trim());
                crypto.Decrypt(encryptedCanary);
            }
            catch
            {
                ErrorMessage = "The schema matches, but the encryption key provided does not decrypt this " +
                                "database's data. Please double-check the key.";
                return;
            }

            _validatedConnectionString = connString;
            CurrentStep = 3;
        }
        catch (PostgresException ex)
        {
            ErrorMessage = $"Database connection failed: {ex.MessageText}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void BrowseStorageDirectory()
    {
        var dialog = new OpenFolderDialog { Title = "Select This Database's Existing Secure File Storage Folder" };
        if (dialog.ShowDialog() == true) StorageDirectory = dialog.FolderName;
    }

    [RelayCommand]
    private void BrowseNewStorageParentDirectory()
    {
        var dialog = new OpenFolderDialog { Title = "Select Where to Create a New Secure File Storage Folder" };
        if (dialog.ShowDialog() == true) NewStorageParentDirectory = dialog.FolderName;
    }

    [RelayCommand]
    private void NextFromStorageStep()
    {
        ErrorMessage = string.Empty;

        if (!IsCreatingNewStorage)
        {
            if (string.IsNullOrWhiteSpace(StorageDirectory) || !Directory.Exists(StorageDirectory))
            {
                ErrorMessage = "Please choose the existing folder where this database's encrypted documents are stored.";
                return;
            }

            CurrentStep = 4;
            return;
        }

        // Creating a brand new folder because the original is lost.
        if (string.IsNullOrWhiteSpace(NewStorageParentDirectory) || !Directory.Exists(NewStorageParentDirectory))
        {
            ErrorMessage = "Please choose a valid location for the new secure storage folder.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewStorageFolderName))
            NewStorageFolderName = ".Secure";

        if (!SecureStorageHelper.NameIsAvailable(NewStorageParentDirectory, NewStorageFolderName))
        {
            ErrorMessage = $"An item named '{NewStorageFolderName}' already exists there. Choose a different name.";
            return;
        }

        var confirm = MessageBox.Show(
            "This database already has file records that point to an original storage folder. " +
            "Creating a new empty folder means any previously attached documents will no longer be " +
            "reachable and will show as 'File Not Found' in Document Management.\n\n" +
            "All other data — file records, RR numbers, circulation history, disposal history — is " +
            "unaffected and will continue to work normally. New documents you attach going forward " +
            "will be stored in the new folder.\n\n" +
            "Do you want to proceed with a new empty storage folder?",
            "Original Storage Folder Not Found",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        CurrentStep = 4;
    }

    [RelayCommand]
    private async Task FinishSetupAsync(Window window)
    {
        ErrorMessage = string.Empty;
        IsProcessing = true;

        var profileId = Guid.NewGuid();
        var profileFolder = AppPaths.ProfileFolder(profileId);

        try
        {
            Directory.CreateDirectory(profileFolder);

            KeyVaultService.ImportKey(profileFolder, EncryptionKeyInput.Trim());

            string finalStoragePath;
            if (IsCreatingNewStorage)
            {
                var storageResult = SecureStorageHelper.Create(NewStorageParentDirectory, NewStorageFolderName);
                if (!storageResult.Success)
                {
                    ErrorMessage = storageResult.Message;
                    return;
                }
                finalStoragePath = storageResult.FullPath;
            }
            else
            {
                finalStoragePath = StorageDirectory;
            }

            var cryptoService = new CryptoService(EncryptionKeyInput.Trim());
            var appSettingsNode = new JsonObject
            {
                ["ConnectionStrings"] = new JsonObject
                {
                    ["DefaultConnection"] = cryptoService.Encrypt(_validatedConnectionString)
                },
                ["SecureStorage"] = new JsonObject
                {
                    ["Path"] = finalStoragePath
                }
            };

            File.WriteAllText(Path.Combine(profileFolder, "appsettings.json"),
                appSettingsNode.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            var profile = new ConnectionProfile
            {
                Id = profileId,
                DisplayName = $"{DbName} @ {DbHost}",
                DbHost = DbHost,
                DbName = DbName
            };
            _registryService.Add(profile);
            _registryService.SetActive(profileId);
            SessionContext.ActiveProfile = profile;

            var successMessage = IsCreatingNewStorage
                ? "Existing database attached successfully with a new empty storage folder. " +
                  "Previously stored documents will appear as missing. You can now log in."
                : "Existing database attached successfully! You can now log in with an account from that database.";

            MessageBox.Show(successMessage, "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            window.DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {
            SessionContext.ActiveProfile = null;
            try { if (Directory.Exists(profileFolder)) Directory.Delete(profileFolder, true); } catch { /* best effort */ }

            ErrorMessage = $"Setup failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 1) CurrentStep--;
    }
}