using System;

namespace Lazarus.Desktop.Services;

public sealed record SystemMetrics(
    double CpuUsagePercent,
    double RamUsedGb,
    double RamTotalGb,
    double GpuUsagePercent,
    double GpuVramUsedGb,
    double GpuVramTotalGb,
    string? GpuName
);

public interface ISystemMetricsService : IDisposable
{
    event EventHandler<SystemMetrics>? MetricsUpdated;

    bool Start();
    void Stop();
}


