using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Management;

namespace Lazarus.Orchestrator.Services;

public static class SystemInfoService
{
    public static SystemMemoryInfo GetSystemMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsMemoryInfo();
        }

        // Fallback for other platforms
        var process = Process.GetCurrentProcess();
        return new SystemMemoryInfo
        {
            TotalBytes = 0,
            UsedBytes = process.WorkingSet64,
            AvailableBytes = 0
        };
    }

    public static GpuMemoryInfo GetGpuMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsGpuInfo();
        }

        return new GpuMemoryInfo
        {
            TotalBytes = 0,
            UsedBytes = 0,
            AvailableBytes = 0,
            Temperature = 0,
            PowerDraw = 0
        };
    }

    private static SystemMemoryInfo GetWindowsMemoryInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            using var results = searcher.Get();
            var totalMemory = results.Cast<ManagementObject>().First()["TotalPhysicalMemory"];

            var availableCounter = new PerformanceCounter("Memory", "Available Bytes");
            var availableBytes = (long)availableCounter.NextValue();
            var totalBytes = Convert.ToInt64(totalMemory);

            return new SystemMemoryInfo
            {
                TotalBytes = totalBytes,
                UsedBytes = totalBytes - availableBytes,
                AvailableBytes = availableBytes
            };
        }
        catch
        {
            return new SystemMemoryInfo();
        }
    }

    private static GpuMemoryInfo GetWindowsGpuInfo()
    {
        try
        {
            // Basic GPU info - you may want to use NVIDIA ML API for more detailed info
            using var searcher = new ManagementObjectSearcher("SELECT AdapterRAM FROM Win32_VideoController WHERE AdapterRAM IS NOT NULL");
            using var results = searcher.Get();
            var gpu = results.Cast<ManagementObject>().FirstOrDefault();

            if (gpu?["AdapterRAM"] is uint adapterRam)
            {
                var totalBytes = (long)adapterRam;
                // For actual VRAM usage, you'd need NVIDIA ML API or similar
                return new GpuMemoryInfo
                {
                    TotalBytes = totalBytes,
                    UsedBytes = totalBytes / 2, // Mock data
                    AvailableBytes = totalBytes / 2,
                    Temperature = 68,
                    PowerDraw = 387
                };
            }
        }
        catch { }

        return new GpuMemoryInfo();
    }
}

public record SystemMemoryInfo
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long AvailableBytes { get; init; }

    public double UsagePercentage => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
    public string TotalFormatted => FormatBytes(TotalBytes);
    public string UsedFormatted => FormatBytes(UsedBytes);

    private static string FormatBytes(long bytes)
    {
        const long gb = 1024 * 1024 * 1024;
        return bytes >= gb ? $"{bytes / (double)gb:F1} GB" : $"{bytes / (1024 * 1024):F0} MB";
    }
}

public record GpuMemoryInfo
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long AvailableBytes { get; init; }
    public int Temperature { get; init; }
    public int PowerDraw { get; init; }

    public double UsagePercentage => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
    public string TotalFormatted => FormatBytes(TotalBytes);
    public string UsedFormatted => FormatBytes(UsedBytes);

    private static string FormatBytes(long bytes)
    {
        const long gb = 1024 * 1024 * 1024;
        return bytes >= gb ? $"{bytes / (double)gb:F1} GB" : $"{bytes / (1024 * 1024):F0} MB";
    }
}