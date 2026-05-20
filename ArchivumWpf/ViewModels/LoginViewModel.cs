using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using ArchivumWpf.Services;
using ArchivumWpf.Models;

namespace ArchivumWpf.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty] private string _usernameInput = string.Empty;
    public string PasswordInput { get; set; } = string.Empty;

    [ObservableProperty] private bool _isRecoveryMode = false;
    public string MasterKeyInput { get; set; } = string.Empty;
    [ObservableProperty] private string _newUsernameInput = string.Empty;
    public string NewPasswordInput { get; set; } = string.Empty;
    
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isProcessing = false;
    
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public LoginViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [RelayCommand]
    private async Task LoginAsync(Window window)
    {
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

            if (matchedUser == null || !VerifyPassword(PasswordInput, matchedUser.PasswordHash))
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            window.DialogResult = true;
            window.Close();
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

            try
            {
                var cryptoService = new CryptoService(MasterKeyInput);
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
            userToReset.PasswordHash = HashPassword(NewPasswordInput);

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

    private string HashPassword(string password)
    {
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        
        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expectedHash = Convert.FromBase64String(parts[1]);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] actualHash = pbkdf2.GetBytes(32);
        
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}