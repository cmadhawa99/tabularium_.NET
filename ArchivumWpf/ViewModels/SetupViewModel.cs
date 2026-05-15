using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Services;
using Npgsql;

namespace ArchivumWpf.ViewModels;

public partial class SetupViewModel : ObservableObject
{
    
    [ObservableProperty] private string _dbHost = string.Empty;
    [ObservableProperty] private string _dbName = string.Empty;
    [ObservableProperty] private string _dbUser = string.Empty;
    [ObservableProperty] private string _dbPassword = string.Empty;
    
    [ObservableProperty] private string _recoveryKeyInput = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isProcessing = false;

    [RelayCommand]
    private async Task RestoreSystemAsync(Window window)
    {
        if (string.IsNullOrWhiteSpace(RecoveryKeyInput) || string.IsNullOrWhiteSpace(DbHost) ||
            string.IsNullOrWhiteSpace(DbName) || string.IsNullOrWhiteSpace(DbUser) ||
            string.IsNullOrWhiteSpace(DbPassword))
        {
            ErrorMessage = "Please fill in all database fields and paste your recovery key.";
            return;
        }

        string pastedKey = RecoveryKeyInput.Trim();

        try
        {
            byte[] keyBytes = Convert.FromBase64String(pastedKey);
            if (keyBytes.Length != 32)
            {
                ErrorMessage = "Invalid key length. The master key must be exactly 32 bytes.";
                return;
            }
        }
        catch
        {
            ErrorMessage = "Invalid format. Please ensure you pasted the exact Base64 string.";
            return;
        }

        IsProcessing = true;
        ErrorMessage = string.Empty;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = _dbHost,
            Database = _dbName,
            Username = _dbUser,
            Password = _dbPassword,
            Timeout = 5

        };

        try
        {
            await using var connection = new NpgsqlConnection(builder.ToString());
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("SELECT \"EncryptedCanary\" FROM \"AppSecurityMetas\" LIMIT 1;",
                connection);
            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                string encryptedCanary = result.ToString() ?? "";

                try
                {
                    var cryptoService = new CryptoService(pastedKey);
                    string testDecryption = cryptoService.Decrypt(encryptedCanary);
                }
                catch (CryptographicException)
                {
                    ErrorMessage = "Access Denied. The provided Recovery Key cannot decrypt this database.";
                    IsProcessing = false;
                    return;
                }
            }
            else
            {
                ErrorMessage = "No security artifact found in the database. Are you sure you restored your PostgreSQL data backup? (An empty database cannot be 'restored'. If this is a fresh installation, please use 'Initialize New Archive' instead).";
                IsProcessing = false;
                return;
            }

            KeyVaultService.ImportKey(pastedKey);

            string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var jsonNode = JsonNode.Parse(File.ReadAllText(appSettingsPath));
                if (jsonNode?["ConnectionStrings"] != null)
                {
                    var cryptoService = new CryptoService(pastedKey);
                    jsonNode["ConnectionStrings"]!["DefaultConnection"] = cryptoService.Encrypt(builder.ToString());

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(appSettingsPath, jsonNode.ToJsonString(options));

                }
            }

            MessageBox.Show("Database connected and Security Vault restored successfully!", "System Restored",
                MessageBoxButton.OK, MessageBoxImage.Information);

            window.DialogResult = true;
            window.Close();
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
}