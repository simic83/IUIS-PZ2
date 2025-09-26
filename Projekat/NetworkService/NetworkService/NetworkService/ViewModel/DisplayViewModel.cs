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
                // Zaboravi na stari
                if (server != null)
                {
                    server.PropertyChanged -= OnServerPropertyChanged;
                }

                SetProperty(ref server, value);

                // Subscribe na novi
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

        // Method za računanje centra
        public void UpdateCenterPoint(double slotWidth, double slotHeight, double gridMargin = 10)
        {
            int row = SlotIndex / 4;
            int col = SlotIndex % 4;

            // offset za margine
            double x = gridMargin + (col * slotWidth) + (slotWidth / 2);
            double y = gridMargin + (row * slotHeight) + (slotHeight / 2);

            CenterPoint = new Point(x, y);
        }

        private void OnServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Force UI update kada se server updateuje
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

    // Helper classe
    public class DisplayState
    {
        public Dictionary<int, int> SlotConfiguration { get; set; } // SlotIndex -> ServerId
        public List<(int, int)> ConnectionConfiguration { get; set; } // Server1Id, Server2Id parovi

        public DisplayState()
        {
            SlotConfiguration = new Dictionary<int, int>();
            ConnectionConfiguration = new List<(int, int)>();
        }
    }

    // Classa za čuvanje dnd undo informacija
    public class DragDropUndoInfo
    {
        public int ServerId { get; set; }
        public int? FromSlotIndex { get; set; } // null iz tree
        public int? ToSlotIndex { get; set; }   // null ako vraceno u tree
        public int? SwappedServerId { get; set; } // ako je bio swap
        public int? SwappedFromSlot { get; set; } // početni slot swap servera
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

        // Memorisanje
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

            // Subscribe na promene u server kolekciji
            mainViewModel.Servers.CollectionChanged += (s, e) =>
            {
                RefreshGroupedServers();
                // Skloni izbrisane servere sa slotova
                if (e.OldItems != null)
                {
                    foreach (Server server in e.OldItems)
                    {
                        RemoveServerFromAllSlots(server);
                        RemoveConnectionsForServer(server.Id);
                    }
                }
                // Restore configuration za nove servere ako su prethodno postavljeni
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

        // Method za praćenje drag početka
        public void StartDragFromSlot(int slotIndex)
        {
            DraggedFromSlot = slotIndex;
            IsDragging = true;
        }

        public void EndDrag()
        {
            IsDragging = false;
            DraggedFromSlot = -1;
            // Čišćenje svih drag over-a
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

        // Method za postavljanje servera u poseban slot
        public Server GetServerInSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < DisplaySlots.Count)
            {
                return DisplaySlots[slotIndex].Server;
            }
            return null;
        }

        // Method za sklanjanje servera
        public void RemoveServerFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < DisplaySlots.Count)
            {
                var server = DisplaySlots[slotIndex].Server;
                if (server != null)
                {
                    var undoInfo = new DragDropUndoInfo
                    {
                        ServerId = server.Id,
                        FromSlotIndex = slotIndex,
                        ToSlotIndex = null // vrati u drvo
                    };

                    DisplaySlots[slotIndex].Server = null;
                    RemoveConnectionsForServer(server.Id);
                    SaveConfiguration();

                    var undoAction = new MyICommand(() => RestoreDragDrop(undoInfo));
                    mainViewModel.AddUndoAction(undoAction);
                }
            }
        }

        // Popravka duplikata
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

        // Očisti slotove
        private void ClearSlotsWithUndo()
        {
            var previousState = SaveCurrentState();

            // Mala provera da li uopšte ima nešto
            bool hasContent = DisplaySlots.Any(s => s.Server != null) || Connections.Any();

            if (hasContent)
            {
                ClearSlots();

                var undoAction = new MyICommand(() => RestoreState(previousState));
                mainViewModel.AddUndoAction(undoAction);
            }
        }

        // Clear Connections i Undo
        private void ClearConnectionsWithUndo()
        {
            var previousConnections = Connections.Select(c =>
                (c.Server1Id, c.Server2Id)).ToList();

            if (previousConnections.Any())
            {
                ClearAllConnections();

                var undoAction = new MyICommand(() => RestoreConnections(previousConnections));
                mainViewModel.AddUndoAction(undoAction);
            }
        }

        private void ClearSlots()
        {
            foreach (var slot in DisplaySlots)
            {
                slot.Server = null;
            }
            ClearAllConnections();
            SaveConfiguration();
        }

        // Display
        private DisplayState SaveCurrentState()
        {
            var state = new DisplayState();

            // Čuvaj slotove
            for (int i = 0; i < DisplaySlots.Count; i++)
            {
                if (DisplaySlots[i].Server != null)
                {
                    state.SlotConfiguration[i] = DisplaySlots[i].Server.Id;
                }
            }

            // Čuvaj konekciju
            foreach (var connection in Connections)
            {
                state.ConnectionConfiguration.Add((connection.Server1Id, connection.Server2Id));
            }

            return state;
        }

        // Vrati display stanje
        private void RestoreState(DisplayState state)
        {
            // Čišćenje trenutnog stanja
            foreach (var slot in DisplaySlots)
            {
                slot.Server = null;
            }
            Connections.Clear();

            // Vrati stanje slotova
            foreach (var kvp in state.SlotConfiguration)
            {
                var server = mainViewModel.Servers.FirstOrDefault(s => s.Id == kvp.Value);
                if (server != null && kvp.Key < DisplaySlots.Count)
                {
                    DisplaySlots[kvp.Key].Server = server;
                }
            }

            // Vrati konekcije
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

        // Vrati konekcije
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

        // Početak konekcije
        public void HandleSlotClick(int slotIndex)
        {
            if (!IsConnectionMode) return;

            var server = GetServerInSlot(slotIndex);
            if (server == null) return;

            if (connectionStartServer == null)
            {
                connectionStartServer = server;
                connectionStartSlot = slotIndex;
            }
            else
            {
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
                    // Ne sakriva konekciju odma
                    var server1Exists = DisplaySlots.Any(s => s.Server?.Id == connection.Server1Id);
                    var server2Exists = DisplaySlots.Any(s => s.Server?.Id == connection.Server2Id);

                    if (!server1Exists || !server2Exists)
                    {
                        connection.IsVisible = false;
                    }
                }
            }
        }

        // Čuvanje
        private void SaveConfiguration()
        {
            // Slotovi
            slotConfiguration.Clear();
            for (int i = 0; i < DisplaySlots.Count; i++)
            {
                if (DisplaySlots[i].Server != null)
                {
                    slotConfiguration[i] = DisplaySlots[i].Server.Id;
                }
            }

            // Konekcije
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

            // Restore connections - ali prvo sačekaj na račun UI
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
                        IsVisible = false // Prvo su ne vidljive
                    };
                    Connections.Add(connection);
                }
            }
        }
    }
}