using System.Windows;
using Archivum.Services;
using Archivum.ViewModels;

namespace Archivum.Views;

public partial class ConnectionManagerWindow : Window
{
    public ConnectionManagerWindow()
    {
        InitializeComponent();
        DataContext = new ConnectionManagerViewModel(new ConnectionsRegistryService());
    }
}