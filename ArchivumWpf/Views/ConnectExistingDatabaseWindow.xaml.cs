using System.Windows;
using System.Windows.Controls;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views;

public partial class ConnectExistingDatabaseWindow : Window
{
    public ConnectExistingDatabaseWindow()
    {
        InitializeComponent();
        DataContext = new ConnectExistingDatabaseViewModel(new ConnectionsRegistryService());
    }

    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectExistingDatabaseViewModel vm)
            vm.DbPassword = ((PasswordBox)sender).Password;
    }
}