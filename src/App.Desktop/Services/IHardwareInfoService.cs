using System.Collections.ObjectModel;

namespace Lazarus.Desktop.Services;

public sealed record CpuInfo(string Name, int PhysicalCores, int LogicalProcessors);
public sealed record GpuInfo(int Index, string Name, ulong AdapterRamBytes);
public sealed record HardwareInfo(CpuInfo? Cpu, IReadOnlyList<GpuInfo> Gpus);

public interface IHardwareInfoService
{
    Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken cancellationToken = default);
}

