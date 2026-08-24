using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ArchivumWpf.Views
{
    public partial class DocumentPreviewWindow : Window
    {
        public DocumentPreviewWindow(MemoryStream imageStream)
        {
            InitializeComponent();
            LoadImageFromStream(imageStream);
        }

        private void LoadImageFromStream(MemoryStream stream)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            
            PreviewImage.Source = bitmap;
            
            stream.Dispose();
        }
        
    }
}