using System.Windows;
using System.Windows.Controls;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views
{
    public partial class DatabaseSetupWindow : Window
    {
        public DatabaseSetupWindow()
        {
            InitializeComponent();
            this.DataContext = new DatabaseSetupViewModel();
        }

        private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DatabaseSetupViewModel viewModel)
            {
                viewModel.DbPassword = DbPasswordBox.Password;
            }
        }
    }
}