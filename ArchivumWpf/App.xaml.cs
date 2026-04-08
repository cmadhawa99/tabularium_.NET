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
            .AddJsonFile("appsettings.json", optional:false, reloadOnChange: true)
            .Build();
        
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));
        
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

        base.OnStartup(e);
        
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }
}