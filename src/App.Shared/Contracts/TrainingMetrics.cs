using System;
using System.Collections.Generic;

namespace Lazarus.Shared.Contracts
{
    public sealed class TrainingMetricsSnapshot
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public long GlobalStep { get; set; }
        public int Epoch { get; set; }
        public double Loss { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new(); // accuracy, perplexity, etc.
        public double LearningRate { get; set; }
        public TimeSpan StepTime { get; set; }
        public long VramUsedBytes { get; set; }
        public double CpuUsagePercent { get; set; }
        public double GpuUtilizationPercent { get; set; }
    }

    public sealed class TrainingLogEvent
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public LogLevel Level { get; set; }
        public required string Message { get; set; }
        public long? Step { get; set; }
        public int? Epoch { get; set; }
        public string? Component { get; set; } // trainer, loader, model, etc.
    }

    public enum LogLevel
    {
        Debug,
        Info, 
        Warning,
        Error,
        Critical
    }

    // Time series data for charts
    public sealed class MetricSeries
    {
        public required string Name { get; init; }
        public List<MetricPoint> Points { get; set; } = new();
        public string? Color { get; set; }
        public bool Visible { get; set; } = true;
    }

    public sealed class MetricPoint
    {
        public DateTime Timestamp { get; init; }
        public long Step { get; set; }
        public double Value { get; set; }
    }
}
