// Test Script

using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;
using ArchivumWpf.Views;

namespace ArchivumWpf;

public partial class App : Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional:true, reloadOnChange: true)
            .Build();
        
        string rawConnString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
        string activeConnString = rawConnString;

        if (!string.IsNullOrEmpty(rawConnString) && !rawConnString.Contains("Host="))
        {
            try
            {
                var masterKey = KeyVaultService.GetMasterKey();
                var cryptoService = new CryptoService(masterKey);
                activeConnString = cryptoService.Decrypt(rawConnString);
            }
            catch (Exception)
            {
            }
        }

        if (string.IsNullOrWhiteSpace(activeConnString))
        {
            activeConnString = "Host=placeholder;Database=placeholder;Username=placeholder;Password=placeholder";
        }
        
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(activeConnString));
        
        services.AddSingleton<IPreferencesService, PreferencesService>();
        
        services.AddTransient<IArchiveService, ArchiveService>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<CirculationViewModel>();
        services.AddSingleton<DisposalViewModel>();
        services.AddSingleton<EntryViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ClockViewModel>();
        
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // =========================================================================
        // 1. APPLY SAVED LANGUAGE AT STARTUP (Bulletproof Method)
        // =========================================================================
        var preferencesService = Services.GetRequiredService<IPreferencesService>();
        var prefs = preferencesService.GetPreferences();
        
        string languageCode = "en-US"; // Default
        
        if (prefs.Language == "Sinhala")
        {
            languageCode = "si-LK";
        }
        else if (prefs.Language == "Tamil")
        {
            languageCode = "ta-LK";
        }

        // Create the Culture Object
        var culture = new CultureInfo(languageCode);

        // FIX A: Force the active threads to use the culture
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // FIX B: Force internal WPF controls (like DatePickers) to translate
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        // FIX C: Directly command your auto-generated Strings file to switch!
        ArchivumWpf.Localization.Strings.Culture = culture;
        // =========================================================================


        // =========================================================================
        // =================== [REMOVE BEFORE DEPLOYMENT START] ====================
        // =========================================================================
        string[] args = e.Args;

        if (args.Length > 0 && args[0].ToLower() == "--seed-security")
        {
            try
            {
                var factory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var context = await factory.CreateDbContextAsync();

                if (!context.AppSecurityMetas.Any())
                {
                    
                    string existingMasterKey = "W5bZnVXXs+eq9GLHdLTU6btIYmpHEQ9NLfxZjWAb4mI=";

                    byte[] canaryBytes = new byte[32];
                    System.Security.Cryptography.RandomNumberGenerator.Fill(canaryBytes);
                    string plainTextCanary = Convert.ToBase64String(canaryBytes);

                    var cryptoService = new ArchivumWpf.Services.CryptoService(existingMasterKey);
                    string encryptedCanary = cryptoService.Encrypt(plainTextCanary);

                    context.AppSecurityMetas.Add(new ArchivumWpf.Models.AppSecurityMeta
                        { EncryptedCanary = encryptedCanary });
                    await context.SaveChangesAsync();

                    MessageBox.Show("Security Canary injected into the database!\n\nIt was encrypted using your existing Master Key.", 
                        "Terminal Seeder", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "A Security Canary already exists in the database. Please clear the AppSecurityMetas table if you want to generate a new one.",
                        "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Security seeding failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            Current.Shutdown();
            return;
            
        }
        
        if (args.Length > 0 && args[0].ToLower() == "--seed")
        {
            int count = 50; 
            if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
            {
                count = parsedCount;
            }

            try
            {
                var factory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var context = await factory.CreateDbContextAsync();
                var seeder = new DatabaseSeeder(context);
                
                await seeder.SeedFileRecordsAsync(count);
                
                MessageBox.Show($"Successfully seeded {count} fake Sinhala records into the database!", 
                    "Terminal Seeder", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database seeding failed: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Current.Shutdown();
            return; 
        }
        // =========================================================================
        // =================== [REMOVE BEFORE DEPLOYMENT END] ======================
        // =========================================================================
        
        // 2. Security check

        string appSettingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        bool appSettingsExists = System.IO.File.Exists(appSettingsPath);
        bool valueExists = ArchivumWpf.Services.KeyVaultService.VaultExists();

        if (!valueExists)
        {
            var setupWindow = new ArchivumWpf.Views.SetupWindow();
            setupWindow.ShowDialog();
        } 
        else if (!appSettingsExists)
        {
            var dbSetupWindow = new ArchivumWpf.Views.DatabaseSetupWindow();
            dbSetupWindow.ShowDialog();
        }
        
        if (!ArchivumWpf.Services.KeyVaultService.VaultExists() || !System.IO.File.Exists(appSettingsPath))
        {
            MessageBox.Show("Application cannot start without valid database configuration and a security vault.", "Initialization Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
            return;
        }  
        

        base.OnStartup(e);
        
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }
}