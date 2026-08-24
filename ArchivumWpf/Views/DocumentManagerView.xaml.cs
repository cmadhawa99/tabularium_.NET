using System.Windows;
using System.Windows.Controls;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views
{

    public partial class DocumentManagerView : UserControl
    {
        public DocumentManagerView()
        {
            InitializeComponent();
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (DataContext is DocumentManagerViewModel vm)
                {
                    vm.HandleDrop(files);
                }
            }
        }
    }
}