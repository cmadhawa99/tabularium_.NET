using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

    [ObservableProperty] private ObservableCollection<SectorItem> _sectors = new();
    [ObservableProperty] private ObservableCollection<string> _fileTypes = new();
    
    public ColorPickerViewModel SectorColorPicker { get; } = new();
    
    [ObservableProperty] private string _newSectorName = string.Empty;
    [ObservableProperty] private bool _isColorPickerOpen = false;
    [ObservableProperty] private string _newFileTypeName = string.Empty;
    
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
        
        Sectors.Clear();
        foreach (var sector in prefs.Sectors) Sectors.Add(sector);
        
        FileTypes.Clear();
        foreach (var type in prefs.FileTypes) FileTypes.Add(type);

        if (File.Exists(_appSettingsPath))
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
    private void AddSector()
    {
        if (string.IsNullOrWhiteSpace(NewSectorName)) return;
        
        Sectors.Add(new SectorItem {Name = NewSectorName, ColorHex = SectorColorPicker.HexColor});
        NewSectorName = string.Empty;
        SectorColorPicker.SetHex("#FFFFFF");
    }

    [RelayCommand]
    private void RemoveSector(SectorItem sector)
    {
        if (sector != null) Sectors.Remove(sector);
    }

    [RelayCommand]
    private void AddFileType()
    {
        if (string.IsNullOrWhiteSpace(NewFileTypeName)) return;
        
        FileTypes.Add(NewFileTypeName);
        NewFileTypeName = string.Empty;
    }

    [RelayCommand]
    private void RemoveFileType(string fileType)
    {
        if (fileType != null) FileTypes.Remove(fileType);
    }

    [RelayCommand]
    private void OpenColorPicker() => IsColorPickerOpen = true;
    
    [RelayCommand]
    private void CloseColorPicker() => IsColorPickerOpen = false;
    

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
            AutoBackupDirectory = this.AutoBackupDirectory,
            
            Sectors = new List<SectorItem>(this.Sectors),
            FileTypes = new List<string>(this.FileTypes)
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

        WeakReferenceMessenger.Default.Send(new SettingsChangedMessage());
        ShowStatus("Settings saved successfully! (App restart required for Database changes to apply).", "#4CAF50");
        
    }
    

    private void ShowStatus(string message, string color)
    {
        StatusMessage =  message;
        StatusColor = color;
    }
    

    
}