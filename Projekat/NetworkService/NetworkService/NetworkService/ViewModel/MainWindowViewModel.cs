using NetworkService.Common;
using NetworkService.Model;
using NetworkService.Services;
using NetworkService.ViewModel.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : BindableBase
    {
        private ObservableCollection<Server> servers;
        private ObservableCollection<string> terminalOutput;
        private ObservableCollection<string> commandHistory;
        private ICommand lastUndoAction;
        private string currentCommand;
        private int historyIndex;
        private BindableBase currentViewModel;
        private EntitiesViewModel entitiesViewModel;
        private DisplayViewModel displayViewModel;
        private GraphViewModel graphViewModel;
        private string statusMessage;
        private Thread listeningThread;
        private bool isListening;
        


        private readonly string measurementLogPath = "measurements.txt";

        public ICommand ToggleConnectionModeCommand { get; set; }
        public ICommand ClearConnectionsCommand { get; set; }
        public ICommand ClearAllSlotsCommand { get; set; }

        public ObservableCollection<Server> Servers
        {
            get { return servers; }
            set { SetProperty(ref servers, value); }
        }

        public ObservableCollection<string> TerminalOutput
        {
            get { return terminalOutput; }
            set { SetProperty(ref terminalOutput, value); }
        }

        public ObservableCollection<string> CommandHistory
        {
            get { return commandHistory; }
            set { SetProperty(ref commandHistory, value); }
        }

        public string CurrentCommand
        {
            get { return currentCommand; }
            set { SetProperty(ref currentCommand, value); }
        }

        public BindableBase CurrentViewModel
        {
            get { return currentViewModel; }
            set { SetProperty(ref currentViewModel, value); }
        }

        public string StatusMessage
        {
            get { return statusMessage; }
            set { SetProperty(ref statusMessage, value); }
        }

        // Commands
        public ICommand SwitchToEntitiesCommand { get; set; }
        public ICommand SwitchToDisplayCommand { get; set; }
        public ICommand SwitchToGraphCommand { get; set; }
        public ICommand ExecuteTerminalCommand { get; set; }
        public ICommand NavigateHistoryUpCommand { get; set; }
        public ICommand NavigateHistoryDownCommand { get; set; }
        public ICommand UndoCommand { get; set; }
        public ICommand NextTabCommand { get; set; }
        public ICommand FocusTerminalCommand { get; set; }

        public MainWindowViewModel()
        {
            InitializeData();
            InitializeCommands();
            InitializeViewModels();
            CreateListener();

            StatusMessage = "System Ready";
            AddTerminalOutput("NetworkService Monitor initialized");
            AddTerminalOutput("Type 'help' for available commands");
        }

        private void InitializeData()
        {
            Servers = new ObservableCollection<Server>();
            TerminalOutput = new ObservableCollection<string>();
            CommandHistory = new ObservableCollection<string>();

            // Mock data
            var webServerType = new ServerType("Web", "/Resources/Images/web_server.png");
            var dbServerType = new ServerType("Database", "/Resources/Images/database_server.png");
            var fileServerType = new ServerType("File", "/Resources/Images/file_server.png");

            // Server 1 - Web Server
            var server1 = new Server
            {
                Id = 1,
                Name = "Production Web Server",
                IPAddress = "192.168.1.10",
                Type = webServerType,
                LastMeasurement = 65.5, // Valid  (45-75)
                LastUpdate = DateTime.Now,
                Status = "online"
            };

            // Server 2 - Database Server
            var server2 = new Server
            {
                Id = 2,
                Name = "Primary Database",
                IPAddress = "192.168.1.20",
                Type = dbServerType,
                LastMeasurement = 82.3, // Warning -  75
                LastUpdate = DateTime.Now.AddMinutes(-5),
                Status = "warning"
            };

            // Server 3 - File Server
            var server3 = new Server
            {
                Id = 3,
                Name = "Backup File Storage",
                IPAddress = "192.168.1.30",
                Type = fileServerType,
                LastMeasurement = 48.7, // Valid 
                LastUpdate = DateTime.Now.AddMinutes(-2),
                Status = "online"
            };

            Servers.Add(server1);
            Servers.Add(server2);
            Servers.Add(server3);
        }

        private void InitializeCommands()
        {
            SwitchToEntitiesCommand = new MyICommand(() => SwitchView(entitiesViewModel));
            SwitchToDisplayCommand = new MyICommand(() => SwitchView(displayViewModel));
            SwitchToGraphCommand = new MyICommand(() => SwitchView(graphViewModel));
            ExecuteTerminalCommand = new MyICommand(ExecuteTerminal);
            NavigateHistoryUpCommand = new MyICommand(NavigateHistoryUp);
            NavigateHistoryDownCommand = new MyICommand(NavigateHistoryDown);
            UndoCommand = new MyICommand(ExecuteUndo, () => lastUndoAction != null);
            NextTabCommand = new MyICommand(NextTab);
            FocusTerminalCommand = new MyICommand(FocusTerminal);
            ToggleConnectionModeCommand = new MyICommand(() =>
            {
                if (CurrentViewModel == displayViewModel)
                {
                    displayViewModel.IsConnectionMode = !displayViewModel.IsConnectionMode;
                    AddTerminalOutput($"Connection Mode: {(displayViewModel.IsConnectionMode ? "ON" : "OFF")}");
                }
            });

            ClearConnectionsCommand = new MyICommand(() =>
            {
                if (CurrentViewModel == displayViewModel)
                {
                    displayViewModel.ClearConnectionsCommand.Execute(null);
                    AddTerminalOutput("Connections cleared");
                }
            });

            ClearAllSlotsCommand = new MyICommand(() =>
            {
                if (CurrentViewModel == displayViewModel)
                {
                    displayViewModel.ClearSlotsCommand.Execute(null);
                    AddTerminalOutput("All slots cleared");
                }
            });
        }

        private void InitializeViewModels()
        {
            entitiesViewModel = new EntitiesViewModel(this);
            displayViewModel = new DisplayViewModel(this);
            graphViewModel = new GraphViewModel(this);

            CurrentViewModel = entitiesViewModel;
        }

        private void SwitchView(BindableBase viewModel)
        {
            if (CurrentViewModel != null && CurrentViewModel != viewModel)
            {
                navigationHistory.Push(CurrentViewModel);
            }

            CurrentViewModel = viewModel;
            string viewName = viewModel.GetType().Name.Replace("ViewModel", "");
            AddTerminalOutput($"→ Switched to {viewName} view");

            // Za svaki slucaj
            pendingRemovalServer = null;
        }

        private void NextTab()
        {
            if (CurrentViewModel == entitiesViewModel)
            {
                SwitchView(displayViewModel);
            }
            else if (CurrentViewModel == displayViewModel)
            {
                SwitchView(graphViewModel);
            }
            else if (CurrentViewModel == graphViewModel)
            {
                SwitchView(entitiesViewModel);
            }
        }

        private void FocusTerminal()
        {
            // Pozovi fokusiranje
            App.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = App.Current.MainWindow as MainWindow;
                mainWindow?.FocusTerminalInput();
            });

            // Tekst feedback
            if (!string.IsNullOrWhiteSpace(CurrentCommand))
            {
                // Ako postoji neki tekst..
                AddTerminalOutput("Terminal focused - continue typing...");
            }
            else
            {
                AddTerminalOutput("Terminal focused - ready for input");
            }
        }

        private void ExecuteTerminal()
        {
            if (string.IsNullOrWhiteSpace(CurrentCommand))
                return;

            CommandHistory.Add(CurrentCommand);
            historyIndex = CommandHistory.Count;

            AddTerminalOutput($"$ {CurrentCommand}");

            ProcessTerminalCommand(CurrentCommand);

            CurrentCommand = string.Empty;
        }

        private void ProcessTerminalCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            // Logika za Y/N
            if (awaitingConfirmation)
            {
                HandleConfirmation(command);
                return;
            }

            string[] parts = command.ToLower().Split(' ');

            switch (parts[0])
            {
                case "help":
                    ShowHelp();
                    break;

                // Entity Management (T6 - Servers)
                case "add":
                    if (parts.Length >= 5)
                        AddServerCommand(parts);
                    else
                        AddTerminalOutput("Usage: add <id> <name> <type> <ip>");
                    break;

                case "remove":
                    if (parts.Length > 1)
                        RemoveServerCommand(parts[1]);
                    else
                        AddTerminalOutput("Usage: remove <id>");
                    break;

                case "list":
                    ListServers();
                    break;

                // Search i Filter (P2)
                case "search":
                    if (parts.Length >= 3)
                        SearchServers(parts[1], string.Join(" ", parts.Skip(2)));
                    else
                        AddTerminalOutput("Usage: search <name|type|id> <value>");
                    break;

                case "filter":
                    if (parts.Length >= 2 && parts[1] == "reset")
                    {
                        ResetFilters();
                    }
                    else if (parts.Length >= 3)
                    {
                        ApplyFilter(parts);
                    }
                    else
                    {
                        AddTerminalOutput("Usage: filter <type|id> <operator> <value>");
                        AddTerminalOutput("   or: filter reset");
                    }
                    break;

                // Navigation
                case "navigate":
                    if (parts.Length > 1)
                        NavigateToTab(parts[1]);
                    else
                    {
                        AddTerminalOutput("Usage: navigate <tab_number>");
                        AddTerminalOutput("  1 = Entities, 2 = Display, 3 = Graph");
                    }
                    break;

                case "back":
                    NavigateBack();
                    break;

                // Actions
                case "undo":
                    if (lastUndoAction != null)
                        ExecuteUndo();
                    else
                        AddTerminalOutput("No action to undo");
                    break;

                case "ping":
                    if (parts.Length > 1)
                        PingServer(parts[1]);
                    else
                        AddTerminalOutput("Usage: ping <server_id>");
                    break;

                case "clear":
                    TerminalOutput.Clear();
                    break;

                default:
                    AddTerminalOutput($"Unknown command: {parts[0]}");
                    AddTerminalOutput("Type 'help' for available commands");
                    break;
            }
        }


        private void HandleConfirmation(string response)
        {
            string cleanResponse = response.Trim().ToUpper();

            if (cleanResponse == "Y" || cleanResponse == "YES")
            {
                if (pendingRemovalServer != null)
                {
                    var server = pendingRemovalServer;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        RemoveServer(server);
                        AddTerminalOutput($"✓ Server '{server.Name}' (ID: {server.Id:000}) removed successfully");

                        // Add to undo stack
                        var undoAction = new MyICommand(() => AddServer(server));
                        AddUndoAction(undoAction);
                    });
                }
            }
            else if (cleanResponse == "N" || cleanResponse == "NO")
            {
                AddTerminalOutput("Operation cancelled");
            }
            else
            {
                AddTerminalOutput("Please answer Y (yes) or N (no)");
                return;
            }

            // Reset 
            pendingRemovalServer = null;
            awaitingConfirmation = false;
        }

        private void ShowHelp()
        {
            AddTerminalOutput("╔════════════════════════════════════════════════════════╗");
            AddTerminalOutput("NetworkService Terminal - Available Commands");
            AddTerminalOutput("╚════════════════════════════════════════════════════════╝");
            AddTerminalOutput("");
            AddTerminalOutput("ENTITY MANAGEMENT (T6 - Servers):");
            AddTerminalOutput("  add <id> <name> <type> <ip>  - Add new server");
            AddTerminalOutput("                                  Types: Web, Database, File");
            AddTerminalOutput("  remove <id>                   - Remove server by ID (asks Y/N)");
            AddTerminalOutput("  list                          - List all servers");
            AddTerminalOutput("");
            AddTerminalOutput("SEARCH & FILTER:");
            AddTerminalOutput("  search name <text>            - Search by name");
            AddTerminalOutput("  search type <type>            - Search by type");
            AddTerminalOutput("  search id <id>                - Search by ID");
            AddTerminalOutput("  filter type <type>            - Filter by server type");
            AddTerminalOutput("  filter id < <value>           - Filter ID less than");
            AddTerminalOutput("  filter id > <value>           - Filter ID greater than");
            AddTerminalOutput("  filter id = <value>           - Filter ID equal to");
            AddTerminalOutput("  filter reset                  - Clear all filters");
            AddTerminalOutput("");
            AddTerminalOutput("NAVIGATION:");
            AddTerminalOutput("  navigate <1|2|3>              - Switch tabs");
            AddTerminalOutput("                                  1=Entities, 2=Display, 3=Graph");
            AddTerminalOutput("  back                          - Return to previous view");
            AddTerminalOutput("");
            AddTerminalOutput("ACTIONS:");
            AddTerminalOutput("  undo                          - Undo last action");
            AddTerminalOutput("  ping <id>                     - Ping server");
            AddTerminalOutput("  clear                         - Clear terminal");
            AddTerminalOutput("  help                          - Show this help");
            AddTerminalOutput("");
            AddTerminalOutput("KEYBOARD SHORTCUTS:");
            AddTerminalOutput("  Ctrl+T                        - Focus terminal");
            AddTerminalOutput("  Ctrl+Tab                      - Next tab");
            AddTerminalOutput("  Arrow Up/Down                 - Navigate command history");
            AddTerminalOutput("╚════════════════════════════════════════════════════════╝");
        }

        private void AddServerCommand(string[] parts)
        {
            try
            {
                int id = int.Parse(parts[1]);
                string name = parts[2];
                string typeStr = parts[3];
                string ip = parts[4];

                // Provera da li postoji
                if (Servers.Any(s => s.Id == id))
                {
                    AddTerminalOutput($"Error: Server with ID {id} already exists");
                    return;
                }

                // Validacija tipa i slike
                ServerType type = null;
                switch (typeStr.ToLower())
                {
                    case "web":
                        type = new ServerType("Web", "/Resources/Images/web_server.png");
                        break;
                    case "database":
                    case "db":
                        type = new ServerType("Database", "/Resources/Images/database_server.png");
                        break;
                    case "file":
                        type = new ServerType("File", "/Resources/Images/file_server.png");
                        break;
                    default:
                        AddTerminalOutput($"Error: Invalid server type '{typeStr}'");
                        AddTerminalOutput("Valid types: Web, Database, File");
                        return;
                }

                // Validacija IP
                if (!IsValidIPAddress(ip))
                {
                    AddTerminalOutput($"Error: Invalid IP address format '{ip}'");
                    return;
                }

                var server = new Server
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    IPAddress = ip,
                    LastMeasurement = 0,
                    LastUpdate = DateTime.Now
                };

                App.Current.Dispatcher.Invoke(() =>
                {
                    AddServer(server);
                    AddTerminalOutput($"✓ Server added: {name} (ID: {id:000}, Type: {type.Name}, IP: {ip})");

                    // 
                    var undoAction = new MyICommand(() => RemoveServer(server));
                    AddUndoAction(undoAction);
                });
            }
            catch (FormatException)
            {
                AddTerminalOutput("Error: ID must be a valid number");
            }
            catch (Exception ex)
            {
                AddTerminalOutput($"Error adding server: {ex.Message}");
            }
        }

        private void RemoveServerCommand(string idStr)
        {
            try
            {
                int id = int.Parse(idStr);
                var server = Servers.FirstOrDefault(s => s.Id == id);

                if (server != null)
                {
                    // Podaci za confirm
                    pendingRemovalServer = server;
                    awaitingConfirmation = true;

                    AddTerminalOutput($"Are you sure you want to remove '{server.Name}' (ID: {id:000})? [Y/N]");
                }
                else
                {
                    AddTerminalOutput($"Error: Server with ID {id} not found");
                }
            }
            catch (FormatException)
            {
                AddTerminalOutput("Error: ID must be a valid number");
            }
        }

        private Server pendingRemovalServer = null;
        private bool awaitingConfirmation = false;

        private void SearchServers(string searchType, string searchValue)
        {
            IEnumerable<Server> results = null;

            switch (searchType)
            {
                case "name":
                    results = Servers.Where(s => s.Name.ToLower().Contains(searchValue.ToLower()));
                    AddTerminalOutput($"Searching for servers with name containing '{searchValue}':");
                    break;

                case "type":
                    results = Servers.Where(s => s.Type.Name.ToLower().Equals(searchValue.ToLower()));
                    AddTerminalOutput($"Searching for servers of type '{searchValue}':");
                    break;

                case "id":
                    if (int.TryParse(searchValue, out int id))
                    {
                        results = Servers.Where(s => s.Id == id);
                        AddTerminalOutput($"Searching for server with ID {id}:");
                    }
                    else
                    {
                        AddTerminalOutput("Error: ID must be a valid number");
                        return;
                    }
                    break;

                default:
                    AddTerminalOutput($"Unknown search type: {searchType}");
                    AddTerminalOutput("Valid types: name, type, id");
                    return;
            }

            if (results != null && results.Any())
            {
                foreach (var server in results)
                {
                    AddTerminalOutput($"  {server.Id:000}: {server.Name} ({server.Type.Name}) - {server.IPAddress} [{server.Status}]");
                }
                AddTerminalOutput($"Found {results.Count()} result(s)");
            }
            else
            {
                AddTerminalOutput("No servers found matching the search criteria");
            }
        }

        private void ApplyFilter(string[] parts)
        {
            if (CurrentViewModel == entitiesViewModel)
            {
                if (parts[1] == "type" && parts.Length >= 3)
                {
                    string filterType = parts[2];
                    entitiesViewModel.SelectedFilterType =
                        filterType.Equals("all", StringComparison.OrdinalIgnoreCase) ? "All" :
                        filterType.Equals("web", StringComparison.OrdinalIgnoreCase) ? "Web" :
                        filterType.Equals("database", StringComparison.OrdinalIgnoreCase) ? "Database" :
                        filterType.Equals("file", StringComparison.OrdinalIgnoreCase) ? "File" :
                        "All";

                    AddTerminalOutput($"Filter applied: Type = {entitiesViewModel.SelectedFilterType}");
                }
                else if (parts[1] == "id" && parts.Length >= 4)
                {
                    string op = parts[2];
                    if (int.TryParse(parts[3], out int value))
                    {
                        entitiesViewModel.FilterIdValue = value;

                        switch (op)
                        {
                            case "<":
                                entitiesViewModel.IsLessThan = true;
                                AddTerminalOutput($"Filter applied: ID < {value}");
                                break;
                            case ">":
                                entitiesViewModel.IsGreaterThan = true;
                                AddTerminalOutput($"Filter applied: ID > {value}");
                                break;
                            case "=":
                                entitiesViewModel.IsEqualTo = true;
                                AddTerminalOutput($"Filter applied: ID = {value}");
                                break;
                            default:
                                AddTerminalOutput("Invalid operator. Use <, >, or =");
                                break;
                        }
                    }
                    else
                    {
                        AddTerminalOutput("Error: Filter value must be a number");
                    }
                }
            }
            else
            {
                AddTerminalOutput("Filters can only be applied in the Entities view");
                AddTerminalOutput("Switch to Entities view first: navigate 1");
            }
        }

        private void ResetFilters()
        {
            if (CurrentViewModel == entitiesViewModel)
            {
                entitiesViewModel.SelectedFilterType = "All";
                entitiesViewModel.FilterIdValue = 0;
                entitiesViewModel.IsLessThan = false;
                entitiesViewModel.IsGreaterThan = false;
                entitiesViewModel.IsEqualTo = false;
                AddTerminalOutput("✓ All filters have been reset");
            }
            else
            {
                AddTerminalOutput("Switch to Entities view to reset filters: navigate 1");
            }
        }

        private Stack<BindableBase> navigationHistory = new Stack<BindableBase>();

        private void NavigateBack()
        {
            if (navigationHistory.Count > 0)
            {
                var previousView = navigationHistory.Pop();
                CurrentViewModel = previousView;

                string viewName = previousView == entitiesViewModel ? "Entities" :
                                 previousView == displayViewModel ? "Display" :
                                 previousView == graphViewModel ? "Graph" : "Unknown";

                AddTerminalOutput($"← Navigated back to {viewName} view");
            }
            else
            {
                AddTerminalOutput("No previous view in navigation history");
            }
        }

        private bool IsValidIPAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            string[] parts = ip.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (string part in parts)
            {
                if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                    return false;
            }
            return true;
        }

        private void NavigateToTab(string tabParam)
        {
            switch (tabParam)
            {
                case "1":
                case "entities":
                    SwitchView(entitiesViewModel);
                    AddTerminalOutput("Navigated to Entities tab");
                    break;

                case "2":
                case "display":
                    SwitchView(displayViewModel);
                    AddTerminalOutput("Navigated to Display tab");
                    break;

                case "3":
                case "graph":
                    SwitchView(graphViewModel);
                    AddTerminalOutput("Navigated to Graph tab");
                    break;

                default:
                    AddTerminalOutput($"Invalid tab parameter: {tabParam}");
                    AddTerminalOutput("Valid options: 1 (Entities), 2 (Display), 3 (Graph)");
                    break;
            }
        }

        private void ListServers()
        {
            if (Servers.Count == 0)
            {
                AddTerminalOutput("No servers registered. Waiting for MeteringStation data...");
                return;
            }

            foreach (var server in Servers)
            {
                AddTerminalOutput($"{server.Id:000}: {server.Name} ({server.IPAddress}) - {server.Status}");
            }
        }

        private void PingServer(string idStr)
        {
            if (int.TryParse(idStr, out int id))
            {
                var server = Servers.FirstOrDefault(s => s.Id == id);
                if (server != null)
                {
                    AddTerminalOutput($"Pinging {server.IPAddress}... Response: {server.Status}");
                }
                else
                {
                    AddTerminalOutput($"Server with ID {id} not found");
                }
            }
            else
            {
                AddTerminalOutput("Invalid server ID");
            }
        }

        private void NavigateHistoryUp()
        {
            if (historyIndex > 0 && CommandHistory.Count > 0)
            {
                historyIndex--;
                CurrentCommand = CommandHistory[historyIndex];
            }
        }

        private void NavigateHistoryDown()
        {
            if (historyIndex < CommandHistory.Count - 1)
            {
                historyIndex++;
                CurrentCommand = CommandHistory[historyIndex];
            }
            else if (historyIndex == CommandHistory.Count - 1)
            {
                historyIndex = CommandHistory.Count;
                CurrentCommand = string.Empty;
            }
        }

        public void AddServer(Server server)
        {
            Servers.Add(server);
            LogToFile($"Server added: {server.Name} (ID: {server.Id})");
            ToastService.Success($"Server added: {server.Name} (ID: {server.Id:000}).");
        }

        public void RemoveServer(Server server)
        {
            Servers.Remove(server);
            LogToFile($"Server removed: {server.Name} (ID: {server.Id})");
            ToastService.Info($"Server removed: {server.Name} (ID: {server.Id})");
        }

        public void AddUndoAction(ICommand command)
        {
            lastUndoAction = command;
            ((MyICommand)UndoCommand).RaiseCanExecuteChanged();
        }

        private void ExecuteUndo()
        {
            if (lastUndoAction != null)
            {
                var action = lastUndoAction;
                lastUndoAction = null;  // Ocisti, samo poslednja akcija

                action.Execute(null);
                AddTerminalOutput("Undo executed");
                StatusMessage = "Action undone";
                ((MyICommand)UndoCommand).RaiseCanExecuteChanged();
            }
        }

        private void AddTerminalOutput(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            TerminalOutput.Add($"[{timestamp}] {message}");

            // Limit terminala
            while (TerminalOutput.Count > 100)
            {
                TerminalOutput.RemoveAt(0);
            }
        }

        private void CreateListener()
        {
            isListening = true;
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            listeningThread = new Thread(() =>
            {
                while (isListening)
                {
                    try
                    {
                        var tcpClient = tcp.AcceptTcpClient();
                        ThreadPool.QueueUserWorkItem(param =>
                        {
                            try
                            {
                                NetworkStream stream = tcpClient.GetStream();
                                byte[] bytes = new byte[1024];
                                int i = stream.Read(bytes, 0, bytes.Length);
                                string incoming = Encoding.ASCII.GetString(bytes, 0, i);

                                if (incoming.Equals("Need object count"))
                                {
                                    byte[] data = Encoding.ASCII.GetBytes(Servers.Count.ToString());
                                    stream.Write(data, 0, data.Length);
                                }
                                else
                                {
                                    ProcessMeasurement(incoming);
                                }

                                stream.Close();
                                tcpClient.Close();
                            }
                            catch (Exception ex)
                            {
                                LogToFile($"Error processing client: {ex.Message}");
                            }
                        }, null);
                    }
                    catch (Exception ex)
                    {
                        if (isListening)
                        {
                            LogToFile($"Listener error: {ex.Message}");
                        }
                    }
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();

            AddTerminalOutput("TCP Listener started on port 25675");
        }

        private void ProcessMeasurement(string data)
        {
            // Format: "Entitet_X:Value"
            string[] parts = data.Split(':');
            if (parts.Length == 2)
            {
                string entityPart = parts[0];
                if (double.TryParse(parts[1], out double value))
                {
                    string[] entityParts = entityPart.Split('_');
                    if (entityParts.Length == 2 && int.TryParse(entityParts[1], out int index))
                    {
                        // MeteringStation salje 0-based index
                        // Update mojih server u odnosu na ovo
                        int serverId = index + 1; // Pretvaranje 1-based ID

                        var server = Servers.FirstOrDefault(s => s.Id == serverId);
                        if (server == null)
                        {
                            // Ako ne postoji, napravi novi
                            string serverType = DetermineServerType(serverId);
                            server = new Server
                            {
                                Id = serverId,
                                Name = $"Server {serverId:000}",
                                IPAddress = $"192.168.1.{10 + serverId}",
                                Type = new ServerType(serverType, $"/Resources/Images/{serverType.ToLower()}_server.png"),
                                LastMeasurement = value,
                                LastUpdate = DateTime.Now
                            };

                            App.Current.Dispatcher.Invoke(() => {
                                AddServer(server);
                                AddTerminalOutput($"New server registered: {server.Name} with initial value {value:F0}%");
                                AppendMeasurementLog(server);
                            });
                        }
                        else
                        {
                            // Update postojeceg
                            App.Current.Dispatcher.Invoke(() => {
                                server.LastMeasurement = value;
                                server.LastUpdate = DateTime.Now;
                                AppendMeasurementLog(server);
                            });
                        }

                        LogToFile($"Measurement received - Server: {serverId}, Value: {value:F0}%, Time: {DateTime.Now}");

                        // Update grafa
                        if (graphViewModel != null)
                        {
                            App.Current.Dispatcher.Invoke(() => {
                                graphViewModel.AddMeasurement(serverId, value);
                            });
                        }
                    }
                }
            }
        }

        private string DetermineServerType(int serverId)
        {
            // Dodeli tip na osnovu T6
            // Web serveri: ID-jevi 1, 4, 7
            // Database serveri: ID-jevi 2, 5, 8
            // File serveri: ID-jevi 3, 6, 9, 0

            int mod = serverId % 10;
            switch (mod)
            {
                case 1:
                case 4:
                case 7:
                    return "Web";
                case 2:
                case 5:
                case 8:
                    return "Database";
                case 3:
                case 6:
                case 9:
                case 0:
                    return "File";
                default:
                    return "Web"; // Default Web
            }
        }

        private void AppendMeasurementLog(Server s)
        {
            try
            {
                const string header = "Timestamp, ID, Name, Type, IP, Address, Load(%), Status";
                if (!File.Exists(measurementLogPath))
                {
                    File.AppendAllText(measurementLogPath, header + Environment.NewLine);
                }

                // format kao u mini terminalu
                string ts = DateTime.Now.ToString("HH:mm:ss"); 
                string name = string.IsNullOrWhiteSpace(s.Name) ? $"Server {s.Id:000}" : s.Name;

                string line =
                    $"[{ts}] {s.Id:000}, {name}, {s.Type?.Name}, {s.IPAddress}, {s.IPAddress}, {s.LastMeasurement:F0}%, {s.Status}{Environment.NewLine}";

                File.AppendAllText(measurementLogPath, line);
            }
            catch
            {
                // ne blokirati UI zbog I/O problema
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                string logPath = "server_log.txt";
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logEntry);
            }
            catch { }
        }

        public void Dispose()
        {
            isListening = false;
            listeningThread?.Join(1000);
        }
    }
}