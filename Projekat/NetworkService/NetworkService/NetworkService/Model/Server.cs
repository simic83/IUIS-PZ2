using System;
using System.ComponentModel;

namespace NetworkService.Model
{
    public class Server : INotifyPropertyChanged
    {
        private int id;
        private string name;
        private string ipAddress;
        private ServerType type;
        private double lastMeasurement;
        private DateTime lastUpdate;
        private string status;
        private bool isSelected;

        public int Id
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("Id");
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public string IPAddress
        {
            get { return ipAddress; }
            set
            {
                ipAddress = value;
                OnPropertyChanged("IPAddress");
            }
        }

        public ServerType Type
        {
            get { return type; }
            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }

        public double LastMeasurement
        {
            get { return lastMeasurement; }
            set
            {
                lastMeasurement = value;
                UpdateStatus();
                OnPropertyChanged("LastMeasurement");
                OnPropertyChanged("MeasurementDisplay");
            }
        }

        public string MeasurementDisplay
        {
            get { return $"{lastMeasurement:F0}%"; }
        }

        public DateTime LastUpdate
        {
            get { return lastUpdate; }
            set
            {
                lastUpdate = value;
                OnPropertyChanged("LastUpdate");
            }
        }

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        private void UpdateStatus()
        {
            if (lastMeasurement < 45 || lastMeasurement > 75)
            {
                Status = lastMeasurement == 0 ? "offline" : "warning";
            }
            else
            {
                Status = "online";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}