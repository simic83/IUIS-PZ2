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

        public Server DraggedServer
        {
            get { return draggedServer; }
            set { SetProperty(ref draggedServer, value); }
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

        private void AutoArrange()
        {
            int slotIndex = 0;
            foreach (var server in mainViewModel.Servers)
            {
                if (slotIndex < 12)
                {
                    DisplaySlots[slotIndex].Server = server;
                    slotIndex++;
                }
            }
        }

        public void PlaceServerInSlot(Server server, int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 12)
            {
                DisplaySlots[slotIndex].Server = server;
            }
        }

        public void RemoveServerFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 12)
            {
                DisplaySlots[slotIndex].Server = null;
            }
        }

        public Server GetServerInSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 12)
            {
                return DisplaySlots[slotIndex].Server;
            }
            return null;
        }
    }
}