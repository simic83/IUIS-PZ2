using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NetworkService.Common;
using NetworkService.Model;
using NetworkService.ViewModel.Commands;

namespace NetworkService.ViewModel
{
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
        private Server[] displaySlots = new Server[12];
        private Server draggedServer;

        public ObservableCollection<ServerGroup> GroupedServers
        {
            get { return groupedServers; }
            set { SetProperty(ref groupedServers, value); }
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
            InitializeCommands();
            RefreshGroupedServers();

            mainViewModel.Servers.CollectionChanged += (s, e) => RefreshGroupedServers();
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
                    displaySlots[slotIndex] = server;
                    slotIndex++;
                }
            }

            OnPropertyChanged("DisplaySlots");
        }

        public void PlaceServerInSlot(Server server, int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 12)
            {
                displaySlots[slotIndex] = server;
                OnPropertyChanged("DisplaySlots");
            }
        }

        public void RemoveServerFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 12)
            {
                displaySlots[slotIndex] = null;
                OnPropertyChanged("DisplaySlots");
            }
        }

        public Server GetServerInSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 12)
            {
                return displaySlots[slotIndex];
            }
            return null;
        }
    }
}