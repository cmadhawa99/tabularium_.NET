using System.Windows;
using ArchivumWpf.Services;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views;

public partial class ConnectionManagerWindow : Window
{
    public ConnectionManagerWindow()
    {
        InitializeComponent();
        DataContext = new ConnectionManagerViewModel(new ConnectionsRegistryService());
    }
}