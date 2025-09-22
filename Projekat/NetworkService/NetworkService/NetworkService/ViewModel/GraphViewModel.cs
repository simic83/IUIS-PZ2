using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using NetworkService.Common;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class MeasurementBar
    {
        public double Value { get; set; }
        public DateTime Time { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public Brush Color { get; set; }
    }

    public class GraphViewModel : BindableBase
    {
        private MainWindowViewModel mainViewModel;
        private Server selectedServer;
        private ObservableCollection<MeasurementBar> measurementBars;
        private ObservableCollection<MeasurementData> measurementHistory;

        public ObservableCollection<Server> Servers => mainViewModel.Servers;

        public Server SelectedServer
        {
            get { return selectedServer; }
            set
            {
                SetProperty(ref selectedServer, value);
                UpdateGraph();
            }
        }

        public ObservableCollection<MeasurementBar> MeasurementBars
        {
            get { return measurementBars; }
            set { SetProperty(ref measurementBars, value); }
        }

        public GraphViewModel(MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            measurementHistory = new ObservableCollection<MeasurementData>();
            MeasurementBars = new ObservableCollection<MeasurementBar>();

            if (Servers.Count > 0)
            {
                SelectedServer = Servers[0];
            }
        }

        public void AddMeasurement(int serverId, double value)
        {
            var measurement = new MeasurementData(serverId, value);
            measurementHistory.Add(measurement);

            // Keep only last 100 measurements per server
            var serverMeasurements = measurementHistory.Where(m => m.ServerId == serverId).ToList();
            if (serverMeasurements.Count > 100)
            {
                measurementHistory.Remove(serverMeasurements[0]);
            }

            if (SelectedServer != null && SelectedServer.Id == serverId)
            {
                UpdateGraph();
            }
        }

        private void UpdateGraph()
        {
            if (SelectedServer == null) return;

            var bars = new ObservableCollection<MeasurementBar>();
            var serverMeasurements = measurementHistory
                .Where(m => m.ServerId == SelectedServer.Id)
                .OrderByDescending(m => m.Timestamp)
                .Take(5)
                .Reverse()
                .ToList();

            double xPosition = 20;
            foreach (var measurement in serverMeasurements)
            {
                var bar = new MeasurementBar
                {
                    Value = measurement.Value,
                    Time = measurement.Timestamp,
                    Height = (measurement.Value / 100.0) * 200, // Scale to canvas height
                    X = xPosition,
                    Color = measurement.IsValid ? Brushes.Lime : Brushes.Red
                };
                bars.Add(bar);
                xPosition += 80;
            }

            MeasurementBars = bars;
        }
    }
}