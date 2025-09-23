using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NetworkService.Common;
using NetworkService.Model;
using NetworkService.ViewModel.Commands;
using System.Collections.Generic;

namespace NetworkService.ViewModel
{
    public class ServerConnection : BindableBase
    {
        private int server1Id;
        private int server2Id;
        private Point startPoint;
        private Point endPoint;
        private bool isVisible;

        public int Server1Id
        {
            get => server1Id;
            set => SetProperty(ref server1Id, value);
        }

        public int Server2Id
        {
            get => server2Id;
            set => SetProperty(ref server2Id, value);
        }

        public Point StartPoint
        {
            get => startPoint;
            set => SetProperty(ref startPoint, value);
        }

        public Point EndPoint
        {
            get => endPoint;
            set => SetProperty(ref endPoint, value);
        }

        public bool IsVisible
        {
            get => isVisible;
            set => SetProperty(ref isVisible, value);
        }
    }

    public class ServerSlot : BindableBase
    {
        private Server server;
        private int slotIndex;
        private bool isDragOver;
        private Point centerPoint;

        public Server Server
        {
            get { return server; }
            set
            {
                // Unsubscribe from old server
                if (server != null)
                {
                    server.PropertyChanged -= OnServerPropertyChanged;
                }

                SetProperty(ref server, value);

                // Subscribe to new server
                if (server != null)
                {
                    server.PropertyChanged += OnServerPropertyChanged;
                }
            }
        }

        public int SlotIndex
        {
            get { return slotIndex; }
            set { SetProperty(ref slotIndex, value); }
        }

        public bool IsDragOver
        {
            get { return isDragOver; }
            set { SetProperty(ref isDragOver, value); }
        }

        public Point CenterPoint
        {
            get => centerPoint;
            set => SetProperty(ref centerPoint, value);
        }

        // Method to calculate center based on grid position
        public void UpdateCenterPoint(double slotWidth, double slotHeight, double gridMargin = 10)
        {
            int row = SlotIndex / 4;
            int col = SlotIndex % 4;

            // Calculate center point including margins
            double x = gridMargin + (col * slotWidth) + (slotWidth / 2);
            double y = gridMargin + (row * slotHeight) + (slotHeight / 2);

            CenterPoint = new Point(x, y);
        }

