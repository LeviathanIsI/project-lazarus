using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Lazarus.Desktop.Services;

internal sealed class SystemMetricsService : ISystemMetricsService
{
    private readonly ILogger<SystemMetricsService> _logger;
    private readonly Timer _timer;
    private PerformanceCounter? _cpuCounter;
    private ulong _totalRamBytes;

    // GPU via WMI (usage not directly available; we approximate via EngineUtilization if NVAPI unavailable)
    private string? _gpuName = null;
    private double _gpuUsagePercent = 0.0;
    private double _gpuVramTotalGb = 0.0;
    private double _gpuVramUsedGb = 0.0;

    public event EventHandler<SystemMetrics>? MetricsUpdated;

    public SystemMetricsService(ILogger<SystemMetricsService> logger)
    {
        _logger = logger;
        _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            // Warm-up read to avoid first-sample spikes
            _ = _cpuCounter.NextValue();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize CPU performance counter");
        }

        try
        {
            // Total physical memory via GlobalMemoryStatusEx for accuracy
            _totalRamBytes = QueryTotalPhysicalBytes();

            // GPU name and VRAM via WMI
            using var searcher = new ManagementObjectSearcher("select Name, AdapterRAM from Win32_VideoController");
            var first = searcher.Get().OfType<ManagementObject>().FirstOrDefault();
            if (first != null)
            {
                _gpuName = (first["Name"] as string)?.Trim();
                if (first["AdapterRAM"] != null && ulong.TryParse(first["AdapterRAM"].ToString(), out var vramBytes))
                {
                    _gpuVramTotalGb = vramBytes / (1024.0 * 1024.0 * 1024.0);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query hardware baseline for metrics");
        }
    }

    public bool Start()
    {
        try
        {
            _timer.Change(0, 1000); // 1s updates
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start metrics timer");
            return false;
        }
    }

    public void Stop()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void OnTick(object? state)
    {
        try
        {
            var cpu = _cpuCounter != null ? Math.Clamp(_cpuCounter.NextValue(), 0, 100) : 0;

            // Live RAM via GlobalMemoryStatusEx
            var (totalBytes, availBytes) = QueryMemoryStatus();
            if (totalBytes > 0) _totalRamBytes = totalBytes; // refresh if changed (rare)
            double totalGb = _totalRamBytes / (1024.0 * 1024.0 * 1024.0);
            double usedGb = Math.Max(0, (totalBytes - availBytes) / (1024.0 * 1024.0 * 1024.0));

            // GPU usage best-effort: not trivial via WMI; leave at 0 if unavailable
            // Future: plug NVML/NVAPI when present
            var metrics = new SystemMetrics(
                Math.Round(cpu, 1),
                Math.Round(usedGb, 2),
                Math.Round(totalGb, 2),
                Math.Round(_gpuUsagePercent, 1),
                Math.Round(_gpuVramUsedGb, 2),
                Math.Round(_gpuVramTotalGb, 2),
                _gpuName
            );

            MetricsUpdated?.Invoke(this, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Metrics tick failed");
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _cpuCounter?.Dispose();
    }

    // P/Invoke for accurate physical memory totals
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    private static ulong QueryTotalPhysicalBytes()
    {
        var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        return GlobalMemoryStatusEx(ref ms) ? ms.ullTotalPhys : 0UL;
    }

    private static (ulong total, ulong avail) QueryMemoryStatus()
    {
        var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref ms))
        {
            return (ms.ullTotalPhys, ms.ullAvailPhys);
        }
        return (0UL, 0UL);
    }
}


