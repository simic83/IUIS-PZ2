using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetworkService.Views
{
    public partial class DisplayView : UserControl
    {
        private DisplayViewModel viewModel => DataContext as DisplayViewModel;
        private Point startPoint;
        private DispatcherTimer updateTimer;

        public DisplayView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;

            // Setup timer for updating connection points
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Delay to ensure layout is complete
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateConnectionPoints();
                // Make connections visible after positions are calculated
                if (viewModel != null)
                {
                    viewModel.UpdateConnectionPositions();
                }
            }), DispatcherPriority.Loaded);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update connection points when window size changes
            if (!updateTimer.IsEnabled)
            {
                updateTimer.Start();
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            updateTimer.Stop();
            UpdateConnectionPoints();
        }

        private void UpdateConnectionPoints()
        {
            if (viewModel == null || SlotsItemsControl == null) return;

            // Get the actual size of the grid
            var uniformGrid = GetVisualChild<UniformGrid>(SlotsItemsControl);
            if (uniformGrid == null || uniformGrid.ActualWidth == 0 || uniformGrid.ActualHeight == 0)
                return;

            // Calculate slot dimensions
            double totalWidth = uniformGrid.ActualWidth;
            double totalHeight = uniformGrid.ActualHeight;
            double slotWidth = totalWidth / 4; // 4 columns
            double slotHeight = totalHeight / 3; // 3 rows

            // Update all slot center points
            for (int i = 0; i < viewModel.DisplaySlots.Count; i++)
            {
                var container = SlotsItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container != null)
                {
                    // Get the actual position relative to the ConnectionCanvas
                    try
                    {
                        var transform = container.TransformToAncestor(MainDisplayGrid);
                        var topLeft = transform.Transform(new Point(0, 0));

                        // Calculate center point
                        var centerX = topLeft.X + (container.ActualWidth / 2);
                        var centerY = topLeft.Y + (container.ActualHeight / 2);

                        viewModel.DisplaySlots[i].CenterPoint = new Point(centerX, centerY);
                    }
                    catch
                    {
                        // Fallback to calculated positions if transform fails
                        int row = i / 4;
                        int col = i % 4;

                        double x = (col * slotWidth) + (slotWidth / 2);
                        double y = (row * slotHeight) + (slotHeight / 2);

                        viewModel.DisplaySlots[i].CenterPoint = new Point(x, y);
                    }
                }
            }

            // Update all connection positions
            viewModel?.UpdateConnectionPositions();
        }

        private T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            if (parent == null) return null;

            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;

                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }

                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (viewModel?.IsConnectionMode == true)
            {
                // In connection mode, don't allow drag from tree
                return;
            }

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
            if (e.LeftButton == MouseButtonState.Pressed && viewModel?.IsConnectionMode == false)
            {
                Point currentPosition = e.GetPosition(null);

                if ((Math.Abs(currentPosition.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(currentPosition.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    var border = sender as Border;
                    var server = border?.DataContext as Server;

                    if (server != null && viewModel != null)
                    {
                        viewModel.DraggedServer = server;
                        viewModel.StartDragFromSlot(-1);
                        Mouse.OverrideCursor = Cursors.Hand;
                        DragDrop.DoDragDrop(border, server, DragDropEffects.Move);
                        Mouse.OverrideCursor = null;
                        viewModel.EndDrag();
                    }
                }
            }
        }

        private void SlotBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;

                if (viewModel.IsConnectionMode)
                {
                    // Handle connection mode
                    viewModel.HandleSlotClick(slotIndex);
                    // Update connection points immediately after creating connection
                    UpdateConnectionPoints();
                }
                else
                {
                    // Handle drag mode
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
        }

        private void SlotBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (viewModel?.IsConnectionMode == true)
            {
                // In connection mode, right-click does nothing
                return;
            }

            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;
                var server = viewModel.GetServerInSlot(slotIndex);

                if (server != null)
                {
                    // Simple right-click to remove from slot
                    viewModel.RemoveServerFromSlot(slotIndex);
                    // Update connections after removal
                    UpdateConnectionPoints();
                }
            }
        }

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            if (viewModel?.IsConnectionMode == true)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

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
            if (viewModel?.IsConnectionMode == true)
            {
                e.Handled = true;
                return;
            }

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

                    // Update connection points after drop
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateConnectionPoints();
                    }), DispatcherPriority.Background);

                    // Clear drag over state
                    viewModel.SetSlotDragOver(targetSlotIndex, false);
                    viewModel.EndDrag();
                }
            }
            e.Handled = true;
        }
    }
}