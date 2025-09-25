using NetworkService.Common;
using NetworkService.Model;
using NetworkService.Services;
using NetworkService.ViewModel.Commands;
using NetworkService.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows;
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
        private bool isRestoringState = false;

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

        public ICommand ClearFiltersCommand { get; set; }

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
                // Save current state for undo
                var previousState = new FilterState
                {
                    FilterType = selectedFilterType,
                    IdValue = filterIdValue,
                    LessThan = isLessThan,
                    GreaterThan = isGreaterThan,
                    EqualTo = isEqualTo
                };

                SetProperty(ref selectedFilterType, value);
                ApplyFilter();

                // Add undo action
                var undoAction = new MyICommand(() => RestoreFilterState(previousState));
                mainViewModel.AddUndoAction(undoAction);
            }
        }

        public int FilterIdValue
        {
            get { return filterIdValue; }
            set
            {
                // Don't create undo action if value is being set programmatically during restore
                if (!isRestoringState)
                {
                    // Save current state for undo
                    var previousState = new FilterState
                    {
                        FilterType = selectedFilterType,
                        IdValue = filterIdValue,
                        LessThan = isLessThan,
                        GreaterThan = isGreaterThan,
                        EqualTo = isEqualTo
                    };

                    SetProperty(ref filterIdValue, value);
                    ApplyFilter();

                    // Add undo action only if value actually changed
                    if (previousState.IdValue != value)
                    {
                        var undoAction = new MyICommand(() => RestoreFilterState(previousState));
                        mainViewModel.AddUndoAction(undoAction);
                    }
                }
                else
                {
                    SetProperty(ref filterIdValue, value);
                }
            }
        }

        private class FilterState
        {
            public string FilterType { get; set; }
            public int IdValue { get; set; }
            public bool LessThan { get; set; }
            public bool GreaterThan { get; set; }
            public bool EqualTo { get; set; }
        }

        public bool IsLessThan
        {
            get { return isLessThan; }
            set
            {
                if (!isRestoringState)
                {
                    // Save current state for undo
                    var previousState = new FilterState
                    {
                        FilterType = selectedFilterType,
                        IdValue = filterIdValue,
                        LessThan = isLessThan,
                        GreaterThan = isGreaterThan,
                        EqualTo = isEqualTo
                    };

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

                    // Add undo action
                    var undoAction = new MyICommand(() => RestoreFilterState(previousState));
                    mainViewModel.AddUndoAction(undoAction);
                }
                else
                {
                    SetProperty(ref isLessThan, value);
                }
            }
        }

        public bool IsGreaterThan
        {
            get { return isGreaterThan; }
            set
            {
                if (!isRestoringState)
                {
                    // Save current state for undo
                    var previousState = new FilterState
                    {
                        FilterType = selectedFilterType,
                        IdValue = filterIdValue,
                        LessThan = isLessThan,
                        GreaterThan = isGreaterThan,
                        EqualTo = isEqualTo
                    };

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

                    // Add undo action
                    var undoAction = new MyICommand(() => RestoreFilterState(previousState));
                    mainViewModel.AddUndoAction(undoAction);
                }
                else
                {
                    SetProperty(ref isGreaterThan, value);
                }
            }
        }

        public bool IsEqualTo
        {
            get { return isEqualTo; }
            set
            {
                if (!isRestoringState)
                {
                    // Save current state for undo
                    var previousState = new FilterState
                    {
                        FilterType = selectedFilterType,
                        IdValue = filterIdValue,
                        LessThan = isLessThan,
                        GreaterThan = isGreaterThan,
                        EqualTo = isEqualTo
                    };

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

                    // Add undo action
                    var undoAction = new MyICommand(() => RestoreFilterState(previousState));
                    mainViewModel.AddUndoAction(undoAction);
                }
                else
                {
                    SetProperty(ref isEqualTo, value);
                }
            }
        }

        // Helper method to restore filter state
        private void RestoreFilterState(FilterState state)
        {
            // Use flag to prevent creating new undo actions during restore
            isRestoringState = true;

            try
            {
                // Restore all filter values
                selectedFilterType = state.FilterType;
                OnPropertyChanged(nameof(SelectedFilterType));

                // Clear the ID value field if no filter was applied
                if (!state.LessThan && !state.GreaterThan && !state.EqualTo)
                {
                    filterIdValue = 0; // Clear the field
                }
                else
                {
                    filterIdValue = state.IdValue;
                }
                OnPropertyChanged(nameof(FilterIdValue));

                isLessThan = state.LessThan;
                OnPropertyChanged(nameof(IsLessThan));

                isGreaterThan = state.GreaterThan;
                OnPropertyChanged(nameof(IsGreaterThan));

                isEqualTo = state.EqualTo;
                OnPropertyChanged(nameof(IsEqualTo));

                ApplyFilter();
            }
            finally
            {
                isRestoringState = false;
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
            ClearFiltersCommand = new MyICommand(ClearAllFilters);
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

            // CORRECT: Undo for add should remove
            var undoAction = new MyICommand(() => mainViewModel.RemoveServer(server));
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

                    // CORRECT: Undo for delete should add back
                    var undoAction = new MyICommand(() => mainViewModel.AddServer(server));
                    mainViewModel.AddUndoAction(undoAction);

                    // Optional: Show toast notification
                    ToastService.Info($"Server '{server.Name}' deleted");
                }
            }
        }

        private void ClearAllFilters()
        {
            // Save current state for undo
            var previousState = new FilterState
            {
                FilterType = selectedFilterType,
                IdValue = filterIdValue,
                LessThan = isLessThan,
                GreaterThan = isGreaterThan,
                EqualTo = isEqualTo
            };

            // Clear all filters
            SetProperty(ref selectedFilterType, "All");
            SetProperty(ref filterIdValue, 0);
            SetProperty(ref isLessThan, false);
            SetProperty(ref isGreaterThan, false);
            SetProperty(ref isEqualTo, false);

            ApplyFilter();

            // Add undo action
            var undoAction = new MyICommand(() => RestoreFilterState(previousState));
            mainViewModel.AddUndoAction(undoAction);
        }

        private bool CanDeleteServer()
        {
            return SelectedServer != null;
        }

        private bool CanDeleteSelectedServers()
        {
            return mainViewModel.Servers.Any(s => s.IsSelected);
        }

        // Helper method to validate IP address
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

        protected override void ValidateSelf()
        {
            // Clear any previous validation errors
            ValidationErrors.Clear();

            // ID validation
            if (NewServer.Id <= 0)
            {
                ValidationErrors["Server"] = "ID must be greater than 0";
            }
            else if (mainViewModel.Servers.Any(s => s.Id == NewServer.Id))
            {
                ValidationErrors["Server"] = "ID already exists";
            }
            // Name validation
            else if (string.IsNullOrWhiteSpace(NewServer.Name))
            {
                ValidationErrors["Server"] = "Name is required";
            }
            // IP Address validation
            else if (string.IsNullOrWhiteSpace(NewServer.IPAddress))
            {
                ValidationErrors["Server"] = "IP Address is required";
            }
            else if (!IsValidIPAddress(NewServer.IPAddress))
            {
                ValidationErrors["Server"] = "Invalid IP address format (e.g., 192.168.1.1)";
            }
            // Type validation
            else if (NewServer.Type == null)
            {
                ValidationErrors["Server"] = "Type must be selected";
            }
        }
    }
}