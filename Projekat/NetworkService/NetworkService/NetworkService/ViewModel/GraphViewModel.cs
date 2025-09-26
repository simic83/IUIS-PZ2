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
            get => value;
            set => SetProperty(ref this.value, value);
        }

        public DateTime Time
        {
            get => time;
            set => SetProperty(ref time, value);
        }

        public double Height
        {
            get => height;
            set => SetProperty(ref height, value);
        }

        public double X
        {
            get => x;
            set => SetProperty(ref x, value);
        }

        public Brush Color
        {
            get => color;
            set => SetProperty(ref color, value);
        }

        public string TimeLabel
        {
            get => timeLabel;
            set => SetProperty(ref timeLabel, value);
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
            get => selectedServer;
            set
            {
                SetProperty(ref selectedServer, value);
                UpdateGraph();
                UpdateGraphInfo();
            }
        }

        public ObservableCollection<MeasurementBar> MeasurementBars
        {
            get => measurementBars;
            set => SetProperty(ref measurementBars, value);
        }

        public string GraphTitle
        {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }

        public string StatusInfo
        {
            get => statusInfo;
            set => SetProperty(ref statusInfo, value);
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

            // zadrži poslednjih 5 po serveru
            var serverMeasurements = measurementHistory.Where(m => m.ServerId == serverId).ToList();
            if (serverMeasurements.Count > 5)
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

            // poslednjih 5 merenja za izabrani server
            var serverMeasurements = measurementHistory
                .Where(m => m.ServerId == SelectedServer.Id)
                .OrderByDescending(m => m.Timestamp)
                .Take(5)
                .Reverse()
                .ToList();

            if (serverMeasurements.Count == 0)
            {
                MeasurementBars = bars;
                return;
            }

            // Usklađeno sa XAML koordinatama:
            // X-osa je na Y=340, gornja referenca Y=20, dakle max visina = 340 - 20 = 320
            double barWidth = 80;
            double spacing = 60;   // prostor između stubića
            double startX = 90;    // da stane lepo između X1=50 i X2=750
            double maxHeight = 320;

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
                // green gradient
                var brush = new LinearGradientBrush();
                brush.StartPoint = new System.Windows.Point(0, 0);
                brush.EndPoint = new System.Windows.Point(0, 1);
                brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0, 255, 0), 0));
                brush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0, 200, 0), 1));
                return brush;
            }
            else
            {
                // red gradient
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
