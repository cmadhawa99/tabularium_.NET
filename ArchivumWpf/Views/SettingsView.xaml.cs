using System.Windows;
using System.Windows.Controls;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is SettingsViewModel viewModel)
        {
            viewModel.DbPassword = ((PasswordBox)sender).Password;
        }
    }
}