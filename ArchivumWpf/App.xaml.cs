using System;
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
        //services.AddTransient<IArchiveService, DummyArchiveService>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<CirculationViewModel>();
        services.AddSingleton<EntryViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<DisposalViewModel>();

        services.AddSingleton<MainWindow>();
        
        return services.BuildServiceProvider();
    }

    // Note: Changed to 'async void' so we can await the seeder task
    protected override async void OnStartup(StartupEventArgs e)
    {
        // THIS IS A TEST SCRIPT
        // =========================================================================
        // ================== [REMOVE BEFORE DEPLOYMENT START] =====================
        // =========================================================================
        // This block intercepts the terminal commands. E.g. "dotnet run -- seed 250"
        if (e.Args.Length > 0 && e.Args[0].ToLower() == "seed")
        {
            int count = 100; // Default count
            if (e.Args.Length > 1 && int.TryParse(e.Args[1], out int parsedCount))
            {
                count = parsedCount;
            }

            try
            {
                // Grab the perfectly configured DbContext from your existing Services setup!
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

            // CRITICAL: Shut down the app so the normal UI does not open
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