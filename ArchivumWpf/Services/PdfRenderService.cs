using System.IO;
using System.Windows.Media.Imaging;
using PDFtoImage;
using SkiaSharp;

namespace ArchivumWpf.Services;

public interface IPdfRenderService
{
    Task<int> GetPageCountAsync(byte[] pdfBytes);
    Task<BitmapImage> RenderPageAsync(byte[] pdfBytes, int pageIndex, double scale = 2.0);
}

public class PdfRenderService : IPdfRenderService
{
    public Task<int> GetPageCountAsync(byte[] pdfBytes)
    {
        return Task.Run(() =>
        {
            using var ms = new MemoryStream(pdfBytes);
            return Conversion.GetPageCount(ms);
        });
    }

    public Task<BitmapImage> RenderPageAsync(byte[] pdfBytes, int pageIndex, double scale = 2.0)
    {
        return Task.Run(() =>
        {
            using var ms = new MemoryStream(pdfBytes);

            var options = new RenderOptions
            {
                Dpi = (int)(96 * scale),
                WithAspectRatio = true
            };

            using var bitmap = Conversion.ToImage(ms, pageIndex, options: options);

            using var pngStream = new MemoryStream();
            bitmap.Encode(pngStream, SKEncodedImageFormat.Png, 100);
            pngStream.Position = 0;

            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = pngStream;
            img.EndInit();
            img.Freeze();
            return img;
        });
    }
}