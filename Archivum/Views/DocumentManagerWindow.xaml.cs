using System.Windows;
using Archivum.ViewModels;

namespace Archivum.Views;

public partial class DocumentManagerWindow : Window
{
    public DocumentManagerWindow(DocumentManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}