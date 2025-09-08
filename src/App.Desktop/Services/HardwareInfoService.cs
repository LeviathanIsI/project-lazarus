using Microsoft.Extensions.Logging;
using System.Management;

namespace Lazarus.Desktop.Services;

internal sealed class HardwareInfoService : IHardwareInfoService
{
    private readonly ILogger<HardwareInfoService> _logger;

    public HardwareInfoService(ILogger<HardwareInfoService> logger)
    {
        _logger = logger;
    }

    public async Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            CpuInfo? cpu = null;
            var gpus = new List<GpuInfo>();

            try
            {
                using var cpuSearcher = new ManagementObjectSearcher("select Name, NumberOfCores, NumberOfLogicalProcessors from Win32_Processor");
                foreach (var obj in cpuSearcher.Get().OfType<ManagementObject>())
                {
                    var name = (obj["Name"] as string)?.Trim() ?? "Unknown CPU";
                    var cores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                    var logical = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? Environment.ProcessorCount);
                    cpu = new CpuInfo(name, cores > 0 ? cores : Environment.ProcessorCount, logical > 0 ? logical : Environment.ProcessorCount);
                    break; // take first CPU entry
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query CPU info via WMI");
                cpu = new CpuInfo("Unknown CPU", Environment.ProcessorCount, Environment.ProcessorCount);
            }

            try
            {
                using var gpuSearcher = new ManagementObjectSearcher("select Name, AdapterRAM from Win32_VideoController");
                int idx = 0;
                foreach (var obj in gpuSearcher.Get().OfType<ManagementObject>())
                {
                    var name = (obj["Name"] as string)?.Trim() ?? $"GPU {idx}";
                    ulong adapterRam = 0;
                    try
                    {
                        // AdapterRAM is in bytes
                        if (obj["AdapterRAM"] != null)
                        {
                            adapterRam = Convert.ToUInt64(obj["AdapterRAM"]);
                        }
                    }
                    catch { /* ignore per device */ }

                    gpus.Add(new GpuInfo(idx, name, adapterRam));
                    idx++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query GPU info via WMI");
            }

            return new HardwareInfo(cpu, gpus);
        }, cancellationToken);
    }
}

