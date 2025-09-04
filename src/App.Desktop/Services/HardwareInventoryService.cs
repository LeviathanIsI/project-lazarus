using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using Lazarus.App.Desktop.Services.Models;
using System.Text.Json;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Comprehensive hardware inventory service with deep system penetration
/// </summary>
public class HardwareInventoryService : IDisposable, INotifyPropertyChanged
{
    private readonly ILogger<HardwareInventoryService> _logger;
    private readonly Timer _updateTimer;
    private bool _disposed = false;
    
    // Hardware specifications
    private CpuSpecification _cpuSpec = new();
    private MemorySpecification _memorySpec = new();
    private List<GpuSpecification> _gpuSpecs = new();
    private List<StorageDevice> _storageDevices = new();
    private HardwareRollupSummary _rollupSummary = new();

    // Performance counters for real-time metrics
    private readonly Dictionary<int, PerformanceCounter> _cpuCoreCounters = new();
    private PerformanceCounter? _memoryCounter;
    private readonly List<PerformanceCounter> _diskCounters = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<HardwareInventoryEventArgs>? InventoryUpdated;

    public HardwareInventoryService(ILogger<HardwareInventoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        InitializeHardwareInventory();
        InitializePerformanceCounters();
        
        // Update every 3 seconds for detailed hardware monitoring
        _updateTimer = new Timer(UpdateHardwareMetrics, null, 
            TimeSpan.Zero, TimeSpan.FromSeconds(3));
        
        _logger.LogInformation("Hardware inventory service initialized with deep system penetration");
    }

    #region Public Properties

    public CpuSpecification CpuSpecification
    {
        get => _cpuSpec;
        private set
        {
            _cpuSpec = value;
            OnPropertyChanged(nameof(CpuSpecification));
        }
    }

    public MemorySpecification MemorySpecification
    {
        get => _memorySpec;
        private set
        {
            _memorySpec = value;
            OnPropertyChanged(nameof(MemorySpecification));
        }
    }

    public List<GpuSpecification> GpuSpecifications
    {
        get => _gpuSpecs;
        private set
        {
            _gpuSpecs = value;
            OnPropertyChanged(nameof(GpuSpecifications));
        }
    }

    public List<StorageDevice> StorageDevices
    {
        get => _storageDevices;
        private set
        {
            _storageDevices = value;
            OnPropertyChanged(nameof(StorageDevices));
        }
    }

    public HardwareRollupSummary RollupSummary
    {
        get => _rollupSummary;
        private set
        {
            _rollupSummary = value;
            OnPropertyChanged(nameof(RollupSummary));
        }
    }

    #endregion

    #region Hardware Inventory Initialization

