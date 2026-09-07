using System.Windows;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views;

public partial class NewDatabaseWizardWindow : Window
{
    public NewDatabaseWizardWindow()
    {
        InitializeComponent();
        DataContext = new NewDatabaseWizardViewModel(new ConnectionsRegistryService());
    }

    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is NewDatabaseWizardViewModel vm)
            vm.DbPassword = ((System.Windows.Controls.PasswordBox)sender).Password;
    }

    private void AdminPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is NewDatabaseWizardViewModel vm)
            vm.AdminPassword = ((System.Windows.Controls.PasswordBox)sender).Password;
    }

    private void AdminPasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is NewDatabaseWizardViewModel vm)
            vm.AdminPasswordConfirm = ((System.Windows.Controls.PasswordBox)sender).Password;
    }
}