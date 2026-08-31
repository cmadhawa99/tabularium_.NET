using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArchivumWpf.ViewModels;

namespace ArchivumWpf.Views
{

    public partial class DocumentManagerView : UserControl
    {
        public DocumentManagerView() => InitializeComponent();

        private async void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is not DocumentManagerViewModel vm || !vm.IsImportAllowed) return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                await vm.ImportPathsAsync(files);
            }
        }

        private void PreviewOverlay_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not DocumentManagerViewModel vm || !vm.IsPreviewOpen) return;

            switch (e.Key)
            {
                case Key.Escape:
                    vm.ClosePreviewCommand.Execute(null);
                    break;
                case Key.Right when vm.IsPdfMode:
                    vm.NextPdfPageCommand.Execute(null);
                    break;
                case Key.Left when vm.IsPdfMode:
                    vm.PreviousPdfPageCommand.Execute(null);
                    break;
                case Key.OemPlus or Key.Add when vm.IsPdfMode:
                    vm.ZoomInPdfCommand.Execute(null);
                    break;
                case Key.OemMinus or Key.Subtract when vm.IsPdfMode:
                    vm.ZoomOutPdfCommand.Execute(null);
                    break;
            }
        }
    }
}