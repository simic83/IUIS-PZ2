using System;

namespace NetworkService.Model
{
    public class MeasurementData
    {
        public int ServerId { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsValid { get; set; }

        public MeasurementData(int serverId, double value)
        {
            ServerId = serverId;
            Value = value;
            Timestamp = DateTime.Now;
            IsValid = value >= 45 && value <= 75;
        }
    }
}