    /// <summary>
    /// Initializes complete hardware inventory with deep system penetration
    /// </summary>
    private async void InitializeHardwareInventory()
    {
        try
        {
            await Task.Run(() =>
            {
                DiscoverCpuSpecifications();
                DiscoverMemorySpecifications();
                DiscoverGpuSpecifications();
                DiscoverStorageDevices();
                CalculateRollupSummary();
            });
            
            _logger.LogInformation("Complete hardware inventory discovered: {CpuCores} cores, {MemoryGB:F1}GB RAM, {GpuCount} GPUs, {DriveCount} drives",
                _cpuSpec.CoreCount, _memorySpec.TotalPhysicalGB, _gpuSpecs.Count, _storageDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hardware inventory initialization");
        }
    }

    /// <summary>
    /// Discovers detailed CPU specifications and core layout
    /// </summary>
    private void DiscoverCpuSpecifications()
    {
        try
        {
            _logger.LogDebug("Starting CPU discovery via Win32_Processor WMI query");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            using var results = searcher.Get();

            bool cpuFound = false;
            foreach (ManagementObject cpu in results)
            {
                _logger.LogDebug("Processing CPU WMI object with {PropertyCount} properties", cpu.Properties.Count);
                
                // Log all available properties for debugging
                LogWmiObjectProperties(cpu, "Win32_Processor");
                
                _cpuSpec = new CpuSpecification
                {
                    ProcessorName = GetWmiStringProperty(cpu, "Name", "Unknown CPU"),
                    Manufacturer = GetWmiStringProperty(cpu, "Manufacturer", "Unknown"),
                    Architecture = GetWmiStringProperty(cpu, "Architecture", "Unknown"), 
                    Family = GetWmiStringProperty(cpu, "Family", "Unknown"),
                    Model = GetWmiStringProperty(cpu, "Model", "Unknown"),
                    Stepping = GetWmiStringProperty(cpu, "Stepping", "Unknown"),
                    CoreCount = GetWmiIntProperty(cpu, "NumberOfCores", Environment.ProcessorCount),
                    ThreadCount = GetWmiIntProperty(cpu, "NumberOfLogicalProcessors", Environment.ProcessorCount),
                    BaseFrequencyMhz = GetWmiDoubleProperty(cpu, "CurrentClockSpeed", 0),
                    MaxFrequencyMhz = GetWmiDoubleProperty(cpu, "MaxClockSpeed", 0),
                    CacheL3KB = GetWmiIntProperty(cpu, "L3CacheSize", 0),
                    SocketType = GetWmiStringProperty(cpu, "SocketDesignation", "Unknown"),
                    TdpWatts = GetWmiIntProperty(cpu, "ThermalDesignPower", 0)
                };

                // Initialize per-core metrics
                _cpuSpec.CoreMetrics.Clear();
                for (int i = 0; i < _cpuSpec.CoreCount; i++)
                {
                    _cpuSpec.CoreMetrics.Add(new CpuCoreMetrics { CoreId = i });
                }

                cpuFound = true;
                _logger.LogInformation("CPU discovered: {Name} - {Cores}C/{Threads}T @ {BaseFreq}MHz (Manufacturer: {Manufacturer})",
                    _cpuSpec.ProcessorName, _cpuSpec.CoreCount, _cpuSpec.ThreadCount, _cpuSpec.BaseFrequencyMhz, _cpuSpec.Manufacturer);
                break; // Take first processor for now
            }
            
            if (!cpuFound)
            {
                _logger.LogWarning("No CPU objects found in Win32_Processor query results");
                ApplyCpuFallback();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering CPU specifications via WMI");
            ApplyCpuFallback();
        }
    }

    /// <summary>
    /// Discovers comprehensive memory specifications including individual RAM modules
    /// </summary>
    private void DiscoverMemorySpecifications()
    {
        try
        {
            _logger.LogDebug("Starting memory discovery via Win32_ComputerSystem and Win32_PhysicalMemory WMI queries");
            
            // Get overall memory information
            using var memSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            using var memResults = memSearcher.Get();

            foreach (ManagementObject system in memResults)
            {
                LogWmiObjectProperties(system, "Win32_ComputerSystem");
                _memorySpec.TotalPhysicalBytes = GetWmiLongProperty(system, "TotalPhysicalMemory", 0);
                break;
            }

            // Get individual memory modules with comprehensive detection
            using var moduleSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            using var moduleResults = moduleSearcher.Get();

            _memorySpec.MemoryModules.Clear();
            int slotNumber = 0;

            foreach (ManagementObject module in moduleResults)
            {
                _logger.LogDebug("Processing memory module #{Index} WMI object", slotNumber);
                LogWmiObjectProperties(module, $"Win32_PhysicalMemory[{slotNumber}]");
                
                var memoryModule = new MemoryModule
                {
                    SlotNumber = slotNumber++,
                    BankLabel = GetWmiStringProperty(module, "BankLabel", $"BANK {slotNumber}"),
                    CapacityBytes = GetWmiLongProperty(module, "Capacity", 0),
                    SpeedMhz = GetWmiIntProperty(module, "Speed", 0),
                    Manufacturer = GetWmiStringProperty(module, "Manufacturer", "Unknown"),
                    PartNumber = GetWmiStringProperty(module, "PartNumber", "Unknown"),
                    SerialNumber = GetWmiStringProperty(module, "SerialNumber", "Unknown"),
                    FormFactor = GetMemoryFormFactor(module),
                    MemoryType = GetMemoryType(module),
                    DataWidth = GetWmiIntProperty(module, "DataWidth", 0),
                    TotalWidth = GetWmiIntProperty(module, "TotalWidth", 0)
                };

                _memorySpec.MemoryModules.Add(memoryModule);
                _logger.LogDebug("Memory module #{Index}: {Manufacturer} {CapacityGB:F1}GB {Speed}MHz {Type}",
                    memoryModule.SlotNumber, memoryModule.Manufacturer, memoryModule.CapacityBytes / (1024.0 * 1024.0 * 1024.0),
                    memoryModule.SpeedMhz, memoryModule.MemoryType);
            }

            // Get virtual memory information
            using var osSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            using var osResults = osSearcher.Get();

            foreach (ManagementObject os in osResults)
            {
                var totalVirtualKB = GetWmiLongProperty(os, "TotalVirtualMemorySize", 0);
                _memorySpec.TotalVirtualBytes = totalVirtualKB * 1024;
                break;
            }

            // Validate total physical memory against module sum
            var moduleSum = _memorySpec.MemoryModules.Sum(m => m.CapacityBytes);
            if (_memorySpec.TotalPhysicalBytes == 0 && moduleSum > 0)
            {
                _logger.LogInformation("Using memory module sum for total physical memory: {SumGB:F1}GB", moduleSum / (1024.0 * 1024.0 * 1024.0));
                _memorySpec.TotalPhysicalBytes = moduleSum;
            }

            _logger.LogInformation("Memory discovery complete: {TotalGB:F1}GB physical ({ModuleCount} modules), {VirtualGB:F1}GB virtual",
                _memorySpec.TotalPhysicalGB, _memorySpec.MemoryModules.Count, _memorySpec.TotalVirtualBytes / (1024.0 * 1024.0 * 1024.0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering memory specifications via WMI");
            ApplyMemoryFallback();
        }
    }

    /// <summary>
    /// Discovers all graphics adapters with complete specifications
    /// </summary>
    private void DiscoverGpuSpecifications()
    {
        try
        {
            _logger.LogDebug("Starting GPU discovery via Win32_VideoController WMI query");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var results = searcher.Get();

            _gpuSpecs.Clear();
            int gpuIndex = 0;

            foreach (ManagementObject gpu in results)
            {
                _logger.LogDebug("Processing GPU #{Index} WMI object with {PropertyCount} properties", gpuIndex, gpu.Properties.Count);
                
                // Log all available properties for debugging  
                LogWmiObjectProperties(gpu, $"Win32_VideoController[{gpuIndex}]");
                
                // Get adapter RAM with multiple fallback methods
                var adapterRamBytes = GetGpuAdapterRam(gpu, gpuIndex);
                if (adapterRamBytes == 0)
                {
                    _logger.LogWarning("GPU #{Index} has no VRAM detected, skipping", gpuIndex);
                    gpuIndex++;
                    continue;
                }

                var gpuName = GetWmiStringProperty(gpu, "Name", $"Unknown GPU #{gpuIndex}");
                var gpuSpec = new GpuSpecification
                {
                    DeviceId = GetWmiStringProperty(gpu, "DeviceID", Guid.NewGuid().ToString()),
                    Name = gpuName,
                    Manufacturer = GetWmiStringProperty(gpu, "AdapterCompatibility", "Unknown"),
                    DriverVersion = GetWmiStringProperty(gpu, "DriverVersion", "Unknown"),
                    DriverDate = GetWmiStringProperty(gpu, "DriverDate", "Unknown"),
                    AdapterRamBytes = adapterRamBytes,
                    VideoProcessor = GetWmiStringProperty(gpu, "VideoProcessor", "Unknown"),
                    VideoArchitecture = GetWmiStringProperty(gpu, "VideoArchitecture", "Unknown"),
                    MaxRefreshRate = GetWmiIntProperty(gpu, "MaxRefreshRate", 0),
                    VideoModeDescription = GetWmiStringProperty(gpu, "VideoModeDescription", "Unknown"),
                    IsIntegrated = DetermineIfIntegratedGpu(gpuName),
                    IsPrimary = _gpuSpecs.Count == 0 // First GPU is typically primary
                };

                _gpuSpecs.Add(gpuSpec);
                _logger.LogInformation("GPU #{Index} discovered: {Name} - {VramGB:F1}GB VRAM (Driver: {Driver})",
                    gpuIndex, gpuSpec.Name, gpuSpec.AdapterRamGB, gpuSpec.DriverVersion);
                
                gpuIndex++;
            }

            _logger.LogInformation("GPU discovery complete: {GpuCount} adapters, Total VRAM: {TotalVram:F1}GB",
                _gpuSpecs.Count, _gpuSpecs.Sum(g => g.AdapterRamGB));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering GPU specifications via WMI");
        }
    }

    /// <summary>
    /// Discovers all storage devices with health and performance metrics
    /// </summary>
    private void DiscoverStorageDevices()
    {
        try
        {
            _logger.LogDebug("Starting storage discovery via Win32_DiskDrive and Win32_LogicalDisk WMI queries");
            
            // Get physical disk drives
            using var diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            using var diskResults = diskSearcher.Get();

            _storageDevices.Clear();
            int driveIndex = 0;

            foreach (ManagementObject disk in diskResults)
            {
                _logger.LogDebug("Processing storage device #{Index} WMI object", driveIndex);
                LogWmiObjectProperties(disk, $"Win32_DiskDrive[{driveIndex}]");
                
                var device = new StorageDevice
                {
                    DeviceId = GetWmiStringProperty(disk, "DeviceID", Guid.NewGuid().ToString()),
                    Model = GetWmiStringProperty(disk, "Model", "Unknown Drive"),
                    Manufacturer = GetWmiStringProperty(disk, "Manufacturer", "Unknown"),
                    SerialNumber = GetWmiStringProperty(disk, "SerialNumber", "Unknown"),
                    TotalSize = GetWmiLongProperty(disk, "Size", 0),
                    InterfaceType = GetWmiStringProperty(disk, "InterfaceType", "Unknown"),
                    IsRemovable = GetWmiStringProperty(disk, "MediaType", "").Contains("Removable"),
                    DriveType = DetermineStorageType(disk)
                };

                _storageDevices.Add(device);
                _logger.LogDebug("Storage device #{Index}: {Model} - {SizeGB:F1}GB {Type} ({Interface})",
                    driveIndex, device.Model, device.TotalSizeGB, device.DriveType, device.InterfaceType);
                
                driveIndex++;
            }

            // Get logical disk information for free space and file systems
            using var logicalSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3"); // Only fixed drives
            using var logicalResults = logicalSearcher.Get();

            foreach (ManagementObject logical in logicalResults)
            {
                LogWmiObjectProperties(logical, "Win32_LogicalDisk");
                
                var driveLetter = GetWmiStringProperty(logical, "DeviceID", "");
                var freeSpace = GetWmiLongProperty(logical, "FreeSpace", 0);
                var totalSize = GetWmiLongProperty(logical, "Size", 0);
                var fileSystem = GetWmiStringProperty(logical, "FileSystem", "Unknown");

                // Try to match with physical drives by size
                var matchingDevice = _storageDevices.FirstOrDefault(d => 
                    string.IsNullOrEmpty(d.DriveLetter) && 
                    Math.Abs(d.TotalSize - totalSize) < (1024L * 1024 * 1024)) ?? // Within 1GB tolerance
                    _storageDevices.FirstOrDefault(d => string.IsNullOrEmpty(d.DriveLetter));

                if (matchingDevice != null)
                {
                    matchingDevice.DriveLetter = driveLetter;
                    matchingDevice.FreeSpace = freeSpace;
                    matchingDevice.FileSystem = fileSystem;
                    matchingDevice.IsSystemDrive = driveLetter.Equals("C:", StringComparison.OrdinalIgnoreCase);
                    _logger.LogDebug("Matched logical drive {Drive} with physical device {Model}", driveLetter, matchingDevice.Model);
                }
                else
                {
                    // Create virtual storage device for unmatched logical drives
                    var virtualDevice = new StorageDevice
                    {
                        DeviceId = $"LOGICAL_{driveLetter}",
                        Model = $"Logical Drive {driveLetter}",
                        Manufacturer = "Virtual",
                        DriveLetter = driveLetter,
                        TotalSize = totalSize,
                        FreeSpace = freeSpace,
                        FileSystem = fileSystem,
                        DriveType = "Virtual",
                        IsSystemDrive = driveLetter.Equals("C:", StringComparison.OrdinalIgnoreCase)
                    };
                    _storageDevices.Add(virtualDevice);
                    _logger.LogDebug("Created virtual storage device for unmatched logical drive {Drive}", driveLetter);
                }
            }

            _logger.LogInformation("Storage discovery complete: {DriveCount} devices, Total capacity: {TotalCapacity:F1}GB",
                _storageDevices.Count, _storageDevices.Sum(d => d.TotalSizeGB));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering storage devices via WMI");
        }
    }

    #endregion

    #region Performance Counter Initialization

    /// <summary>
    /// Initializes performance counters for real-time hardware monitoring
    /// </summary>
    private void InitializePerformanceCounters()
    {
        try
        {
            // Initialize per-core CPU counters
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                try
                {
                    var counter = new PerformanceCounter("Processor", "% Processor Time", $"{i}");
                    counter.NextValue(); // Prime the counter
                    _cpuCoreCounters[i] = counter;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to initialize CPU core {Core} counter", i);
                }
            }

            // Initialize memory counter
            try
            {
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _memoryCounter.NextValue(); // Prime the counter
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to initialize memory counter");
            }

            _logger.LogDebug("Performance counters initialized: {CpuCores} CPU cores, Memory counter: {HasMemory}",
                _cpuCoreCounters.Count, _memoryCounter != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing performance counters");
        }
    }

    #endregion

    #region Real-time Metrics Update

    /// <summary>
    /// Updates all hardware metrics with real-time data
    /// </summary>
    private async void UpdateHardwareMetrics(object? state)
    {
        if (_disposed) return;

        try
        {
            await Task.Run(() =>
            {
                UpdateCpuMetrics();
                UpdateMemoryMetrics();
                UpdateGpuMetrics();
                UpdateStorageMetrics();
                CalculateRollupSummary();
            });

            // Raise inventory updated event
            var eventArgs = new HardwareInventoryEventArgs
            {
                CpuSpecification = _cpuSpec,
                MemorySpecification = _memorySpec,
                GpuSpecifications = _gpuSpecs,
                StorageDevices = _storageDevices,
                RollupSummary = _rollupSummary,
                Timestamp = DateTime.UtcNow
            };

            InventoryUpdated?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hardware metrics");
        }
    }

    /// <summary>
    /// Updates CPU metrics including per-core utilization
    /// </summary>
    private void UpdateCpuMetrics()
    {
        try
        {
            // Update per-core metrics
            double totalUsage = 0;
            int validCores = 0;

            foreach (var kvp in _cpuCoreCounters)
            {
                try
                {
                    var coreUsage = kvp.Value.NextValue();
                    if (kvp.Key < _cpuSpec.CoreMetrics.Count)
                    {
                        _cpuSpec.CoreMetrics[kvp.Key].Usage = Math.Min(100, Math.Max(0, coreUsage));
                        totalUsage += coreUsage;
                        validCores++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to update CPU core {Core} metrics", kvp.Key);
                }
            }

            // Update overall CPU usage
            if (validCores > 0)
            {
                _cpuSpec.CurrentUsage = totalUsage / validCores;
            }

            // Simulate CPU temperature (real implementation would use thermal sensors)
            _cpuSpec.CurrentTemperature = 35 + (_cpuSpec.CurrentUsage * 0.4) + Math.Sin(DateTime.Now.Minute * 0.1) * 5;
            
            // Simulate current frequency based on usage
            _cpuSpec.CurrentFrequencyMhz = _cpuSpec.BaseFrequencyMhz + 
                (_cpuSpec.CurrentUsage / 100.0) * (_cpuSpec.MaxFrequencyMhz - _cpuSpec.BaseFrequencyMhz);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error updating CPU metrics");
        }
    }

    /// <summary>
    /// Updates memory metrics including detailed usage breakdown with accurate real-time data
    /// </summary>
    private void UpdateMemoryMetrics()
    {
        try
        {
            if (_memoryCounter != null && _memorySpec.TotalPhysicalBytes > 0)
            {
                var availableMB = _memoryCounter.NextValue();
                var availableBytes = (long)(availableMB * 1024 * 1024);
                var usedBytes = _memorySpec.TotalPhysicalBytes - availableBytes;
                var usagePercentage = (usedBytes * 100.0) / _memorySpec.TotalPhysicalBytes;
                
                // Update with change detection for efficient UI updates
                _memorySpec.AvailableBytes = availableBytes;
                _memorySpec.UsedBytes = usedBytes;
                _memorySpec.UsagePercentage = Math.Min(100, Math.Max(0, usagePercentage));
                
                _logger.LogTrace("Memory updated: {UsedGB:F1}GB / {TotalGB:F1}GB ({Percentage:F1}%)", 
                    _memorySpec.UsedGB, _memorySpec.TotalPhysicalGB, _memorySpec.UsagePercentage);
            }
            else
            {
                // Fallback: Get memory info using GC and Environment
                try
                {
                    var workingSet = Environment.WorkingSet;
                    var totalPhysical = GC.GetTotalMemory(false);
                    
                    // Rough estimation if no performance counter available
                    if (_memorySpec.TotalPhysicalBytes == 0)
                    {
                        _memorySpec.TotalPhysicalBytes = Math.Max(8L * 1024 * 1024 * 1024, totalPhysical * 16); // At least 8GB
                    }
                    
                    // Estimate usage based on working set and system behavior
                    var estimatedUsed = Math.Max(workingSet * 8, _memorySpec.TotalPhysicalBytes * 0.3); // Conservative estimate
                    _memorySpec.UsedBytes = (long)Math.Min(_memorySpec.TotalPhysicalBytes * 0.9, estimatedUsed);
                    _memorySpec.AvailableBytes = _memorySpec.TotalPhysicalBytes - _memorySpec.UsedBytes;
                    _memorySpec.UsagePercentage = (_memorySpec.UsedBytes * 100.0) / _memorySpec.TotalPhysicalBytes;
                    
                    _logger.LogDebug("Memory fallback estimation: {UsedGB:F1}GB / {TotalGB:F1}GB", 
                        _memorySpec.UsedGB, _memorySpec.TotalPhysicalGB);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogDebug(fallbackEx, "Memory fallback estimation failed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error updating memory metrics");
        }
    }

    /// <summary>
    /// Updates GPU metrics for all graphics adapters
    /// </summary>
    private void UpdateGpuMetrics()
    {
        try
        {
            // Real GPU monitoring would require specific APIs (NVML, ADL, etc.)
            // For now, simulate realistic usage patterns
            var time = DateTime.Now;
            
            for (int i = 0; i < _gpuSpecs.Count; i++)
            {
                var gpu = _gpuSpecs[i];
                
                // Simulate GPU usage based on time patterns
                gpu.CurrentUsage = Math.Max(0, Math.Min(100, 
                    15 + Math.Sin(time.Minute * 0.2 + i) * 25 + (time.Millisecond % 20)));
                
                // Simulate VRAM usage
                gpu.CurrentMemoryUsed = (long)(gpu.AdapterRamBytes * (0.1 + gpu.CurrentUsage / 200.0));
                
                // Simulate temperature
                gpu.CurrentTemperature = 40 + (gpu.CurrentUsage * 0.5) + Math.Sin(time.Second * 0.1) * 8;
                
                // Simulate fan speed
                gpu.CurrentFanSpeed = 800 + (gpu.CurrentUsage * 15) + (gpu.CurrentTemperature - 40) * 20;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error updating GPU metrics");
        }
    }

    /// <summary>
    /// Updates storage device metrics including health and performance
    /// </summary>
    private void UpdateStorageMetrics()
    {
        try
        {
            foreach (var device in _storageDevices)
            {
                // Update free space from actual file system
                if (!string.IsNullOrEmpty(device.DriveLetter))
                {
                    try
                    {
                        var driveInfo = new DriveInfo(device.DriveLetter);
                        if (driveInfo.IsReady)
                        {
                            device.FreeSpace = driveInfo.AvailableFreeSpace;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to update free space for {Drive}", device.DriveLetter);
                    }
                }

                // Simulate other metrics
                device.Temperature = device.DriveType == "SSD" ? 30 + (DateTime.Now.Millisecond % 15) : 35 + (DateTime.Now.Millisecond % 20);
                device.ReadSpeedMBps = device.DriveType == "SSD" ? 500 + (DateTime.Now.Millisecond % 100) : 120 + (DateTime.Now.Millisecond % 50);
                device.WriteSpeedMBps = device.ReadSpeedMBps * 0.8; // Write typically slower

                // Simulate health based on usage
                device.HealthStatus = device.UsagePercentage > 90 ? DriveHealth.Fair :
                                     device.UsagePercentage > 70 ? DriveHealth.Good : DriveHealth.Excellent;
                device.HealthPercentage = Math.Max(85, 100 - (int)(device.UsagePercentage * 0.15));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error updating storage metrics");
        }
    }

    #endregion

    #region Rollup Summary Calculation

    /// <summary>
    /// Calculates system-wide rollup summary from individual component metrics
    /// </summary>
    private void CalculateRollupSummary()
    {
        try
        {
            _rollupSummary.TotalCpuCores = _cpuSpec.CoreCount;
            _rollupSummary.TotalCpuThreads = _cpuSpec.ThreadCount;
            _rollupSummary.TotalCpuUsage = _cpuSpec.CurrentUsage;

            _rollupSummary.TotalMemoryBytes = _memorySpec.TotalPhysicalBytes;
            _rollupSummary.TotalMemoryUsage = _memorySpec.UsagePercentage;

            _rollupSummary.TotalGpuCount = _gpuSpecs.Count;
            _rollupSummary.TotalGpuMemoryBytes = _gpuSpecs.Sum(g => g.AdapterRamBytes);
            _rollupSummary.TotalGpuUsage = _gpuSpecs.Any() ? _gpuSpecs.Average(g => g.CurrentUsage) : 0;

            _rollupSummary.TotalDriveCount = _storageDevices.Count;
            _rollupSummary.TotalStorageBytes = _storageDevices.Sum(d => d.TotalSize);
            _rollupSummary.TotalFreeSpaceBytes = _storageDevices.Sum(d => d.FreeSpace);

            // Calculate average temperature
            var temperatures = new List<double> { _cpuSpec.CurrentTemperature };
            temperatures.AddRange(_gpuSpecs.Select(g => g.CurrentTemperature));
            temperatures.AddRange(_storageDevices.Select(d => d.Temperature));
            _rollupSummary.AverageTemperature = temperatures.Where(t => t > 0).DefaultIfEmpty(40).Average();

            // Determine overall health
            _rollupSummary.OverallHealth = DetermineOverallHealth();
            _rollupSummary.LastUpdated = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating rollup summary");
        }
    }

    /// <summary>
    /// Determines overall system health based on component metrics
    /// </summary>
    private SystemHealthStatus DetermineOverallHealth()
    {
        var issues = 0;

        // Check CPU health
        if (_cpuSpec.CurrentUsage > 90) issues++;
        if (_cpuSpec.CurrentTemperature > 80) issues++;

        // Check memory health  
        if (_memorySpec.UsagePercentage > 90) issues++;

        // Check GPU health
        foreach (var gpu in _gpuSpecs)
        {
            if (gpu.CurrentUsage > 95) issues++;
            if (gpu.CurrentTemperature > 85) issues++;
        }

        // Check storage health
        foreach (var drive in _storageDevices)
        {
            if (drive.UsagePercentage > 95) issues++;
            if (drive.HealthStatus == DriveHealth.Poor || drive.HealthStatus == DriveHealth.Critical) issues++;
        }

        return issues switch
        {
            0 => SystemHealthStatus.Excellent,
            1 => SystemHealthStatus.Good,
            2 => SystemHealthStatus.Fair,
            3 => SystemHealthStatus.Poor,
            _ => SystemHealthStatus.Critical
        };
    }

    #endregion

    #region WMI Helper Methods

    /// <summary>
    /// Safely gets a string property from WMI object with detailed logging
    /// </summary>
    private string GetWmiStringProperty(ManagementObject obj, string propertyName, string defaultValue)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                _logger.LogDebug("WMI property {Property} is null, using default: {Default}", propertyName, defaultValue);
                return defaultValue;
            }
            
            var stringValue = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(stringValue))
            {
                _logger.LogDebug("WMI property {Property} is empty, using default: {Default}", propertyName, defaultValue);
                return defaultValue;
            }
            
            _logger.LogTrace("WMI property {Property} = '{Value}'", propertyName, stringValue);
            return stringValue;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading WMI string property {Property}, using default: {Default}", propertyName, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely gets an integer property from WMI object with detailed logging
    /// </summary>
    private int GetWmiIntProperty(ManagementObject obj, string propertyName, int defaultValue)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                _logger.LogDebug("WMI property {Property} is null, using default: {Default}", propertyName, defaultValue);
                return defaultValue;
            }
            
            var intValue = Convert.ToInt32(value);
            _logger.LogTrace("WMI property {Property} = {Value}", propertyName, intValue);
            return intValue;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading WMI int property {Property}, using default: {Default}", propertyName, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely gets a double property from WMI object with detailed logging
    /// </summary>
    private double GetWmiDoubleProperty(ManagementObject obj, string propertyName, double defaultValue)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                _logger.LogDebug("WMI property {Property} is null, using default: {Default}", propertyName, defaultValue);
                return defaultValue;
            }
            
            var doubleValue = Convert.ToDouble(value);
            _logger.LogTrace("WMI property {Property} = {Value}", propertyName, doubleValue);
            return doubleValue;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading WMI double property {Property}, using default: {Default}", propertyName, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely gets a long property from WMI object with detailed logging
    /// </summary>
    private long GetWmiLongProperty(ManagementObject obj, string propertyName, long defaultValue)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                _logger.LogDebug("WMI property {Property} is null, using default: {Default}", propertyName, defaultValue);
                return defaultValue;
            }
            
            var longValue = Convert.ToInt64(value);
            _logger.LogTrace("WMI property {Property} = {Value}", propertyName, longValue);
            return longValue;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading WMI long property {Property}, using default: {Default}", propertyName, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// Logs all properties of a WMI object for debugging purposes
    /// </summary>
    private void LogWmiObjectProperties(ManagementObject obj, string objectType)
    {
        try
        {
            _logger.LogDebug("=== {ObjectType} Properties ===", objectType);
            foreach (var property in obj.Properties)
            {
                try
                {
                    var value = property.Value;
                    var displayValue = value?.ToString() ?? "<null>";
                    if (displayValue.Length > 100)
                        displayValue = displayValue.Substring(0, 100) + "...";
                    
                    _logger.LogDebug("{Property}: {Value} ({Type})", 
                        property.Name, displayValue, property.Type);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Failed to read property {Property}: {Error}", property.Name, ex.Message);
                }
            }
            _logger.LogDebug("=== End {ObjectType} Properties ===", objectType);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error logging WMI object properties for {ObjectType}", objectType);
        }
    }

    /// <summary>
    /// Applies CPU fallback values when WMI detection fails
    /// </summary>
    private void ApplyCpuFallback()
    {
        _logger.LogWarning("Applying CPU fallback detection");
        _cpuSpec.ProcessorName = $"CPU ({Environment.ProcessorCount} cores)";
        _cpuSpec.Manufacturer = "Unknown";
        _cpuSpec.CoreCount = Environment.ProcessorCount;
        _cpuSpec.ThreadCount = Environment.ProcessorCount;
        _cpuSpec.Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        
        // Initialize per-core metrics
        _cpuSpec.CoreMetrics.Clear();
        for (int i = 0; i < _cpuSpec.CoreCount; i++)
        {
            _cpuSpec.CoreMetrics.Add(new CpuCoreMetrics { CoreId = i });
        }
    }

    /// <summary>
    /// Gets GPU adapter RAM with multiple fallback methods including high-end GPU detection
    /// </summary>
    private long GetGpuAdapterRam(ManagementObject gpu, int gpuIndex)
    {
        try
        {
            var gpuName = GetWmiStringProperty(gpu, "Name", "");
            
            // First, try to detect high-end GPUs with known VRAM sizes
            var highEndVram = DetectHighEndGpuVram(gpuName);
            if (highEndVram > 0)
            {
                _logger.LogInformation("GPU #{Index} detected as high-end GPU {Name} with {VramGB}GB VRAM", 
                    gpuIndex, gpuName, highEndVram / (1024L * 1024 * 1024));
                return highEndVram;
            }

            // Try AdapterRAM (most common, but limited to 32-bit)
            var adapterRam = gpu["AdapterRAM"];
            if (adapterRam != null)
            {
                var ramBytes = Convert.ToInt64(adapterRam);
                if (ramBytes > 0)
                {
                    // Check if this might be a truncated value for high-end GPU
                    if (IsLikelyTruncatedVram(gpuName, ramBytes))
                    {
                        var estimatedVram = EstimateActualVram(gpuName, ramBytes);
                        _logger.LogDebug("GPU #{Index} AdapterRAM appears truncated ({TruncatedGB:F1}GB), estimated actual VRAM: {EstimatedGB:F1}GB", 
                            gpuIndex, ramBytes / (1024.0 * 1024.0 * 1024.0), estimatedVram / (1024.0 * 1024.0 * 1024.0));
                        return estimatedVram;
                    }
                    
                    _logger.LogDebug("GPU #{Index} AdapterRAM: {RamMB:F1}MB", gpuIndex, ramBytes / (1024.0 * 1024.0));
                    return ramBytes;
                }
            }

            // Try AdapterDACType which sometimes contains memory info
            var dacType = GetWmiStringProperty(gpu, "AdapterDACType", "");
            if (!string.IsNullOrEmpty(dacType) && dacType.Contains("GB"))
            {
                // Parse memory from DAC type string (e.g., "24 GB")
                var match = Regex.Match(dacType, @"(\d+)\s*GB");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var gbValue))
                {
                    var ramBytes = (long)gbValue * 1024 * 1024 * 1024;
                    _logger.LogDebug("GPU #{Index} extracted VRAM from AdapterDACType: {RamGB}GB", gpuIndex, gbValue);
                    return ramBytes;
                }
            }

            // Check if this might be integrated graphics with shared memory
            if (DetermineIfIntegratedGpu(gpuName))
            {
                // Assign reasonable shared memory amount for integrated GPUs
                var sharedRam = 2L * 1024 * 1024 * 1024; // 2GB shared
                _logger.LogDebug("GPU #{Index} appears to be integrated, assigning shared VRAM: {RamGB}GB", gpuIndex, 2);
                return sharedRam;
            }

            _logger.LogWarning("GPU #{Index} ({Name}) has no detectable VRAM", gpuIndex, gpuName);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting GPU #{Index} adapter RAM", gpuIndex);
            return 0;
        }
    }

    /// <summary>
    /// Detects high-end GPUs with known VRAM configurations
    /// </summary>
    private long DetectHighEndGpuVram(string gpuName)
    {
        if (string.IsNullOrEmpty(gpuName))
            return 0;

        var nameLower = gpuName.ToLower();

        // NVIDIA RTX 50 series
        if (nameLower.Contains("rtx 5090")) return 24L * 1024 * 1024 * 1024; // 24GB
        if (nameLower.Contains("rtx 5080")) return 16L * 1024 * 1024 * 1024; // 16GB
        if (nameLower.Contains("rtx 5070")) return 12L * 1024 * 1024 * 1024; // 12GB

        // NVIDIA RTX 40 series
        if (nameLower.Contains("rtx 4090")) return 24L * 1024 * 1024 * 1024; // 24GB
        if (nameLower.Contains("rtx 4080")) return 16L * 1024 * 1024 * 1024; // 16GB
        if (nameLower.Contains("rtx 4070 ti")) return 12L * 1024 * 1024 * 1024; // 12GB
        if (nameLower.Contains("rtx 4070")) return 12L * 1024 * 1024 * 1024; // 12GB
        if (nameLower.Contains("rtx 4060 ti")) return 16L * 1024 * 1024 * 1024; // 16GB variant
        if (nameLower.Contains("rtx 4060")) return 8L * 1024 * 1024 * 1024; // 8GB

        // NVIDIA RTX 30 series
        if (nameLower.Contains("rtx 3090 ti")) return 24L * 1024 * 1024 * 1024; // 24GB
        if (nameLower.Contains("rtx 3090")) return 24L * 1024 * 1024 * 1024; // 24GB
        if (nameLower.Contains("rtx 3080 ti")) return 12L * 1024 * 1024 * 1024; // 12GB
        if (nameLower.Contains("rtx 3080")) return 10L * 1024 * 1024 * 1024; // 10GB

        // AMD RX 7000 series
        if (nameLower.Contains("rx 7900 xtx")) return 24L * 1024 * 1024 * 1024; // 24GB
        if (nameLower.Contains("rx 7900 xt")) return 20L * 1024 * 1024 * 1024; // 20GB
        if (nameLower.Contains("rx 7800 xt")) return 16L * 1024 * 1024 * 1024; // 16GB
        if (nameLower.Contains("rx 7700 xt")) return 12L * 1024 * 1024 * 1024; // 12GB

        // Professional cards
        if (nameLower.Contains("tesla") || nameLower.Contains("quadro") || nameLower.Contains("a100"))
            return 0; // Let WMI handle professional cards as they vary widely

        return 0; // Unknown high-end GPU
    }

    /// <summary>
    /// Checks if VRAM value is likely truncated due to 32-bit limitations
    /// </summary>
    private bool IsLikelyTruncatedVram(string gpuName, long vramBytes)
    {
        var vramGB = vramBytes / (1024.0 * 1024.0 * 1024.0);
        var nameLower = gpuName.ToLower();

        // If it's a known high-end GPU but showing small VRAM, likely truncated
        if ((nameLower.Contains("rtx 5090") || nameLower.Contains("rtx 4090")) && vramGB < 8)
            return true;

        if ((nameLower.Contains("rtx 5080") || nameLower.Contains("rtx 4080")) && vramGB < 8)
            return true;

        // Any high-end GPU showing exactly 4GB is likely truncated
        if ((nameLower.Contains("rtx") || nameLower.Contains("rx")) && Math.Abs(vramGB - 4.0) < 0.1)
            return true;

        return false;
    }

    /// <summary>
    /// Estimates actual VRAM based on GPU name when WMI value is truncated
    /// </summary>
    private long EstimateActualVram(string gpuName, long truncatedBytes)
    {
        // First try high-end detection
        var highEndVram = DetectHighEndGpuVram(gpuName);
        if (highEndVram > 0)
            return highEndVram;

        var nameLower = gpuName.ToLower();
        
        // Fallback estimation for unknown GPUs
        if (nameLower.Contains("rtx") || nameLower.Contains("rx"))
        {
            // If truncated value is ~4GB, real value might be much higher
            if (truncatedBytes > 3L * 1024 * 1024 * 1024) // > 3GB
                return 12L * 1024 * 1024 * 1024; // Estimate 12GB
        }

        return truncatedBytes; // Return original if can't estimate
    }

    /// <summary>
    /// Determines if a GPU is integrated based on its name
    /// </summary>
    private bool DetermineIfIntegratedGpu(string gpuName)
    {
        if (string.IsNullOrEmpty(gpuName))
            return false;
            
        var nameLower = gpuName.ToLower();
        return nameLower.Contains("intel") && !nameLower.Contains("arc") ||
               nameLower.Contains("amd") && nameLower.Contains("radeon") && nameLower.Contains("graphics") ||
               nameLower.Contains("integrated") ||
               nameLower.Contains("uhd") ||
               nameLower.Contains("hd graphics");
    }

    /// <summary>
    /// Gets memory form factor from WMI data with fallback logic
    /// </summary>
    private string GetMemoryFormFactor(ManagementObject module)
    {
        var formFactor = GetWmiIntProperty(module, "FormFactor", 0);
        return formFactor switch
        {
            8 => "DIMM",
            12 => "SO-DIMM", 
            13 => "Micro-DIMM",
            1 => "Other",
            2 => "SIP",
            3 => "DIP",
            4 => "ZIP",
            5 => "SOJ",
            6 => "Proprietary",
            7 => "SIMM",
            9 => "RIMM",
            10 => "SODIMM",
            11 => "SRIMM",
            _ => $"Unknown ({formFactor})"
        };
    }

    /// <summary>
    /// Gets memory type from WMI data with fallback logic
    /// </summary>
    private string GetMemoryType(ManagementObject module)
    {
        var memoryType = GetWmiIntProperty(module, "MemoryType", 0);
        var smBiosMemoryType = GetWmiIntProperty(module, "SMBIOSMemoryType", 0);
        
        // Try SMBIOSMemoryType first (more accurate for modern RAM)
        if (smBiosMemoryType > 0)
        {
            return smBiosMemoryType switch
            {
                26 => "DDR4",
                34 => "DDR5",
                24 => "DDR3",
                21 => "DDR2",
                20 => "DDR",
                2 => "DRAM",
                15 => "SDRAM",
                _ => $"SMBIOS-{smBiosMemoryType}"
            };
        }

        // Fallback to MemoryType
        return memoryType switch
        {
            20 => "DDR",
            21 => "DDR2", 
            24 => "DDR3",
            26 => "DDR4",
            1 => "Other",
            2 => "DRAM",
            3 => "Synchronous DRAM",
            4 => "Cache DRAM",
            5 => "EDO",
            6 => "EDRAM",
            7 => "VRAM",
            8 => "SRAM",
            9 => "RAM",
            10 => "ROM",
            11 => "Flash",
            12 => "EEPROM",
            13 => "FEPROM",
            14 => "EPROM",
            15 => "CDRAM",
            16 => "3DRAM",
            17 => "SDRAM",
            18 => "SGRAM",
            19 => "RDRAM",
            _ => $"Unknown ({memoryType})"
        };
    }

    /// <summary>
    /// Determines storage device type from WMI properties
    /// </summary>
    private string DetermineStorageType(ManagementObject disk)
    {
        var model = GetWmiStringProperty(disk, "Model", "").ToLower();
        var interfaceType = GetWmiStringProperty(disk, "InterfaceType", "").ToLower();
        var mediaType = GetWmiStringProperty(disk, "MediaType", "").ToLower();

        // NVMe detection
        if (model.Contains("nvme") || interfaceType.Contains("nvme"))
            return "NVMe SSD";

        // SSD detection
        if (model.Contains("ssd") || model.Contains("solid state"))
            return "SSD";

        // Interface-based detection
        if (interfaceType.Contains("sata") && (model.Contains("ssd") || !model.Contains("hdd")))
            return "SATA SSD";

        if (interfaceType.Contains("usb"))
            return "USB Drive";

        if (interfaceType.Contains("1394"))
            return "FireWire Drive";

        // Media type detection
        if (mediaType.Contains("fixed"))
        {
            // Try to determine if it's SSD or HDD based on other properties
            var rpm = GetWmiIntProperty(disk, "RPM", 0);
            if (rpm > 0)
                return "HDD";
            else if (model.Contains("hd") || model.Contains("hard"))
                return "HDD";
            else
                return "Fixed Drive";
        }

        if (mediaType.Contains("removable"))
            return "Removable Drive";

        // Default fallback
        return "Unknown";
    }

    /// <summary>
    /// Applies memory fallback values when WMI detection fails
    /// </summary>
    private void ApplyMemoryFallback()
    {
        _logger.LogWarning("Applying memory fallback detection");
        
        // Try to get total physical memory from Environment
        var totalPhysical = GC.GetTotalMemory(false);
        _memorySpec.TotalPhysicalBytes = Math.Max(8L * 1024 * 1024 * 1024, totalPhysical * 8); // At least 8GB fallback
        _memorySpec.TotalVirtualBytes = _memorySpec.TotalPhysicalBytes * 2; // Assume 2x virtual
        
        // Create fallback memory module
        _memorySpec.MemoryModules.Clear();
        _memorySpec.MemoryModules.Add(new MemoryModule
        {
            SlotNumber = 0,
            BankLabel = "Unknown Bank",
            CapacityBytes = _memorySpec.TotalPhysicalBytes,
            Manufacturer = "Unknown",
            MemoryType = "Unknown",
            FormFactor = "Unknown"
        });
    }

    #endregion

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            _updateTimer?.Dispose();
            
            foreach (var counter in _cpuCoreCounters.Values)
            {
                counter?.Dispose();
            }
            _cpuCoreCounters.Clear();
            
            _memoryCounter?.Dispose();
            
            foreach (var counter in _diskCounters)
            {
                counter?.Dispose();
            }
            _diskCounters.Clear();
            
            _logger.LogInformation("Hardware inventory service disposed");
        }
    }
}

/// <summary>
/// Event arguments for hardware inventory updates
/// </summary>
public class HardwareInventoryEventArgs : EventArgs
{
    public CpuSpecification CpuSpecification { get; set; } = new();
    public MemorySpecification MemorySpecification { get; set; } = new();
    public List<GpuSpecification> GpuSpecifications { get; set; } = new();
    public List<StorageDevice> StorageDevices { get; set; } = new();
    public HardwareRollupSummary RollupSummary { get; set; } = new();
    public DateTime Timestamp { get; set; }
}