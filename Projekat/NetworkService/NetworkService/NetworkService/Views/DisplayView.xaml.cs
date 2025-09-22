using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkService.Model;
using NetworkService.ViewModel;

namespace NetworkService.Views
{
    public partial class DisplayView : UserControl
    {
        private DisplayViewModel viewModel => DataContext as DisplayViewModel;

        public DisplayView()
        {
            InitializeComponent();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var server = border?.DataContext as Server;

            if (server != null && viewModel != null)
            {
                viewModel.DraggedServer = server;
                DragDrop.DoDragDrop(border, server, DragDropEffects.Move);
            }
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Border_MouseLeftButtonDown(sender, null);
            }
        }

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Server)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Server)))
            {
                var server = e.Data.GetData(typeof(Server)) as Server;
                var border = sender as Border;

                if (server != null && border != null && viewModel != null)
                {
                    int slotIndex = int.Parse(border.Tag.ToString());
                    viewModel.PlaceServerInSlot(server, slotIndex);

                    // Update visual
                    var contentPresenter = border.FindName($"slot{slotIndex}") as ContentPresenter;
                    if (contentPresenter != null)
                    {
                        var serverDisplay = new Border
                        {
                            Background = new System.Windows.Media.SolidColorBrush(
                                server.Status == "online" ? System.Windows.Media.Colors.DarkGreen :
                                server.Status == "warning" ? System.Windows.Media.Colors.DarkOrange :
                                System.Windows.Media.Colors.DarkRed),
                            Padding = new Thickness(10),
                            Child = new StackPanel
                            {
                                Children =
                                {
                                    new TextBlock { Text = server.Name, Foreground = System.Windows.Media.Brushes.White },
                                    new TextBlock { Text = $"ID: {server.Id}", Foreground = System.Windows.Media.Brushes.LightGray },
                                    new TextBlock { Text = server.MeasurementDisplay, Foreground = System.Windows.Media.Brushes.White }
                                }
                            }
                        };
                        contentPresenter.Content = serverDisplay;
                    }
                }
            }
            e.Handled = true;
        }
    }
}