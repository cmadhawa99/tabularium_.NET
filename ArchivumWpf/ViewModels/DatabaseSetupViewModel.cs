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

public partial class DatabaseSetupViewModel : ObservableObject
{
    [ObservableProperty] private string _dbHost =  string.Empty;
    [ObservableProperty] private string _dbName =  string.Empty;
    [ObservableProperty] private string _dbUser =  string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isProcessing = false;

    [RelayCommand]
    private async Task ConnectAndSaveAsync(Window window)
    {
        if (string.IsNullOrWhiteSpace(DbHost) || string.IsNullOrWhiteSpace(DbName) ||
            string.IsNullOrWhiteSpace(DbUser) || string.IsNullOrWhiteSpace(DbPassword))
        {
            ErrorMessage = "Please fill in all database fields.";
            return;
        }
        
        IsProcessing = true;
        ErrorMessage = string.Empty;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = DbHost,
            Database = DbName,
            Username = DbUser,
            Password = DbPassword,
            Timeout = 5
        };

        try
        {
            string masterKey = KeyVaultService.GetMasterKey();

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
                    var cryptoService = new CryptoService(masterKey);
                    cryptoService.Decrypt(encryptedCanary);
                }

                catch (CryptographicException)
                {
                    ErrorMessage =
                        "Connection successful, but your local Master Key cannot decrypt this database. You may be connecting to the wrong server.";
                    IsProcessing = false;
                    return;
                }
            }

            string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            var jsonNode = File.Exists(appSettingsPath)
                ? JsonNode.Parse(File.ReadAllText(appSettingsPath))
                : new JsonObject { ["ConnectionStrings"] = new JsonObject() };

            var cryptoWriter = new CryptoService(masterKey);
            jsonNode!["ConnectionStrings"]!["DefaultConnection"] = cryptoWriter.Encrypt(builder.ToString());

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(appSettingsPath, jsonNode.ToJsonString(options));

            MessageBox.Show("Database configuration restored successfully!", "Success", MessageBoxButton.OK,
                MessageBoxImage.Information);

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
        } finally{
        {
            IsProcessing = false;
        }}
    }
    
}