using System.Windows;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views;

public partial class DatabaseSetupWindow : Window
{
    public DatabaseSetupWindow()
    {
        InitializeComponent();
        DataContext = new DatabaseSetupViewModel();
    }

    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DatabaseSetupViewModel viewModel) viewModel.DbPassword = DbPasswordBox.Password;
    }
}