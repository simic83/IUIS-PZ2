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

            // Tajmer za osvežavanje poveznih linija, zbog promene pozicija
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Mali delay za učitavanje
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateConnectionPoints();
                // Kada se učita, pusti vidljivost konekcija
                if (viewModel != null)
                {
                    viewModel.UpdateConnectionPositions();
                }
            }), DispatcherPriority.Loaded);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // U slučaju prozora
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

        // Items panel
        private void UpdateConnectionPoints()
        {
            if (viewModel == null || SlotsItemsControl == null) return;

            // Veličina grida
            var uniformGrid = GetVisualChild<UniformGrid>(SlotsItemsControl);
            if (uniformGrid == null || uniformGrid.ActualWidth == 0 || uniformGrid.ActualHeight == 0)
                return;

            // Dimenzije kartica
            double totalWidth = uniformGrid.ActualWidth;
            double totalHeight = uniformGrid.ActualHeight;
            double slotWidth = totalWidth / 4; // 4 kolone
            double slotHeight = totalHeight / 3; // 3 reda

            // Update all slot center points
            for (int i = 0; i < viewModel.DisplaySlots.Count; i++)
            {
                var container = SlotsItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container != null)
                {
                    // Pozicije u odnosu na MainDisplayGrid
                    try
                    {
                        var transform = container.TransformToAncestor(MainDisplayGrid);
                        var topLeft = transform.Transform(new Point(0, 0));

                        // Center point
                        var centerX = topLeft.X + (container.ActualWidth / 2);
                        var centerY = topLeft.Y + (container.ActualHeight / 2);

                        viewModel.DisplaySlots[i].CenterPoint = new Point(centerX, centerY);
                    }
                    catch
                    {
                        // Drugi način
                        int row = i / 4;
                        int col = i % 4;

                        double x = (col * slotWidth) + (slotWidth / 2);
                        double y = (row * slotHeight) + (slotHeight / 2);

                        viewModel.DisplaySlots[i].CenterPoint = new Point(x, y);
                    }
                }
            }

            // Update pozicija
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


        // Početak
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (viewModel?.IsConnectionMode == true)
            {
                return;
            }

            startPoint = e.GetPosition(null);
            var border = sender as Border;
            var server = border?.DataContext as Server;

            if (server != null && viewModel != null)
            {
                viewModel.DraggedServer = server;
                viewModel.StartDragFromSlot(-1); // Dragging iz drveta

                // Promena kursora
                Mouse.OverrideCursor = Cursors.Hand;

                DragDrop.DoDragDrop(border, server, DragDropEffects.Move);

                // Reset kursora posle drag i dropa
                Mouse.OverrideCursor = null;
                viewModel.EndDrag();
            }
        }

        // Pomeraj
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

        // Početak konekcije
        private void SlotBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;

                if (viewModel.IsConnectionMode)
                {
                    // Connection mode
                    viewModel.HandleSlotClick(slotIndex);
                    // Update odma posle konektovanja
                    UpdateConnectionPoints();
                }
                else
                {
                    // Drag mode
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
                return;
            }

            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;
                var server = viewModel.GetServerInSlot(slotIndex);

                if (server != null)
                {
                    // Desni klik cisti slot
                    viewModel.RemoveServerFromSlot(slotIndex);
                    UpdateConnectionPoints();
                }
            }
        }

        // Prebacivanje
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

                // Drag završen
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

        // Van bordera
        private void Canvas_DragLeave(object sender, DragEventArgs e)
        {
            var border = sender as Border;
            if (border != null && viewModel != null)
            {
                int slotIndex = (int)border.Tag;
                viewModel.SetSlotDragOver(slotIndex, false);
            }
        }

        // Uspešan drop
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
                        // Premeštanje sa drugog slota
                        if (existingServer != null && existingServer != server)
                        {
                            // SWAP: prebaci ono što je bilo na ciljnom u izvorni slot
                            viewModel.PlaceServerInSlot(existingServer, viewModel.DraggedFromSlot);
                        }
                    }


                    // Spusti prevučeni server u ciljni slot
                    viewModel.PlaceServerInSlot(server, targetSlotIndex);


                    // Osvježi tačke konekcija nakon drop-a
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateConnectionPoints();
                    }), DispatcherPriority.Background);


                    // Očisti vizuelno stanje
                    viewModel.SetSlotDragOver(targetSlotIndex, false);
                    viewModel.EndDrag();
                }
            }
            e.Handled = true;
        }
    }
}