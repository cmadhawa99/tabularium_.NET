using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArchivumWpf.ViewModels;

public partial class ConnectionSetupViewModel : ObservableObject
{
    [RelayCommand]
    private void OpenNewDatabaseWizard(Window ownerWindow)
    {
        var window = new Views.NewDatabaseWizardWindow { Owner = ownerWindow };
        if (window.ShowDialog() == true) CloseWithResult(ownerWindow);
    }

    [RelayCommand]
    private void OpenConnectExistingDatabase(Window ownerWindow)
    {
        var window = new Views.ConnectExistingDatabaseWindow { Owner = ownerWindow };
        if (window.ShowDialog() == true) CloseWithResult(ownerWindow);
    }

    [RelayCommand]
    private void OpenConnectionManager(Window ownerWindow)
    {
        var window = new Views.ConnectionManagerWindow { Owner = ownerWindow };
        if (window.ShowDialog() == true) CloseWithResult(ownerWindow);
    }


    private static void CloseWithResult(Window window)
    {
        window.DialogResult = true;
        window.Close();
    }
}