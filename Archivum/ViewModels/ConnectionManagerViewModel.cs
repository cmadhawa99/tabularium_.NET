using System.Collections.ObjectModel;
using System.Windows;
using Archivum.Models;
using Archivum.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Archivum.ViewModels;

public partial class ConnectionManagerViewModel : ObservableObject
{
    private readonly IConnectionsRegistryService _registryService;

    public ObservableCollection<ConnectionProfile> Connections { get; } = new();

    [ObservableProperty] private ConnectionProfile? _selectedConnection;

    public ConnectionManagerViewModel(IConnectionsRegistryService registryService)
    {
        _registryService = registryService;
        Reload();
    }

    private void Reload()
    {
        Connections.Clear();
        foreach (var c in _registryService.GetAll().OrderBy(c => c.DisplayName))
            Connections.Add(c);
    }

    [RelayCommand]
    private void Connect(Window window)
    {
        if (SelectedConnection == null) return;

        _registryService.SetActive(SelectedConnection.Id);
        SessionContext.ActiveProfile = SelectedConnection;

        window.DialogResult = true;
        window.Close();
    }

    [RelayCommand]
    private void Remove(ConnectionProfile profile)
    {
        if (profile == null) return;

        var result = MessageBox.Show(
            $"Remove connection '{profile.DisplayName}'?\n\n" +
            "This only removes it from this app (your encryption key, local settings and cache). " +
            "The actual PostgreSQL database and its data are NOT deleted.",
            "Remove Connection", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _registryService.Remove(profile.Id, deleteFiles: true);

        if (SessionContext.ActiveProfile?.Id == profile.Id)
            SessionContext.ActiveProfile = null;

        Reload();
    }
}