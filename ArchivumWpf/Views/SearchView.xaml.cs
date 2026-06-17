using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArchivumWpf.Views;

public partial class SearchView : UserControl
{
    private Point _lastMousePosition;
    private bool _isDragging  = false;
    
    public SearchView()
    {
        InitializeComponent();
    }

    private void ImageViewerOverlayIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isVisible && isVisible)
        {
            if (ImageScale != null && ImageTranslate != null)
            {
                ImageScale.ScaleX = 1.0;
                ImageScale.ScaleY = 1.0;
                ImageTranslate.X = 0;
                ImageTranslate.Y = 0;

            }
        }
    }

    private void ImageViewerMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double zoomFactor = e.Delta > 0 ? 1.1 : 1 / 1.1;

        if (ImageScale.ScaleX * zoomFactor < 0.2 || ImageScale.ScaleX * zoomFactor > 10)
        {
            return;
        }
        
        Point relative = e.GetPosition(ImageContainer);
        double absoluteX = relative.X * ImageScale.ScaleY + ImageTranslate.X;
        double absoluteY = relative.Y * ImageScale.ScaleY + ImageTranslate.Y;
        
        ImageScale.ScaleX *= zoomFactor;
        ImageScale.ScaleY *= zoomFactor;
        
        ImageTranslate.X = absoluteX - relative.X * ImageScale.ScaleX;
        ImageTranslate.Y = absoluteY - relative.Y * ImageScale.ScaleY;
    }

    private void ImageViewerMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement border)
        {
            _lastMousePosition = e.GetPosition(border);
            _isDragging = true;
            border.CaptureMouse();
        }
    }

    private void ImageViewerMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement border)
        {
            _isDragging = false;
            border.ReleaseMouseCapture();
        }
    }

    private void ImageViewerMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && sender is FrameworkElement border)
        {
            Point currentPosition = e.GetPosition(border);
            
            double dX = currentPosition.X - _lastMousePosition.X;
            double dY = currentPosition.Y - _lastMousePosition.Y;
            
            ImageTranslate.X += dX;
            ImageTranslate.Y += dY;
            
            _lastMousePosition = currentPosition;
        }
    }
    
}