        private void OnServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Force UI update when server properties change
            OnPropertyChanged(nameof(Server));
        }
    }

    public class ServerGroup
    {
        public string TypeName { get; set; }
        public ObservableCollection<Server> Servers { get; set; }

        public ServerGroup()
        {
            Servers = new ObservableCollection<Server>();
        }
    }

    public class DisplayViewModel : BindableBase
    {
        private MainWindowViewModel mainViewModel;
        private ObservableCollection<ServerGroup> groupedServers;
        private ObservableCollection<ServerSlot> displaySlots;
        private ObservableCollection<ServerConnection> connections;
        private Server _draggedServer;
        private bool _isDragging;
        private bool isConnectionMode;
        private Server connectionStartServer;
        private int connectionStartSlot = -1;

        // Persistence storage
        private static Dictionary<int, int> slotConfiguration = new Dictionary<int, int>(); // SlotIndex -> ServerId
        private static List<(int, int)> connectionConfiguration = new List<(int, int)>(); // Server1Id, Server2Id pairs

        public Server DraggedServer
        {
            get => _draggedServer;
            set => SetProperty(ref _draggedServer, value);
        }

        public bool IsDragging
        {
            get => _isDragging;
            set => SetProperty(ref _isDragging, value);
        }

        public bool IsConnectionMode
        {
            get => isConnectionMode;
            set
            {
                SetProperty(ref isConnectionMode, value);
                if (!value)
                {
                    connectionStartServer = null;
                    connectionStartSlot = -1;
                }
            }
        }

        public ObservableCollection<ServerGroup> GroupedServers
        {
            get { return groupedServers; }
            set { SetProperty(ref groupedServers, value); }
        }

        public ObservableCollection<ServerSlot> DisplaySlots
        {
            get { return displaySlots; }
            set { SetProperty(ref displaySlots, value); }
        }

        public ObservableCollection<ServerConnection> Connections
        {
            get => connections;
            set => SetProperty(ref connections, value);
        }

        private int _draggedFromSlot = -1;
        public int DraggedFromSlot
        {
            get => _draggedFromSlot;
            private set => SetProperty(ref _draggedFromSlot, value);
        }

        public ICommand ClearSlotsCommand { get; set; }
        public ICommand RemoveFromSlotCommand { get; set; }
        public ICommand ToggleConnectionModeCommand { get; set; }
        public ICommand ClearConnectionsCommand { get; set; }

        public DisplayViewModel(MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            Connections = new ObservableCollection<ServerConnection>();
            InitializeSlots();
            InitializeCommands();
            RefreshGroupedServers();
            RestoreConfiguration();

            // Subscribe to collection changes
            mainViewModel.Servers.CollectionChanged += (s, e) =>
            {
                RefreshGroupedServers();
                // Remove deleted servers from slots
                if (e.OldItems != null)
                {
                    foreach (Server server in e.OldItems)
                    {
                        RemoveServerFromAllSlots(server);
                        RemoveConnectionsForServer(server.Id);
                    }
                }
                // Restore configuration for new servers if they were previously placed
                if (e.NewItems != null)
                {
                    RestoreConfiguration();
                }
            };
        }

        private void InitializeSlots()
        {
            DisplaySlots = new ObservableCollection<ServerSlot>();
            for (int i = 0; i < 12; i++)
            {
                var slot = new ServerSlot { SlotIndex = i };
                DisplaySlots.Add(slot);
            }
        }

        // Public method to update center points from actual UI measurements
        public void UpdateSlotCenterPoints(double slotWidth, double slotHeight, double gridMargin = 10)
        {
            foreach (var slot in DisplaySlots)
            {
                slot.UpdateCenterPoint(slotWidth, slotHeight, gridMargin);
            }
            UpdateConnectionPositions();
        }

        private void InitializeCommands()
        {
            ClearSlotsCommand = new MyICommand(ClearSlots);
            ToggleConnectionModeCommand = new MyICommand(() => IsConnectionMode = !IsConnectionMode);
            ClearConnectionsCommand = new MyICommand(ClearAllConnections);
        }

        private void RefreshGroupedServers()
        {
            var groups = mainViewModel.Servers
                .GroupBy(s => s.Type.Name)
                .Select(g => new ServerGroup
                {
                    TypeName = g.Key,
                    Servers = new ObservableCollection<Server>(g)
                });

            GroupedServers = new ObservableCollection<ServerGroup>(groups);
        }

        // Method to track where the drag started from
        public void StartDragFromSlot(int slotIndex)
        {
            DraggedFromSlot = slotIndex;
            IsDragging = true;
        }

        public void EndDrag()
        {
            IsDragging = false;
            DraggedFromSlot = -1;
            // Clear all drag over states
            foreach (var slot in DisplaySlots)
            {
                slot.IsDragOver = false;
            }
        }

        public void SetSlotDragOver(int slotIndex, bool isDragOver)
        {
            if (slotIndex >= 0 && slotIndex < DisplaySlots.Count)
            {
                DisplaySlots[slotIndex].IsDragOver = isDragOver;
            }
        }

        // Method to get server in a specific slot
        public Server GetServerInSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < DisplaySlots.Count)
            {
                return DisplaySlots[slotIndex].Server;
            }
            return null;
        }

        // Method to remove server from a specific slot
        public void RemoveServerFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < DisplaySlots.Count)
            {
                var server = DisplaySlots[slotIndex].Server;
                if (server != null)
                {
                    DisplaySlots[slotIndex].Server = null;
                    RemoveConnectionsForServer(server.Id);
                    SaveConfiguration();
                }
            }
        }

        // Helper method to remove a server from all slots (prevents duplicates)
        private void RemoveServerFromAllSlots(Server server)
        {
            foreach (var slot in DisplaySlots)
            {
                if (slot.Server == server)
                {
                    slot.Server = null;
                }
            }
            SaveConfiguration();
        }

        public void PlaceServerInSlot(Server server, int slotIndex)
        {
            if (server == null || slotIndex < 0 || slotIndex >= DisplaySlots.Count)
                return;

            // First, remove the server from any existing slot to prevent duplicates
            RemoveServerFromAllSlots(server);

            // Place the server in the target slot
            DisplaySlots[slotIndex].Server = server;

            // Update connections to reflect new position
            UpdateConnectionPositions();
            SaveConfiguration();
        }

        // Clear Slots Command implementation
        private void ClearSlots()
        {
            foreach (var slot in DisplaySlots)
            {
                slot.Server = null;
            }
            ClearAllConnections();
            SaveConfiguration();
        }

        // Connection management
        public void HandleSlotClick(int slotIndex)
        {
            if (!IsConnectionMode) return;

            var server = GetServerInSlot(slotIndex);
            if (server == null) return;

            if (connectionStartServer == null)
            {
                // Start a new connection
                connectionStartServer = server;
                connectionStartSlot = slotIndex;
            }
            else
            {
                // Complete the connection
                if (server != connectionStartServer && !ConnectionExists(connectionStartServer.Id, server.Id))
                {
                    CreateConnection(connectionStartServer.Id, server.Id, connectionStartSlot, slotIndex);
                }
                connectionStartServer = null;
                connectionStartSlot = -1;
            }
        }

        private bool ConnectionExists(int server1Id, int server2Id)
        {
            return Connections.Any(c =>
                (c.Server1Id == server1Id && c.Server2Id == server2Id) ||
                (c.Server1Id == server2Id && c.Server2Id == server1Id));
        }

        private void CreateConnection(int server1Id, int server2Id, int slot1Index, int slot2Index)
        {
            var connection = new ServerConnection
            {
                Server1Id = server1Id,
                Server2Id = server2Id,
                StartPoint = DisplaySlots[slot1Index].CenterPoint,
                EndPoint = DisplaySlots[slot2Index].CenterPoint,
                IsVisible = true
            };
            Connections.Add(connection);
            SaveConfiguration();
        }

        private void RemoveConnectionsForServer(int serverId)
        {
            var toRemove = Connections.Where(c => c.Server1Id == serverId || c.Server2Id == serverId).ToList();
            foreach (var connection in toRemove)
            {
                Connections.Remove(connection);
            }
            SaveConfiguration();
        }

        private void ClearAllConnections()
        {
            Connections.Clear();
            SaveConfiguration();
        }

        public void UpdateConnectionPositions()
        {
            foreach (var connection in Connections)
            {
                var slot1 = DisplaySlots.FirstOrDefault(s => s.Server?.Id == connection.Server1Id);
                var slot2 = DisplaySlots.FirstOrDefault(s => s.Server?.Id == connection.Server2Id);

                if (slot1 != null && slot2 != null)
                {
                    connection.StartPoint = slot1.CenterPoint;
                    connection.EndPoint = slot2.CenterPoint;
                    connection.IsVisible = true;
                }
                else
                {
                    connection.IsVisible = false;
                }
            }
        }

        // Persistence methods
        private void SaveConfiguration()
        {
            // Save slot configuration
            slotConfiguration.Clear();
            for (int i = 0; i < DisplaySlots.Count; i++)
            {
                if (DisplaySlots[i].Server != null)
                {
                    slotConfiguration[i] = DisplaySlots[i].Server.Id;
                }
            }

            // Save connection configuration
            connectionConfiguration.Clear();
            foreach (var connection in Connections)
            {
                connectionConfiguration.Add((connection.Server1Id, connection.Server2Id));
            }
        }

        private void RestoreConfiguration()
        {
            // Restore slot configuration
            foreach (var kvp in slotConfiguration)
            {
                var server = mainViewModel.Servers.FirstOrDefault(s => s.Id == kvp.Value);
                if (server != null && kvp.Key < DisplaySlots.Count)
                {
                    DisplaySlots[kvp.Key].Server = server;
                }
            }

            // Restore connections - but don't set positions yet, wait for UI to calculate
            Connections.Clear();
            foreach (var (server1Id, server2Id) in connectionConfiguration)
            {
                var slot1 = DisplaySlots.FirstOrDefault(s => s.Server?.Id == server1Id);
                var slot2 = DisplaySlots.FirstOrDefault(s => s.Server?.Id == server2Id);

                if (slot1 != null && slot2 != null)
                {
                    var connection = new ServerConnection
                    {
                        Server1Id = server1Id,
                        Server2Id = server2Id,
                        StartPoint = slot1.CenterPoint,
                        EndPoint = slot2.CenterPoint,
                        IsVisible = false // Start invisible until positions are calculated
                    };
                    Connections.Add(connection);
                }
            }
        }
    }
}