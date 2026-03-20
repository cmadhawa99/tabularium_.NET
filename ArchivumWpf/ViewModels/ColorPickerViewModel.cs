using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchivumWpf.ViewModels;

public partial class ColorPickerViewModel : ObservableObject
{
    private bool _isUpdatingInternally =  false;
    
    [ObservableProperty] private int _alpha = 255;
    [ObservableProperty] private int _red = 255;
    [ObservableProperty] private int _green = 255;
    [ObservableProperty] private int _blue = 255;

    [ObservableProperty] private string _hexColor = "#FFFFFF";

    public ColorPickerViewModel()
    {
        UpdateHexFromSliders();
    }
    
    partial void OnAlphaChanged(int value) => UpdateHexFromSliders();
    partial void OnRedChanged(int value) => UpdateHexFromSliders();
    partial void OnGreenChanged(int value) => UpdateHexFromSliders();
    partial void OnBlueChanged(int value) => UpdateHexFromSliders();
    partial void OnHexColorChanged(string value) => UpdateSlidersFromHex(value);

    public void SetHex(string hex)
    {
        HexColor = hex;
    }

    private void UpdateHexFromSliders()
    {
        if (_isUpdatingInternally) return;
        _isUpdatingInternally = true;

        try
        {
            if (Alpha == 255)
                HexColor = $"#{Red:X2}{Green:X2}{Blue:X2}";
            else
                HexColor = $"#{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private void UpdateSlidersFromHex(string hex)
    {
        if (_isUpdatingInternally || string.IsNullOrWhiteSpace(hex) || !hex.StartsWith("#")) return; 
        
        _isUpdatingInternally = true;

        try
        {
            string cString = hex.Trim().Replace("#", "");

            if (cString.Length == 8)
            {
                Alpha = int.Parse(cString.Substring(0, 2), NumberStyles.HexNumber);
                Red = int.Parse(cString.Substring(2, 2), NumberStyles.HexNumber);
                Green = int.Parse(cString.Substring(4, 2), NumberStyles.HexNumber);
                Blue = int.Parse(cString.Substring(6, 2), NumberStyles.HexNumber);
            }
            else if (cString.Length == 6)
            {
                Alpha = 255;
                Red = int.Parse(cString.Substring(0, 2), NumberStyles.HexNumber);
                Green = int.Parse(cString.Substring(2, 2), NumberStyles.HexNumber);
                Blue = int.Parse(cString.Substring(4, 2), NumberStyles.HexNumber);
            }
        }
        
        catch {}
        
        finally{ _isUpdatingInternally = false; }
    }

}