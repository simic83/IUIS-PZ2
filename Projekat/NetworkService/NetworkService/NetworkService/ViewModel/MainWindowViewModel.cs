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
        private Stack<ICommand> undoStack;
        private string currentCommand;
        private int historyIndex;
        private BindableBase currentViewModel;
        private EntitiesViewModel entitiesViewModel;
        private DisplayViewModel displayViewModel;
        private GraphViewModel graphViewModel;
        private string statusMessage;
        private Thread listeningThread;
        private bool isListening;

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
            undoStack = new Stack<ICommand>();

            // Don't add initial mock servers - wait for MeteringStation data
            // The application should start with empty server list
        }

        private void InitializeCommands()
        {
            SwitchToEntitiesCommand = new MyICommand(() => SwitchView(entitiesViewModel));
            SwitchToDisplayCommand = new MyICommand(() => SwitchView(displayViewModel));
            SwitchToGraphCommand = new MyICommand(() => SwitchView(graphViewModel));
            ExecuteTerminalCommand = new MyICommand(ExecuteTerminal);
            NavigateHistoryUpCommand = new MyICommand(NavigateHistoryUp);
            NavigateHistoryDownCommand = new MyICommand(NavigateHistoryDown);
            UndoCommand = new MyICommand(ExecuteUndo, () => undoStack.Count > 0);
            NextTabCommand = new MyICommand(NextTab);
            FocusTerminalCommand = new MyICommand(FocusTerminal);
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
            CurrentViewModel = viewModel;
            string viewName = viewModel.GetType().Name.Replace("ViewModel", "");
            AddTerminalOutput($"Switched to {viewName} view");
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
            AddTerminalOutput("Terminal focused");
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
            string[] parts = command.ToLower().Split(' ');

            switch (parts[0])
            {
                case "help":
                    AddTerminalOutput("Available commands: list, status, ping, refresh, clear, help");
                    break;
                case "list":
                    ListServers();
                    break;
                case "status":
                    ShowStatus();
                    break;
                case "ping":
                    if (parts.Length > 1)
                        PingServer(parts[1]);
                    else
                        AddTerminalOutput("Usage: ping <server_id>");
                    break;
                case "refresh":
                    RefreshData();
                    break;
                case "clear":
                    TerminalOutput.Clear();
                    break;
                default:
                    AddTerminalOutput($"Unknown command: {parts[0]}");
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

        private void ShowStatus()
        {
            int online = Servers.Count(s => s.Status == "online");
            int warning = Servers.Count(s => s.Status == "warning");
            int offline = Servers.Count(s => s.Status == "offline");

            AddTerminalOutput($"Online: {online}, Warning: {warning}, Offline: {offline}");
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

        private void RefreshData()
        {
            AddTerminalOutput("Refreshing server data...");
            StatusMessage = "Data refreshed";
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
            undoStack.Push(command);
            ((MyICommand)UndoCommand).RaiseCanExecuteChanged();
        }

        private void ExecuteUndo()
        {
            if (undoStack.Count > 0)
            {
                var command = undoStack.Pop();
                command.Execute(null);
                AddTerminalOutput("Undo executed");
                StatusMessage = "Action undone";
                ((MyICommand)UndoCommand).RaiseCanExecuteChanged();
            }
        }

        private void AddTerminalOutput(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            TerminalOutput.Add($"[{timestamp}] {message}");

            // Keep terminal output limited
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
            // Format: "Entitet_X:Value" where X is 0-based index
            string[] parts = data.Split(':');
            if (parts.Length == 2)
            {
                string entityPart = parts[0];
                if (double.TryParse(parts[1], out double value))
                {
                    string[] entityParts = entityPart.Split('_');
                    if (entityParts.Length == 2 && int.TryParse(entityParts[1], out int index))
                    {
                        // MeteringStation sends 0-based index
                        // We need to create or update servers based on this index
                        int serverId = index + 1; // Convert to 1-based ID

                        var server = Servers.FirstOrDefault(s => s.Id == serverId);
                        if (server == null)
                        {
                            // Create new server if it doesn't exist
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
                            });
                        }
                        else
                        {
                            // Update existing server
                            App.Current.Dispatcher.Invoke(() => {
                                server.LastMeasurement = value;
                                server.LastUpdate = DateTime.Now;
                            });
                        }

                        LogToFile($"Measurement received - Server: {serverId}, Value: {value:F0}%, Time: {DateTime.Now}");

                        // Update graph data if needed
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
            // Assign server types based on ID patterns for T6 (Servers)
            // Web servers: IDs ending in 1, 4, 7
            // Database servers: IDs ending in 2, 5, 8
            // File servers: IDs ending in 3, 6, 9, 0

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
                    return "Web"; // Default to Web
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