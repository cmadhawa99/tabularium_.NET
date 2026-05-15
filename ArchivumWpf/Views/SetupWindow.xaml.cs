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
    }
}