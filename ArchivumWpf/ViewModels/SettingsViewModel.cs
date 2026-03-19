using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using Npgsql;


namespace ArchivumWpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    
    private readonly IPreferencesService _preferencesService;
    private readonly string _appSettingsPath;
    
    [ObservableProperty] private string _organizationName =  string.Empty;
    [ObservableProperty] private string _appTitle = string.Empty;
    [ObservableProperty] private string _currentUser  = string.Empty;
    [ObservableProperty] private string _selectedLanguage  =  string.Empty;
    [ObservableProperty] private string _selectedTheme = string.Empty;
    [ObservableProperty] private int _defaultPaginationSize;
    [ObservableProperty] private string _defaultExportDirectory  = string.Empty;
  
    
    public ObservableCollection<string> AvailableLanguages { get; } = new() { "English", "Sinhala", "Tamil" };
    public ObservableCollection<string> AvailableThemes { get; } = new() { "Dark", "Light" };
    public ObservableCollection<int> PaginationOptions { get; } = new() { 25, 50, 100, 250, 500 };
    
    [ObservableProperty] private string _dbHost = string.Empty;
    [ObservableProperty] private string _dbName = string.Empty;
    [ObservableProperty] private string _dbUser = string.Empty;
    [ObservableProperty] private string _dbPassword = string.Empty;
    
    [ObservableProperty] private bool _autoBackupEnabled;
    [ObservableProperty] private string _autoBackupDirectory = string.Empty;
    
    [ObservableProperty] private string _statusMessage =  string.Empty;
    [ObservableProperty] private string _statusColor = "Gray";
    [ObservableProperty] private bool _isProcessing = false;

    public SettingsViewModel(IPreferencesService preferencesService)
    {
        _preferencesService =  preferencesService;
        _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        LoadSettings();
    }

    private void LoadSettings()
    {
        var prefs = _preferencesService.GetPreferences();
        
        OrganizationName = prefs.OrganizationName;
        AppTitle = prefs.AppTitle;
        CurrentUser = prefs.CurrentUser;
        SelectedLanguage =  prefs.Language;
        SelectedTheme = prefs.Theme;
        DefaultPaginationSize = prefs.DefaultPaginationSize;
        DefaultExportDirectory = prefs.DefaultExportDirectory;
        AutoBackupEnabled = prefs.AutoBackupEnabled;
        AutoBackupDirectory = prefs.AutoBackupDirectory;

        if (!File.Exists(_appSettingsPath))
        {
            try
            {
                var jsonNode = JsonNode.Parse(File.ReadAllText(_appSettingsPath));
                string connString = jsonNode?["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(connString))
                {
                    var builder = new NpgsqlConnectionStringBuilder(connString);
                    DbHost = builder.Host ?? "";
                    DbName = builder.Database ?? "";
                    DbUser = builder.Username ?? "";
                    DbPassword = builder.Password ?? "";
                }
            }
            catch{}
        }
    }

    [RelayCommand]
    private async Task TestDatabaseConnectionAsync()
    {
        IsProcessing = true;
        ShowStatus ("Testing connection...", "Yellow");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = DbHost,
            Database = DbName,
            Username = DbUser,
            Password = DbPassword,
            Timeout = 3
        };

        try
        {
            await using var connection = new NpgsqlConnection(builder.ToString());
            await connection.OpenAsync();
            ShowStatus("Connection Successful!", "#4CAF50");
        }
        catch (Exception ex)
        {
            ShowStatus($"Connection Failed: {ex.Message}", "#F44336");
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    

    [RelayCommand]
    private void SaveSettings()
    {
        var prefs = new UserPreferences()
        {
            OrganizationName = this.OrganizationName,
            AppTitle = this.AppTitle,
            CurrentUser = this.CurrentUser,
            Language = this.SelectedLanguage,
            Theme = this.SelectedTheme,
            DefaultPaginationSize = this.DefaultPaginationSize,
            DefaultExportDirectory = this.DefaultExportDirectory,
            AutoBackupEnabled = this.AutoBackupEnabled,
            AutoBackupDirectory = this.AutoBackupDirectory
        };
        
        _preferencesService.SavePreferences(prefs);

        if (File.Exists(_appSettingsPath))
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = DbHost,
                    Database = DbName,
                    Username = DbUser,
                    Password = DbPassword

                };

                var jsonNode = JsonNode.Parse(File.ReadAllText(_appSettingsPath));
                if (jsonNode?["ConnectionStrings"] != null)
                {
                    jsonNode["ConnectionStrings"]!["DefaultConnection"] = builder.ToString();

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(_appSettingsPath, jsonNode.ToJsonString(options));
                }
            }

            catch (Exception ex)
            {
                ShowStatus($"Error saving appsettings.json: {ex.Message}", "#F44336");
                return;
            }
        }
        
        ShowStatus("Settings saved successfully! (App restart required for Database changes to apply).", "#4CAF50");
        
    }

    private void ShowStatus(string message, string color)
    {
        StatusMessage =  message;
        StatusColor = color;
    }
}