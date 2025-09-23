using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetworkService.Views
{
    public partial class DisplayView : UserControl
    {
        private DisplayViewModel viewModel => DataContext as DisplayViewModel;
        private Point startPoint;

        public DisplayView()
        {
            InitializeComponent();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
            var border = sender as Border;
            var server = border?.DataContext as Server;

            if (server != null && viewModel != null)
            {
                viewModel.DraggedServer = server;
                viewModel.StartDragFromSlot(-1); // Dragging from tree

                // Change cursor to indicate dragging
                Mouse.OverrideCursor = Cursors.Hand;

                DragDrop.DoDragDrop(border, server, DragDropEffects.Move);

                // Reset cursor
                Mouse.OverrideCursor = null;
                viewModel.EndDrag();
            }
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(null);

                if ((Math.Abs(currentPosition.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(currentPosition.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    Border_MouseLeftButtonDown(sender, null);
                }
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

                    // Change cursor to indicate dragging
                    Mouse.OverrideCursor = Cursors.Hand;

                    DragDrop.DoDragDrop(border, server, DragDropEffects.Move);

                    // Reset cursor
                    Mouse.OverrideCursor = null;
                    viewModel.EndDrag();
                }
            }
        }

        private void SlotBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;
                var server = viewModel.GetServerInSlot(slotIndex);

                if (server != null)
                {
                    // Simple right-click to remove from slot
                    viewModel.RemoveServerFromSlot(slotIndex);
                }
            }
        }

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Server)))
            {
                e.Effects = DragDropEffects.Move;

                // Set drag over state for visual feedback
                var border = sender as Border;
                if (border != null && viewModel != null)
                {
                    int slotIndex = (int)border.Tag;
                    viewModel.SetSlotDragOver(slotIndex, true);
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Canvas_DragLeave(object sender, DragEventArgs e)
        {
            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;
                viewModel.SetSlotDragOver(slotIndex, false);
            }
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

                    // Clear drag over state
                    viewModel.SetSlotDragOver(targetSlotIndex, false);
                    viewModel.EndDrag();
                }
            }
            e.Handled = true;
        }
    }
}