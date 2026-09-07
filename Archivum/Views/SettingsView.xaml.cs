using System.Windows;
using System.Windows.Controls;
using Archivum.ViewModels;

namespace Archivum.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel) viewModel.DbPassword = ((PasswordBox)sender).Password;
    }
}