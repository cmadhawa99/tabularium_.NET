using System.Security.Cryptography;
using System.Windows;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace ArchivumWpf.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IConnectionsRegistryService _registryService;

    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _isRecoveryMode;
    [ObservableProperty] private string _newUsernameInput = string.Empty;
    [ObservableProperty] private string _usernameInput = string.Empty;
    [ObservableProperty] private string _activeConnectionName = "No database connected";
    [ObservableProperty] private bool _hasActiveConnection;

    public string PasswordInput { get; set; } = string.Empty;
    public string MasterKeyInput { get; set; } = string.Empty;
    public string NewPasswordInput { get; set; } = string.Empty;

    public LoginViewModel(IDbContextFactory<AppDbContext> dbContextFactory, IConnectionsRegistryService registryService)
    {
        _dbContextFactory = dbContextFactory;
        _registryService = registryService;
        RefreshActiveConnection();
    }

    private void RefreshActiveConnection()
    {
        var active = _registryService.GetActive();
        SessionContext.ActiveProfile = active;
        HasActiveConnection = active != null;
        ActiveConnectionName = active != null ? active.DisplayName : "No database connected";
    }
    
    [RelayCommand]
    private void OpenConnectionSetup(Window ownerWindow)
    {
        var window = new Views.ConnectionSetupWindow { Owner = ownerWindow };
        if (window.ShowDialog() == true)
            RefreshActiveConnection();
    }
    
    [RelayCommand]
    private async Task LoginAsync(Window window)
    {
        if (!HasActiveConnection || SessionContext.ActiveProfile == null)
        {
            ErrorMessage = "Please connect to a database first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput))
        {
            ErrorMessage = "Please enter both username and password.";
            return;
        }

        IsProcessing = true;
        ErrorMessage = string.Empty;

        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var allUsers = await context.Users.Where(u => u.IsActive).ToListAsync();

            var matchedUser = allUsers.FirstOrDefault(u =>
                string.Equals(u.Username, UsernameInput, StringComparison.OrdinalIgnoreCase));

            var profileFolder = AppPaths.ProfileFolder(SessionContext.ActiveProfile.Id);

            string masterKey = KeyVaultService.GetMasterKey(profileFolder); 
            
            var cryptoService = new CryptoService(masterKey);

            string pepper = PepperStorageHelper.GetPepper(cryptoService);

            if (matchedUser == null || !PasswordHasher.Verify(PasswordInput, matchedUser.PasswordHash, pepper))
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }


            window.DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void ToggleRecoveryMode()
    {
        IsRecoveryMode = !IsRecoveryMode;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ResetAccountAsync()
    {
        if (!HasActiveConnection)
        {
            ErrorMessage = "Please connect to a database first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MasterKeyInput) || string.IsNullOrWhiteSpace(NewUsernameInput) ||
            string.IsNullOrWhiteSpace(NewPasswordInput))
        {
            ErrorMessage = "Please fill in all recovery fields.";
            return;
        }

        IsProcessing = true;
        ErrorMessage = string.Empty;

        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var canaryMeta = await context.AppSecurityMetas.FirstOrDefaultAsync();
            if (canaryMeta == null) throw new Exception("Security Canary missing from database.");


            CryptoService cryptoService;
            try
            {
                cryptoService = new CryptoService(MasterKeyInput);
                cryptoService.Decrypt(canaryMeta.EncryptedCanary);
            }
            catch (CryptographicException)
            {
                ErrorMessage = "Access Denied. Invalid Master Recovery Key.";
                return;
            }


            var userToReset = await context.Users.FirstOrDefaultAsync();
            if (userToReset == null)
            {
                userToReset = new User { Role = "Admin", IsActive = true };
                context.Users.Add(userToReset);
            }

            userToReset.Username = NewUsernameInput;

            string pepper = PepperStorageHelper.GetPepper(cryptoService);
            userToReset.PasswordHash = PasswordHasher.Hash(NewPasswordInput, pepper);


            await context.SaveChangesAsync();

            MessageBox.Show("Account credentials reset successfully! You may now log in.", "Recovery Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
            ToggleRecoveryMode();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Recovery failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}