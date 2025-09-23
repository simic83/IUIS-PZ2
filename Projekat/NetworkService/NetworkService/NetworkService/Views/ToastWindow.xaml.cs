// Path: NetworkService/Views/ToastWindow.xaml.cs
using NetworkService.Services;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NetworkService.Views
{
    public partial class ToastWindow : Window
    {
        public long CreatedAtTicks { get; } = DateTime.UtcNow.Ticks;
        private readonly int _durationMs;

        public ToastWindow(string message, ToastType type, int durationMs)
        {
            InitializeComponent();
            _durationMs = Math.Max(1200, durationMs);

            MessageText.Text = message;

            // Boje/ikonice po tipu (dark stil, MDL2)
            switch (type)
            {
                case ToastType.Success:
                    Icon.Text = "\xE73E";         // CheckMark
                    Icon.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
                    break;
                case ToastType.Info:
                    Icon.Text = "\xE946";         // Info
                    Icon.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#5CC8FF"));
                    break;
                case ToastType.Warning:
                    Icon.Text = "\xE7BA";         // Warning
                    Icon.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
                    break;
                case ToastType.Error:
                    Icon.Text = "\xEA39";         // Error
                    Icon.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252"));
                    break;
            }

            Loaded += (_, __) => BeginShowAnimation();
        }

        private void BeginShowAnimation()
        {
            // Ulaz: fade-in + blagi slide
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() };
            BeginAnimation(OpacityProperty, fadeIn);

            var slide = new ThicknessAnimation
            {
                From = new Thickness(0, 8, 0, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase()
            };
            (Content as FrameworkElement).BeginAnimation(MarginProperty, slide);

            // Auto close
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_durationMs) };
            timer.Tick += (_, __) => { timer.Stop(); CloseWithAnimation(); };
            timer.Start();
        }

        private void CloseWithAnimation()
        {
            // Izlaz: fade-out + slide
            var fadeOut = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() };
            fadeOut.Completed += (_, __) => Close();
            BeginAnimation(OpacityProperty, fadeOut);

            var slide = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(0, 8, 0, 0),
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase()
            };
            (Content as FrameworkElement).BeginAnimation(MarginProperty, slide);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => CloseWithAnimation();
    }
}
