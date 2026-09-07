using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using ArchivumWpf.Localization;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;
using ArchivumWpf.Views;
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

        // NOTE: No connection string is configured here anymore.
        // AppDbContext.OnConfiguring resolves the connection string dynamically
        // from SessionContext.ActiveProfile at the moment a context is created,
        // so the DbContextFactory just needs to know the type - no UseNpgsql() call needed here.
        services.AddDbContextFactory<AppDbContext>();

        services.AddSingleton<IPreferencesService, PreferencesService>();
        services.AddTransient<IArchiveService, ArchiveService>();
        services.AddSingleton<IDocumentService, DocumentService>();
        services.AddSingleton<IPdfRenderService, PdfRenderService>();
        services.AddSingleton<IConnectionsRegistryService, ConnectionsRegistryService>();

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

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Resolve the active connection profile (if any) before anything else touches
        //    PreferencesService, AppDbContext, or DocumentService - they all key off this.
        AppPaths.EnsureRootExists();

        var registryService = Services.GetRequiredService<IConnectionsRegistryService>();
        SessionContext.ActiveProfile = registryService.GetActive();

        // 2. Apply saved language preference (falls back to English defaults if no
        //    active profile / preferences file exists yet - PreferencesService handles that).
        var preferencesService = Services.GetRequiredService<IPreferencesService>();
        var prefs = preferencesService.GetPreferences();

        var languageCode = "en-US"; // Default

        if (prefs.Language == "Sinhala")
            languageCode = "si-LK";
        else if (prefs.Language == "Tamil") languageCode = "ta-LK";

        var culture = new CultureInfo(languageCode);

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        Strings.Culture = culture;

        // 3. No more single-vault / single-appsettings gate here. Connection setup
        //    (new empty DB wizard, attach existing DB, or pick a saved connection)
        //    now all happens from inside LoginWindow via LoginViewModel's
        //    OpenNewDatabaseWizardCommand / OpenConnectionManagerCommand.
        //    LoginViewModel itself blocks login attempts when HasActiveConnection is false.

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