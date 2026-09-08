using System.Windows;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views;

public partial class ConnectionSetupWindow : Window
{
    public ConnectionSetupWindow()
    {
        InitializeComponent();
        DataContext = new ConnectionSetupViewModel();
    }
}