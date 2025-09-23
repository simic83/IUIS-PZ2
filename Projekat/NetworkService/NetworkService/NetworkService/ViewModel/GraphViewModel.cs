using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using NetworkService.Common;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class MeasurementBar : BindableBase
    {
        private double value;
        private DateTime time;
        private double height;
        private double x;
        private Brush color;
        private string timeLabel;

        public double Value
        {
            get { return value; }
            set { SetProperty(ref this.value, value); }
        }

        public DateTime Time
        {
            get { return time; }
            set { SetProperty(ref time, value); }
        }

        public double Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        public double X
        {
            get { return x; }
            set { SetProperty(ref x, value); }
        }

        public Brush Color
        {
            get { return color; }
            set { SetProperty(ref color, value); }
        }

        public string TimeLabel
        {
            get { return timeLabel; }
            set { SetProperty(ref timeLabel, value); }
        }

        public string ValueLabel => $"{Value:F0}%";
    }

    public class GraphViewModel : BindableBase
    {
        private MainWindowViewModel mainViewModel;
        private Server selectedServer;
        private ObservableCollection<MeasurementBar> measurementBars;
        private ObservableCollection<MeasurementData> measurementHistory;
        private string graphTitle;
        private string statusInfo;

        public ObservableCollection<Server> Servers => mainViewModel.Servers;

        public Server SelectedServer
        {
            get { return selectedServer; }
            set
            {
                SetProperty(ref selectedServer, value);
                UpdateGraph();
                UpdateGraphInfo();
            }
        }

        public ObservableCollection<MeasurementBar> MeasurementBars
        {
            get { return measurementBars; }
            set { SetProperty(ref measurementBars, value); }
        }

        public string GraphTitle
        {
            get { return graphTitle; }
            set { SetProperty(ref graphTitle, value); }
        }

        public string StatusInfo
        {
            get { return statusInfo; }
            set { SetProperty(ref statusInfo, value); }
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

            // Keep only last 10 measurements per server (for smooth operation)
            var serverMeasurements = measurementHistory.Where(m => m.ServerId == serverId).ToList();
            if (serverMeasurements.Count > 10)
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

            // Get last 5 measurements for selected server
            var serverMeasurements = measurementHistory
                .Where(m => m.ServerId == SelectedServer.Id)
                .OrderByDescending(m => m.Timestamp)
                .Take(5)  // Only last 5 measurements
                .Reverse()
                .ToList();

            if (serverMeasurements.Count == 0)
            {
                MeasurementBars = bars;
                return;
            }

            // Calculate bar positions and sizes
            double barWidth = 60;  // Wider bars since we only have 5
            double spacing = 100;   // More space between bars
            double startX = 60;    // Start position for first bar
            double maxHeight = 200;

            int index = 0;
            foreach (var measurement in serverMeasurements)
            {
                var bar = new MeasurementBar
                {
                    Value = measurement.Value,
                    Time = measurement.Timestamp,
                    Height = (measurement.Value / 100.0) * maxHeight,
                    X = startX + (index * (barWidth + spacing)),
                    Color = GetBarColor(measurement.IsValid),
                    TimeLabel = measurement.Timestamp.ToString("HH:mm:ss")
                };
                bars.Add(bar);
                index++;
            }

            MeasurementBars = bars;
        }

        private Brush GetBarColor(bool isValid)
        {
            if (isValid)
            {
                // Green gradient for valid values
                var brush = new LinearGradientBrush();
                brush.StartPoint = new System.Windows.Point(0, 0);
                brush.EndPoint = new System.Windows.Point(0, 1);
                brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0, 255, 0), 0));
                brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0, 200, 0), 1));
                return brush;
            }
            else
            {
                // Red gradient for out-of-range values
                var brush = new LinearGradientBrush();
                brush.StartPoint = new System.Windows.Point(0, 0);
                brush.EndPoint = new System.Windows.Point(0, 1);
                brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(255, 100, 100), 0));
                brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(200, 50, 50), 1));
                return brush;
            }
        }

        private void UpdateGraphInfo()
        {
            if (SelectedServer != null)
            {
                GraphTitle = $"Real-time Measurements for {SelectedServer.Name} (ID: {SelectedServer.Id:000})";
                StatusInfo = $"Server Type: {SelectedServer.Type.Name} | Current Status: {SelectedServer.Status}";
            }
            else
            {
                GraphTitle = "No Server Selected";
                StatusInfo = "";
            }
        }
    }
}