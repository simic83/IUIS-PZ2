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
                // Check if dragging from a slot
                var parentBorder = border.Parent as Border;
                if (parentBorder != null && parentBorder.Tag != null)
                {
                    viewModel.StartDragFromSlot((int)parentBorder.Tag);
                }
                else
                {
                    viewModel.StartDragFromSlot(-1); // Dragging from tree
                }

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

        private void SlotBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;
                var server = viewModel.GetServerInSlot(slotIndex);

                if (server != null)
                {
                    viewModel.DraggedServer = server;
                    viewModel.StartDragFromSlot(slotIndex);
                    DragDrop.DoDragDrop(border, server, DragDropEffects.Move);
                }
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
                    int targetSlotIndex = (int)border.Tag;
                    var existingServer = viewModel.GetServerInSlot(targetSlotIndex);

                    if (viewModel.DraggedFromSlot >= 0)
                    {
                        // Dragging from another slot
                        if (existingServer != null && existingServer != server)
                        {
                            // Swap servers between slots
                            viewModel.PlaceServerInSlot(existingServer, viewModel.DraggedFromSlot);
                        }
                        else if (existingServer == null)
                        {
                            // Clear the old slot
                            viewModel.RemoveServerFromSlot(viewModel.DraggedFromSlot);
                        }
                    }

                    // Place the dragged server in the target slot
                    viewModel.PlaceServerInSlot(server, targetSlotIndex);
                    viewModel.StartDragFromSlot(-1); // Reset
                }
            }
            e.Handled = true;
        }
    }
}