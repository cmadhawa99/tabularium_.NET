using System.Windows;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views;

public partial class DocumentManagerWindow : Window
{
    public DocumentManagerWindow(DocumentManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}