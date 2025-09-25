using NetworkService.Model;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NetworkService.Views
{
    public partial class ConfirmationDialog : Window
    {
        public enum DialogType
        {
            Delete,
            Warning,
            Confirm
        }

        public bool IsConfirmed { get; private set; }

        public ConfirmationDialog(string message, string title = "Confirm Action", DialogType type = DialogType.Confirm)
        {
            InitializeComponent();

            MessageText.Text = message;
            TitleText.Text = title;

            // Customize based on type
            switch (type)
            {
                case DialogType.Delete:
                    TitleIcon.Text = "\xE74D";
                    TitleIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f66"));
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a44"));
                    WarningPanel.Visibility = Visibility.Visible;
                    break;

                case DialogType.Warning:
                    TitleIcon.Text = "\xE7BA";
                    TitleIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fa0"));
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f90"));
                    WarningPanel.Visibility = Visibility.Visible;
                    break;

                case DialogType.Confirm:
                    TitleIcon.Text = "\xE8FB";
                    TitleIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0af"));
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0a4"));
                    WarningPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        // Constructor for deleting a server with details
        public ConfirmationDialog(Server server) : this($"Are you sure you want to delete server '{server.Name}'?", "Confirm Delete Server", DialogType.Delete)
        {
            // Show server details
            DetailsSection.Visibility = Visibility.Visible;
            ServerIdText.Text = server.Id.ToString("000");
            ServerNameText.Text = server.Name;
            ServerTypeText.Text = server.Type?.Name ?? "Unknown";
            ServerIpText.Text = server.IPAddress;

            // Add status color to name if needed
            if (server.Status == "online")
            {
                ServerNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f0"));
            }
            else if (server.Status == "warning")
            {
                ServerNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fa0"));
            }
            else if (server.Status == "offline")
            {
                ServerNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f66"));
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        // Static helper methods for easy usage
        public static bool ShowDeleteConfirmation(Server server, Window owner = null)
        {
            var dialog = new ConfirmationDialog(server);
            if (owner != null) dialog.Owner = owner;
            dialog.ShowDialog();
            return dialog.IsConfirmed;
        }

        public static bool ShowConfirmation(string message, string title = "Confirm", Window owner = null)
        {
            var dialog = new ConfirmationDialog(message, title, DialogType.Confirm);
            if (owner != null) dialog.Owner = owner;
            dialog.ShowDialog();
            return dialog.IsConfirmed;
        }

        public static bool ShowWarning(string message, string title = "Warning", Window owner = null)
        {
            var dialog = new ConfirmationDialog(message, title, DialogType.Warning);
            if (owner != null) dialog.Owner = owner;
            dialog.ShowDialog();
            return dialog.IsConfirmed;
        }
    }
}