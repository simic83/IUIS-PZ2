using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NetworkService.Common;
using NetworkService.Model;
using NetworkService.ViewModel.Commands;

namespace NetworkService.ViewModel
{
    public class ServerSlot : BindableBase
    {
        private Server server;
        private int slotIndex;

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
        private Server draggedServer;
        private Server _draggedServer;
        public Server DraggedServer
        {
            get => _draggedServer;
            set => SetProperty(ref _draggedServer, value);
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

        private int _draggedFromSlot = -1;
        public int DraggedFromSlot
        {
            get => _draggedFromSlot;
            private set => SetProperty(ref _draggedFromSlot, value);
        }

        // Method to track where the drag started from
        public void StartDragFromSlot(int slotIndex)
        {
            DraggedFromSlot = slotIndex;
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

        public ICommand AutoArrangeCommand { get; set; }

        public DisplayViewModel(MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            InitializeSlots();
            InitializeCommands();
            RefreshGroupedServers();

            mainViewModel.Servers.CollectionChanged += (s, e) => RefreshGroupedServers();
        }

        private void InitializeSlots()
        {
            DisplaySlots = new ObservableCollection<ServerSlot>();
            for (int i = 0; i < 12; i++)
            {
                DisplaySlots.Add(new ServerSlot { SlotIndex = i });
            }
        }

        private void InitializeCommands()
        {
            AutoArrangeCommand = new MyICommand(AutoArrange);
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

        // Method to remove server from a specific slot
        public void RemoveServerFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < DisplaySlots.Count)
            {
                DisplaySlots[slotIndex].Server = null;
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
        }

        public void PlaceServerInSlot(Server server, int slotIndex)
        {
            if (server == null || slotIndex < 0 || slotIndex >= DisplaySlots.Count)
                return;

            // First, remove the server from any existing slot to prevent duplicates
            RemoveServerFromAllSlots(server);

            // Place the server in the target slot
            DisplaySlots[slotIndex].Server = server;
        }

        // Updated Auto Arrange Command (if you have this)
        private void AutoArrange()
        {
            // Clear all slots first
            foreach (var slot in DisplaySlots)
            {
                slot.Server = null;
            }

            // Place servers in order
            int slotIndex = 0;
            foreach (var group in GroupedServers)
            {
                foreach (var server in group.Servers)
                {
                    if (slotIndex < DisplaySlots.Count)
                    {
                        DisplaySlots[slotIndex].Server = server;
                        slotIndex++;
                    }
                }
            }
        }

        // Clear Slots Command implementation
        private void ClearSlots()
        {
            foreach (var slot in DisplaySlots)
            {
                slot.Server = null;
            }
        }

    }
}