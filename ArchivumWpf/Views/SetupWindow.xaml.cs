using System.Windows;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views
{
    public partial class SetupWindow : Window
    {
        public SetupWindow()
        {
            InitializeComponent();
            this.DataContext = new SetupViewModel();
        }

        private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SetupViewModel viewModel)
            {
                viewModel.DbPassword = DbPasswordBox.Password;
            }
        }

        private void RecoveryKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SetupViewModel viewModel)
            {
                viewModel.RecoveryKeyInput = RecoveryKeyBox.Password;
            }
        }
    }
}