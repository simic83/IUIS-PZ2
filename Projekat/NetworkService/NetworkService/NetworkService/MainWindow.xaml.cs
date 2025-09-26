// MainWindow.xaml.cs
using NetworkService.ViewModel;
using System.Windows;

namespace NetworkService
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainWindowViewModel();
            DataContext = viewModel;

            // Set initial focus to terminal
            Loaded += (s, e) => FocusTerminalInput();
        }

        public void FocusTerminalInput()
        {
            if (TerminalInput != null)
            {
                TerminalInput.Focus();
                TerminalInput.SelectionStart = TerminalInput.Text?.Length ?? 0;
                TerminalInput.SelectionLength = 0;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            viewModel?.Dispose();
        }
    }
}