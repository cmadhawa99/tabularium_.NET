using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArchivumWpf.Controls
{
    public partial class TimeSpinnerControl : UserControl
    {
        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register("SelectedTime", typeof(DateTime?), typeof(TimeSpinnerControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedTimeChanged));

        public DateTime? SelectedTime
        {
            get => (DateTime?)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);

        }

        private bool _isUpdating = false;
        private bool _isHoursActive = true;

        public TimeSpinnerControl()
        {
            InitializeComponent();
        }

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeSpinnerControl control) control.UpdateUI();
        }

        private void UpdateUI()
        {
            if (_isUpdating) return;
            _isUpdating = true;
            if (SelectedTime.HasValue)
            {
                HoursText.Text = SelectedTime.Value.Hour.ToString("D2");
                MinutesText.Text = SelectedTime.Value.Minute.ToString("D2");
            }
            else
            {
                HoursText.Text = "00";
                MinutesText.Text = "00";
            }

            _isUpdating = false;
        }

        private void TimeChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || HoursText == null || MinutesText == null) return;
            CommitTime();
        }

        private void CommitTime()
        {
            if (int.TryParse(HoursText.Text, out int h) && int.TryParse(MinutesText.Text, out int m))
            {
                h = Math.Clamp(h, 0, 23);
                m = Math.Clamp(m, 0, 59);

                DateTime baseDate = SelectedTime ?? DateTime.Today;

                _isUpdating = true;
                SelectedTime = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, h, m, 0);
                _isUpdating = false;
            }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        private void HoursText_GotFocus(object sender, RoutedEventArgs e) => _isHoursActive = true;
        private void MinutesText_GotFocus(object sender, RoutedEventArgs e) => _isHoursActive = false;

        private void Up_Click(object sender, RoutedEventArgs e) => AdjustTime(1);
        private void Down_Click(object sender, RoutedEventArgs e) => AdjustTime(-1);

        private void AdjustTime(int amount)
        {
            if (HoursText == null || MinutesText == null) return;
            
            int h = int.TryParse(HoursText.Text, out int parsedH) ? parsedH : 0;
            int m = int.TryParse(MinutesText.Text, out int parsedM) ? parsedM : 0;

            if (_isHoursActive)
            {
                h = (h + amount + 24) % 24;
            }
            else
            {
                m = (m + amount + 60) % 60;
            }

            _isUpdating = true;
            HoursText.Text = h.ToString("D2");
            MinutesText.Text = m.ToString("D2");
            DateTime baseDate = SelectedTime ?? DateTime.Today;
            SelectedTime = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, h, m, 0);
            _isUpdating = false;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }

    }

}
