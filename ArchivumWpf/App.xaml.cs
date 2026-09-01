// Test Script

using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Markup;
using ArchivumWpf.Localization;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;
using ArchivumWpf.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchivumWpf;

public partial class App : Application
{
    public App()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        Services = ConfigureServices();
    }

    public IServiceProvider Services { get; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        var rawConnString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
        var activeConnString = rawConnString;

        if (!string.IsNullOrEmpty(rawConnString) && !rawConnString.Contains("Host="))
            try
            {
                var masterKey = KeyVaultService.GetMasterKey();
                var cryptoService = new CryptoService(masterKey);
                activeConnString = cryptoService.Decrypt(rawConnString);
            }
            catch (Exception)
            {
            }

        if (string.IsNullOrWhiteSpace(activeConnString))
            activeConnString = "Host=placeholder;Database=placeholder;Username=placeholder;Password=placeholder";

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(activeConnString));

        services.AddSingleton<IPreferencesService, PreferencesService>();
        services.AddTransient<IArchiveService, ArchiveService>();
        services.AddSingleton<IDocumentService, DocumentService>();
        services.AddSingleton<IPdfRenderService, PdfRenderService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<CirculationViewModel>();
        services.AddSingleton<DisposalViewModel>();
        services.AddSingleton<EntryViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ClockViewModel>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();

        services.AddSingleton<DocumentsSearchViewModel>();
        services.AddTransient<DocumentManagerViewModel>();
        services.AddTransient<DocumentManagerWindow>();

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

        var languageCode = "en-US"; // Default

        if (prefs.Language == "Sinhala")
            languageCode = "si-LK";
        else if (prefs.Language == "Tamil") languageCode = "ta-LK";

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
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        // FIX C: Directly command your auto-generated Strings file to switch!
        Strings.Culture = culture;
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
                    var existingMasterKey = "W5bZnVXXs+eq9GLHdLTU6btIYmpHEQ9NLfxZjWAb4mI=";

                    var canaryBytes = new byte[32];
                    RandomNumberGenerator.Fill(canaryBytes);
                    var plainTextCanary = Convert.ToBase64String(canaryBytes);

                    var cryptoService = new CryptoService(existingMasterKey);
                    var encryptedCanary = cryptoService.Encrypt(plainTextCanary);

                    context.AppSecurityMetas.Add(new AppSecurityMeta
                        { EncryptedCanary = encryptedCanary });
                    await context.SaveChangesAsync();

                    MessageBox.Show(
                        "Security Canary injected into the database!\n\nIt was encrypted using your existing Master Key.",
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
                MessageBox.Show($"Security seeding failed: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Current.Shutdown();
            return;
        }

        if (args.Length > 0 && args[0].ToLower() == "--seed")
        {
            var count = 50;
            if (args.Length > 1 && int.TryParse(args[1], out var parsedCount)) count = parsedCount;

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

        var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var appSettingsExists = File.Exists(appSettingsPath);
        var valueExists = KeyVaultService.VaultExists();

        if (!valueExists)
        {
            var setupWindow = new SetupWindow();
            setupWindow.ShowDialog();
        }
        else if (!appSettingsExists)
        {
            var dbSetupWindow = new DatabaseSetupWindow();
            dbSetupWindow.ShowDialog();
        }

        if (!KeyVaultService.VaultExists() || !File.Exists(appSettingsPath))
        {
            MessageBox.Show("Application cannot start without valid database configuration and a security vault.",
                "Initialization Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
            return;
        }


        base.OnStartup(e);

        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var loginWindow = Services.GetRequiredService<LoginWindow>();

        if (loginWindow.ShowDialog() == true)
        {
            Current.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }
        else
        {
            Current.Shutdown();
        }
    }
}