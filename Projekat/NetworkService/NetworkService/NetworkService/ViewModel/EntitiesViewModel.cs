using NetworkService.Common;
using NetworkService.Model;
using NetworkService.Services;
using NetworkService.ViewModel.Commands;
using NetworkService.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace NetworkService.ViewModel
{
    public class EntitiesViewModel : ValidationBase
    {
        private MainWindowViewModel mainViewModel;
        private Server newServer;
        private Server selectedServer;
        private ObservableCollection<ServerType> serverTypes;
        private ICollectionView filteredServers;
        private string selectedFilterType = "All";
        private int filterIdValue;
        private bool isLessThan, isGreaterThan, isEqualTo;
        private string validationMessage;

        public Server NewServer
        {
            get { return newServer; }
            set { SetProperty(ref newServer, value); }
        }

        public Server SelectedServer
        {
            get { return selectedServer; }
            set { SetProperty(ref selectedServer, value); }
        }

        public ObservableCollection<ServerType> ServerTypes
        {
            get { return serverTypes; }
            set { SetProperty(ref serverTypes, value); }
        }

        public ICollectionView FilteredServers
        {
            get { return filteredServers; }
        }

        public ObservableCollection<string> FilterTypes { get; set; }

        public string SelectedFilterType
        {
            get { return selectedFilterType; }
            set
            {
                SetProperty(ref selectedFilterType, value);
                ApplyFilter();
            }
        }

        public int FilterIdValue
        {
            get { return filterIdValue; }
            set
            {
                SetProperty(ref filterIdValue, value);
                // Always apply filter when value changes
                ApplyFilter();
            }
        }

        public bool IsLessThan
        {
            get { return isLessThan; }
            set
            {
                // Allow deselection - if clicking already selected, deselect it
                if (value && isLessThan)
                {
                    SetProperty(ref isLessThan, false);
                    SetProperty(ref isGreaterThan, false);
                    SetProperty(ref isEqualTo, false);
                }
                else
                {
                    SetProperty(ref isLessThan, value);
                    if (value)
                    {
                        SetProperty(ref isGreaterThan, false);
                        SetProperty(ref isEqualTo, false);
                    }
                }
                ApplyFilter();
            }
        }

        public bool IsGreaterThan
        {
            get { return isGreaterThan; }
            set
            {
                // Allow deselection - if clicking already selected, deselect it
                if (value && isGreaterThan)
                {
                    SetProperty(ref isLessThan, false);
                    SetProperty(ref isGreaterThan, false);
                    SetProperty(ref isEqualTo, false);
                }
                else
                {
                    SetProperty(ref isGreaterThan, value);
                    if (value)
                    {
                        SetProperty(ref isLessThan, false);
                        SetProperty(ref isEqualTo, false);
                    }
                }
                ApplyFilter();
            }
        }

        public bool IsEqualTo
        {
            get { return isEqualTo; }
            set
            {
                // Allow deselection - if clicking already selected, deselect it
                if (value && isEqualTo)
                {
                    SetProperty(ref isLessThan, false);
                    SetProperty(ref isGreaterThan, false);
                    SetProperty(ref isEqualTo, false);
                }
                else
                {
                    SetProperty(ref isEqualTo, value);
                    if (value)
                    {
                        SetProperty(ref isLessThan, false);
                        SetProperty(ref isGreaterThan, false);
                    }
                }
                ApplyFilter();
            }
        }

        public string ValidationMessage
        {
            get { return validationMessage; }
            set { SetProperty(ref validationMessage, value); }
        }

        public ICommand AddServerCommand { get; set; }
        public ICommand DeleteServerCommand { get; set; }
        public ICommand DeleteSelectedCommand { get; set; }
        public ICommand UndoCommand { get; set; }

        public EntitiesViewModel(MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            InitializeData();
            InitializeCommands();
            SetupFiltering();
        }

        private void InitializeData()
        {
            NewServer = new Server();

            ServerTypes = new ObservableCollection<ServerType>
            {
                new ServerType("Web", "/Resources/Images/web_server.png"),
                new ServerType("Database", "/Resources/Images/database_server.png"),
                new ServerType("File", "/Resources/Images/file_server.png")
            };

            FilterTypes = new ObservableCollection<string> { "All", "Web", "Database", "File" };
        }

        private void InitializeCommands()
        {
            AddServerCommand = new MyICommand(AddServer);
            DeleteServerCommand = new MyICommand(DeleteServer, CanDeleteServer);
            UndoCommand = mainViewModel.UndoCommand;
        }

        private void SetupFiltering()
        {
            filteredServers = CollectionViewSource.GetDefaultView(mainViewModel.Servers);
            filteredServers.Filter = ServerFilter;
        }

        private bool ServerFilter(object obj)
        {
            var server = obj as Server;
            if (server == null) return false;

            bool typeMatch = selectedFilterType == "All" || server.Type.Name == selectedFilterType;
            bool idMatch = true;

            // Only apply ID filter if a radio button is selected AND there's a value
            if (filterIdValue > 0 && (isLessThan || isGreaterThan || isEqualTo))
            {
                if (isLessThan) idMatch = server.Id < filterIdValue;
                else if (isGreaterThan) idMatch = server.Id > filterIdValue;
                else if (isEqualTo) idMatch = server.Id == filterIdValue;
            }

            return typeMatch && idMatch;
        }

        private void ApplyFilter()
        {
            filteredServers?.Refresh();
        }

        private void AddServer()
        {
            Validate();
            if (!IsValid)
            {
                ValidationMessage = ValidationErrors["Server"];
                return;
            }

            var server = new Server
            {
                Id = NewServer.Id,
                Name = NewServer.Name,
                IPAddress = NewServer.IPAddress,
                Type = NewServer.Type,
                LastMeasurement = 0,
                LastUpdate = DateTime.Now
            };

            mainViewModel.AddServer(server);

            // Create undo action
            var undoAction = new MyICommand(() => mainViewModel.AddServer(server));
            mainViewModel.AddUndoAction(undoAction);

            // Clear form
            NewServer = new Server();
            ValidationMessage = string.Empty;
        }

        private void DeleteServer()
        {
            if (SelectedServer != null)
            {
                // Show confirmation dialog
                var owner = Application.Current.MainWindow;
                if (ConfirmationDialog.ShowDeleteConfirmation(SelectedServer, owner))
                {
                    var server = SelectedServer;
                    mainViewModel.RemoveServer(server);

                    // Create undo action
                    var undoAction = new MyICommand(() => mainViewModel.AddServer(server));
                    mainViewModel.AddUndoAction(undoAction);

                    // Optional: Show toast notification
                    ToastService.Info($"Server '{server.Name}' deleted");
                }
            }
        }

        private bool CanDeleteServer()
        {
            return SelectedServer != null;
        }

        private bool CanDeleteSelectedServers()
        {
            return mainViewModel.Servers.Any(s => s.IsSelected);
        }

        protected override void ValidateSelf()
        {
            if (NewServer.Id <= 0)
            {
                ValidationErrors["Server"] = "ID must be greater than 0";
            }
            else if (mainViewModel.Servers.Any(s => s.Id == NewServer.Id))
            {
                ValidationErrors["Server"] = "ID already exists";
            }
            else if (string.IsNullOrWhiteSpace(NewServer.Name))
            {
                ValidationErrors["Server"] = "Name is required";
            }
            else if (string.IsNullOrWhiteSpace(NewServer.IPAddress))
            {
                ValidationErrors["Server"] = "IP Address is required";
            }
            else if (NewServer.Type == null)
            {
                ValidationErrors["Server"] = "Type must be selected";
            }
        }
    }
}