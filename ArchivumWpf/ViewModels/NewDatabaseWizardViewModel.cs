using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Npgsql;

namespace ArchivumWpf.ViewModels;

public partial class NewDatabaseWizardViewModel : ObservableObject
{
    private readonly IConnectionsRegistryService _registryService;

    [ObservableProperty] private int _currentStep = 1; 
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isProcessing;

    [ObservableProperty] private string _encryptionKeyInput = string.Empty;

    [ObservableProperty] private string _dbHost = string.Empty;
    [ObservableProperty] private string _dbName = string.Empty;
    [ObservableProperty] private string _dbUser = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    
    [ObservableProperty] private string _storageParentDirectory = string.Empty;
    [ObservableProperty] private string _storageFolderName = ".Secure";

    [ObservableProperty] private string _adminUsername = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminPasswordConfirm { get; set; } = string.Empty;

    private Guid _pendingProfileId;

    public NewDatabaseWizardViewModel(IConnectionsRegistryService registryService)
    {
        _registryService = registryService;
    }

    [RelayCommand]
    private void GenerateKey()
    {
        EncryptionKeyInput = KeyVaultService.GenerateRandomKeyBase64();
    }

    [RelayCommand]
    private void NextFromKeyStep()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EncryptionKeyInput))
        {
            ErrorMessage = "Please generate or paste an AES-256-GCM key.";
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
    private async Task NextFromDbStepAsync()
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

            await using var connection = new NpgsqlConnection(builder.ToString());
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';", connection);
            var tableCount = Convert.ToInt64(await command.ExecuteScalarAsync());

            if (tableCount > 0)
            {
                ErrorMessage = $"This database already contains {tableCount} table(s). " +
                                "Please provide the details of a completely empty database.";
                return;
            }

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
        var dialog = new OpenFolderDialog { Title = "Select Secure File Storage Location" };
        if (dialog.ShowDialog() == true) StorageParentDirectory = dialog.FolderName;
    }

    [RelayCommand]
    private void NextFromStorageStep()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(StorageParentDirectory) || !Directory.Exists(StorageParentDirectory))
        {
            ErrorMessage = "Please choose a valid location for secure file storage.";
            return;
        }

        if (string.IsNullOrWhiteSpace(StorageFolderName))
            StorageFolderName = ".Secure";

        if (!SecureStorageHelper.NameIsAvailable(StorageParentDirectory, StorageFolderName))
        {
            ErrorMessage = $"An item named '{StorageFolderName}' already exists there. Choose a different name.";
            return;
        }

        CurrentStep = 4;
    }

    [RelayCommand]
    private void NextFromAdminStep()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(AdminUsername) || string.IsNullOrWhiteSpace(AdminPassword))
        {
            ErrorMessage = "Please provide an admin username and password.";
            return;
        }

        if (AdminPassword != AdminPasswordConfirm)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        CurrentStep = 5;
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

            var storageResult = SecureStorageHelper.Create(StorageParentDirectory, StorageFolderName);
            if (!storageResult.Success)
            {
                ErrorMessage = storageResult.Message;
                return;
            }

            var connBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = DbHost,
                Database = DbName,
                Username = DbUser,
                Password = DbPassword
            };

            var cryptoService = new CryptoService(EncryptionKeyInput.Trim());
            var appSettingsNode = new JsonObject
            {
                ["ConnectionStrings"] = new JsonObject
                {
                    ["DefaultConnection"] = cryptoService.Encrypt(connBuilder.ToString())
                },
                ["SecureStorage"] = new JsonObject
                {
                    ["Path"] = storageResult.FullPath
                }
            };

            File.WriteAllText(Path.Combine(profileFolder, "appsettings.json"),
                appSettingsNode.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            _pendingProfileId = profileId;
            SessionContext.ActiveProfile = new ConnectionProfile { Id = profileId };

            await using (var context = new Services.AppDbContext())
            {
                await context.Database.MigrateAsync();

                var canaryBytes = new byte[32];
                RandomNumberGenerator.Fill(canaryBytes);
                var plainCanary = Convert.ToBase64String(canaryBytes);
                context.AppSecurityMetas.Add(new Models.AppSecurityMeta
                {
                    EncryptedCanary = cryptoService.Encrypt(plainCanary)
                });

                context.Users.Add(new Models.User
                {
                    Role = "Admin",
                    IsActive = true,
                    Username = AdminUsername, 
                    PasswordHash = PasswordHasher.Hash(AdminPassword)
                });

                await context.SaveChangesAsync();
            }

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

            MessageBox.Show(
                "Database connected and initialized successfully! You can now log in with the admin account you just created.",
                "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            window.DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {

            SessionContext.ActiveProfile = null;
            try { if (Directory.Exists(profileFolder)) Directory.Delete(profileFolder, true); } catch { /**/ }

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