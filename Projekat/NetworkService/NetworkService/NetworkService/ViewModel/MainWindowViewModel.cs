using NetworkService.Common;
using NetworkService.Model;
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

            // Add initial servers
            AddServer(new Server
            {
                Id = 1,
                Name = "Web Server 1",
                IPAddress = "192.168.1.10",
                Type = new ServerType("Web", "/Resources/Images/web_server.png"),
                LastMeasurement = 67,
                LastUpdate = DateTime.Now
            });

            AddServer(new Server
            {
                Id = 2,
                Name = "Database Primary",
                IPAddress = "192.168.1.20",
                Type = new ServerType("Database", "/Resources/Images/database_server.png"),
                LastMeasurement = 23,
                LastUpdate = DateTime.Now
            });

            AddServer(new Server
            {
                Id = 3,
                Name = "File Server",
                IPAddress = "192.168.1.30",
                Type = new ServerType("File", "/Resources/Images/file_server.png"),
                LastMeasurement = 89,
                LastUpdate = DateTime.Now
            });
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
            foreach (var server in Servers)
            {
                AddTerminalOutput($"{server.Id}: {server.Name} ({server.IPAddress}) - {server.Status}");
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
        }

        public void RemoveServer(Server server)
        {
            Servers.Remove(server);
            LogToFile($"Server removed: {server.Name} (ID: {server.Id})");
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
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            var listeningThread = new Thread(() =>
            {
                while (true)
                {
                    var tcpClient = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(param =>
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
                    }, null);
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();
        }

        private void ProcessMeasurement(string data)
        {
            // Format: "Entitet_ID:Value"
            string[] parts = data.Split(':');
            if (parts.Length == 2)
            {
                string entityPart = parts[0];
                if (double.TryParse(parts[1], out double value))
                {
                    string[] entityParts = entityPart.Split('_');
                    if (entityParts.Length == 2 && int.TryParse(entityParts[1], out int id))
                    {
                        var server = Servers.FirstOrDefault(s => s.Id == id + 1); // Adjust for 0-based index
                        if (server != null)
                        {
                            server.LastMeasurement = value;
                            server.LastUpdate = DateTime.Now;

                            LogToFile($"Measurement received - Server: {server.Name}, Value: {value}%, Time: {DateTime.Now}");

                            // Update graph data
                            if (CurrentViewModel is GraphViewModel graph)
                            {
                                graph.AddMeasurement(server.Id, value);
                            }
                        }
                    }
                }
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
    }
}