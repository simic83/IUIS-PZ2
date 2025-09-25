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

    // Helper classes for storing state
    public class DisplayState
    {
        public Dictionary<int, int> SlotConfiguration { get; set; } // SlotIndex -> ServerId
        public List<(int, int)> ConnectionConfiguration { get; set; } // Server1Id, Server2Id pairs

        public DisplayState()
        {
            SlotConfiguration = new Dictionary<int, int>();
            ConnectionConfiguration = new List<(int, int)>();
        }
    }

    // Class for storing drag & drop undo information
    public class DragDropUndoInfo
    {
        public int ServerId { get; set; }
        public int? FromSlotIndex { get; set; } // null if from tree
        public int? ToSlotIndex { get; set; }   // null if removed to tree
        public int? SwappedServerId { get; set; } // if a swap occurred
        public int? SwappedFromSlot { get; set; } // original slot of swapped server
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
            ClearSlotsCommand = new MyICommand(ClearSlotsWithUndo);
            ToggleConnectionModeCommand = new MyICommand(() => IsConnectionMode = !IsConnectionMode);
            ClearConnectionsCommand = new MyICommand(ClearConnectionsWithUndo);
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

        // Method to remove server from a specific slot (with undo support)
        public void RemoveServerFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < DisplaySlots.Count)
            {
                var server = DisplaySlots[slotIndex].Server;
                if (server != null)
                {
                    // Create undo info for removal
                    var undoInfo = new DragDropUndoInfo
                    {
                        ServerId = server.Id,
                        FromSlotIndex = slotIndex,
                        ToSlotIndex = null // removed to tree
                    };

                    DisplaySlots[slotIndex].Server = null;
                    RemoveConnectionsForServer(server.Id);
                    SaveConfiguration();

                    // Add undo action
                    var undoAction = new MyICommand(() => RestoreDragDrop(undoInfo));
                    mainViewModel.AddUndoAction(undoAction);
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

            // Store the server ID to preserve connections
            int serverId = server.Id;

            // Create undo info
            var undoInfo = new DragDropUndoInfo
            {
                ServerId = serverId,
                FromSlotIndex = DraggedFromSlot >= 0 ? (int?)DraggedFromSlot : null,
                ToSlotIndex = slotIndex
            };

            // Check if there's a server at the target slot (for swap)
            var existingServer = DisplaySlots[slotIndex].Server;
            if (existingServer != null && existingServer != server)
            {
                undoInfo.SwappedServerId = existingServer.Id;
                undoInfo.SwappedFromSlot = slotIndex;
            }

            // Track if this server was already placed somewhere
            bool wasAlreadyPlaced = DisplaySlots.Any(s => s.Server == server);

            // First, remove the server from any existing slot to prevent duplicates
            // But DON'T remove connections yet
            foreach (var slot in DisplaySlots)
            {
                if (slot.Server == server)
                {
                    slot.Server = null;
                }
            }

            // Handle swap if needed
            if (existingServer != null && existingServer != server && DraggedFromSlot >= 0)
            {
                // SWAP: place existing server in source slot
                DisplaySlots[DraggedFromSlot].Server = existingServer;
            }

            // Place the server in the target slot
            DisplaySlots[slotIndex].Server = server;

            // Now update connection positions - the server is in its new slot
            // so connections will properly update their positions
            UpdateConnectionPositions();

            // Save the new configuration
            SaveConfiguration();

            // Add undo action
            var undoAction = new MyICommand(() => RestoreDragDrop(undoInfo));
            mainViewModel.AddUndoAction(undoAction);
        }

        // Restore drag & drop operation
        private void RestoreDragDrop(DragDropUndoInfo undoInfo)
        {
            var server = mainViewModel.Servers.FirstOrDefault(s => s.Id == undoInfo.ServerId);
            if (server == null) return;

            // Clear server from current position
            foreach (var slot in DisplaySlots)
            {
                if (slot.Server == server)
                {
                    slot.Server = null;
                }
            }

            // Restore to original position
            if (undoInfo.FromSlotIndex.HasValue)
            {
                // Was in a slot, restore to that slot
                DisplaySlots[undoInfo.FromSlotIndex.Value].Server = server;
            }
            // If FromSlotIndex is null, it means it came from tree, so just leave it cleared

            // Handle swap restoration if needed
            if (undoInfo.SwappedServerId.HasValue && undoInfo.SwappedFromSlot.HasValue)
            {
                var swappedServer = mainViewModel.Servers.FirstOrDefault(s => s.Id == undoInfo.SwappedServerId.Value);
                if (swappedServer != null)
                {
                    // Clear swapped server from wherever it is
                    foreach (var slot in DisplaySlots)
                    {
                        if (slot.Server == swappedServer)
                        {
                            slot.Server = null;
                        }
                    }
                    // Restore swapped server to its original position
                    DisplaySlots[undoInfo.SwappedFromSlot.Value].Server = swappedServer;
                }
            }

            UpdateConnectionPositions();
            SaveConfiguration();
        }

        // Clear Slots with Undo support
        private void ClearSlotsWithUndo()
        {
            // Save current state before clearing
            var previousState = SaveCurrentState();

            // Check if there's actually something to clear
            bool hasContent = DisplaySlots.Any(s => s.Server != null) || Connections.Any();

            if (hasContent)
            {
                // Clear everything
                ClearSlots();

                // Create undo action to restore previous state
                var undoAction = new MyICommand(() => RestoreState(previousState));
                mainViewModel.AddUndoAction(undoAction);
            }
        }

        // Clear Connections with Undo support
        private void ClearConnectionsWithUndo()
        {
            // Save current state before clearing connections
            var previousConnections = Connections.Select(c =>
                (c.Server1Id, c.Server2Id)).ToList();

            if (previousConnections.Any())
            {
                // Clear all connections
                ClearAllConnections();

                // Create undo action to restore connections
                var undoAction = new MyICommand(() => RestoreConnections(previousConnections));
                mainViewModel.AddUndoAction(undoAction);
            }
        }

        // Original clear methods (now private, called by the new methods)
        private void ClearSlots()
        {
            foreach (var slot in DisplaySlots)
            {
                slot.Server = null;
            }
            ClearAllConnections();
            SaveConfiguration();
        }

        // Save current display state
        private DisplayState SaveCurrentState()
        {
            var state = new DisplayState();

            // Save slot configuration
            for (int i = 0; i < DisplaySlots.Count; i++)
            {
                if (DisplaySlots[i].Server != null)
                {
                    state.SlotConfiguration[i] = DisplaySlots[i].Server.Id;
                }
            }

            // Save connection configuration
            foreach (var connection in Connections)
            {
                state.ConnectionConfiguration.Add((connection.Server1Id, connection.Server2Id));
            }

            return state;
        }

        // Restore display state
        private void RestoreState(DisplayState state)
        {
            // Clear current state
            foreach (var slot in DisplaySlots)
            {
                slot.Server = null;
            }
            Connections.Clear();

            // Restore slot configuration
            foreach (var kvp in state.SlotConfiguration)
            {
                var server = mainViewModel.Servers.FirstOrDefault(s => s.Id == kvp.Value);
                if (server != null && kvp.Key < DisplaySlots.Count)
                {
                    DisplaySlots[kvp.Key].Server = server;
                }
            }

            // Restore connections
            foreach (var (server1Id, server2Id) in state.ConnectionConfiguration)
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
                        IsVisible = true
                    };
                    Connections.Add(connection);
                }
            }

            UpdateConnectionPositions();
            SaveConfiguration();
        }

        // Restore only connections
        private void RestoreConnections(List<(int, int)> previousConnections)
        {
            Connections.Clear();

            foreach (var (server1Id, server2Id) in previousConnections)
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
                        IsVisible = true
                    };
                    Connections.Add(connection);
                }
            }

            UpdateConnectionPositions();
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

                if (slot1 != null && slot2 != null && slot1.Server != null && slot2.Server != null)
                {
                    connection.StartPoint = slot1.CenterPoint;
                    connection.EndPoint = slot2.CenterPoint;
                    connection.IsVisible = true;
                }
                else
                {
                    // Don't immediately hide - this could be a temporary state during drag/drop
                    // Only hide if the servers are actually removed from the collection
                    var server1Exists = DisplaySlots.Any(s => s.Server?.Id == connection.Server1Id);
                    var server2Exists = DisplaySlots.Any(s => s.Server?.Id == connection.Server2Id);

                    if (!server1Exists || !server2Exists)
                    {
                        connection.IsVisible = false;
                    }
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