using System.ComponentModel;

namespace Lazarus.App.Desktop.Services.Models;

#region CPU Models

/// <summary>
/// Detailed CPU specifications and real-time metrics
/// </summary>
public class CpuSpecification : INotifyPropertyChanged
{
    private double _currentUsage;
    private double _currentTemperature;
    private double _currentFrequency;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Static specifications
    public string ProcessorName { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Stepping { get; set; } = string.Empty;
    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }
    public double BaseFrequencyMhz { get; set; }
    public double MaxFrequencyMhz { get; set; }
    public int CacheL1KB { get; set; }
    public int CacheL2KB { get; set; }
    public int CacheL3KB { get; set; }
    public int TdpWatts { get; set; }
    public string SocketType { get; set; } = string.Empty;

    // Real-time metrics
    public double CurrentUsage
    {
        get => _currentUsage;
        set
        {
            if (Math.Abs(_currentUsage - value) > 0.1)
            {
                _currentUsage = value;
                OnPropertyChanged(nameof(CurrentUsage));
            }
        }
    }

    public double CurrentTemperature
    {
        get => _currentTemperature;
        set
        {
            if (Math.Abs(_currentTemperature - value) > 0.5)
            {
                _currentTemperature = value;
                OnPropertyChanged(nameof(CurrentTemperature));
            }
        }
    }

    public double CurrentFrequencyMhz
    {
        get => _currentFrequency;
        set
        {
            if (Math.Abs(_currentFrequency - value) > 10)
            {
                _currentFrequency = value;
                OnPropertyChanged(nameof(CurrentFrequencyMhz));
            }
        }
    }

    public List<CpuCoreMetrics> CoreMetrics { get; set; } = new();

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Individual CPU core real-time metrics
/// </summary>
public class CpuCoreMetrics : INotifyPropertyChanged
{
    private double _usage;
    private double _temperature;
    private double _frequency;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int CoreId { get; set; }
    public string CoreName => $"Core {CoreId}";

    public double Usage
    {
        get => _usage;
        set
        {
            if (Math.Abs(_usage - value) > 0.1)
            {
                _usage = value;
                OnPropertyChanged(nameof(Usage));
            }
        }
    }

    public double Temperature
    {
        get => _temperature;
        set
        {
            if (Math.Abs(_temperature - value) > 0.5)
            {
                _temperature = value;
                OnPropertyChanged(nameof(Temperature));
            }
        }
    }

    public double FrequencyMhz
    {
        get => _frequency;
        set
        {
            if (Math.Abs(_frequency - value) > 10)
            {
                _frequency = value;
                OnPropertyChanged(nameof(FrequencyMhz));
            }
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#endregion

#region Memory Models

/// <summary>
/// Comprehensive memory analysis and specifications
/// </summary>
public class MemorySpecification : INotifyPropertyChanged
{
    private long _availableBytes;
    private long _usedBytes;
    private double _usagePercentage;

    public event PropertyChangedEventHandler? PropertyChanged;

    // System memory overview
    public long TotalPhysicalBytes { get; set; }
    public long TotalVirtualBytes { get; set; }
    public long PageFileBytes { get; set; }

    public long AvailableBytes
    {
        get => _availableBytes;
        set
        {
            if (Math.Abs(_availableBytes - value) > 1024 * 1024) // 1MB threshold
            {
                _availableBytes = value;
                OnPropertyChanged(nameof(AvailableBytes));
                OnPropertyChanged(nameof(AvailableGB));
            }
        }
    }

    public long UsedBytes
    {
        get => _usedBytes;
        set
        {
            if (Math.Abs(_usedBytes - value) > 1024 * 1024) // 1MB threshold
            {
                _usedBytes = value;
                OnPropertyChanged(nameof(UsedBytes));
                OnPropertyChanged(nameof(UsedGB));
            }
        }
    }

    public double UsagePercentage
    {
        get => _usagePercentage;
        set
        {
            if (Math.Abs(_usagePercentage - value) > 0.1)
            {
                _usagePercentage = value;
                OnPropertyChanged(nameof(UsagePercentage));
            }
        }
    }

    // Convenience properties
    public double TotalPhysicalGB => TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0);
    public double AvailableGB => AvailableBytes / (1024.0 * 1024.0 * 1024.0);
    public double UsedGB => UsedBytes / (1024.0 * 1024.0 * 1024.0);
    public double PageFileGB => PageFileBytes / (1024.0 * 1024.0 * 1024.0);
    
    // Formatted display strings for Dashboard
    public string UsageDisplayText => $"{UsedGB:F1} GB Used / {TotalPhysicalGB:F1} GB Total";
    public string UsageShortDisplayText => $"{UsedGB:F1}GB / {TotalPhysicalGB:F1}GB";

    // Individual RAM modules
    public List<MemoryModule> MemoryModules { get; set; } = new();

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Individual RAM module specifications
/// </summary>
public class MemoryModule
{
    public int SlotNumber { get; set; }
    public string BankLabel { get; set; } = string.Empty;
    public long CapacityBytes { get; set; }
    public double CapacityGB => CapacityBytes / (1024.0 * 1024.0 * 1024.0);
    public int SpeedMhz { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FormFactor { get; set; } = string.Empty; // DDR4, DDR5, etc.
    public string MemoryType { get; set; } = string.Empty;
    public int DataWidth { get; set; }
    public int TotalWidth { get; set; }
    public string Timings { get; set; } = string.Empty;
}

#endregion

#region GPU Models

/// <summary>
/// Complete graphics adapter inventory and real-time metrics
/// </summary>
public class GpuSpecification : INotifyPropertyChanged
{
    private double _currentUsage;
    private long _currentMemoryUsed;
    private double _currentTemperature;
    private double _currentFanSpeed;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Static specifications
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string DriverVersion { get; set; } = string.Empty;
    public string DriverDate { get; set; } = string.Empty;
    public long AdapterRamBytes { get; set; }
    public double AdapterRamGB => AdapterRamBytes / (1024.0 * 1024.0 * 1024.0);
    public string VideoProcessor { get; set; } = string.Empty;
    public string VideoArchitecture { get; set; } = string.Empty;
    public int MaxRefreshRate { get; set; }
    public string VideoModeDescription { get; set; } = string.Empty;
    public bool IsIntegrated { get; set; }
    public bool IsPrimary { get; set; }

    // Real-time metrics
    public double CurrentUsage
    {
        get => _currentUsage;
        set
        {
            if (Math.Abs(_currentUsage - value) > 0.1)
            {
                _currentUsage = value;
                OnPropertyChanged(nameof(CurrentUsage));
            }
        }
    }

    public long CurrentMemoryUsed
    {
        get => _currentMemoryUsed;
        set
        {
            if (Math.Abs(_currentMemoryUsed - value) > 1024 * 1024) // 1MB threshold
            {
                _currentMemoryUsed = value;
                OnPropertyChanged(nameof(CurrentMemoryUsed));
                OnPropertyChanged(nameof(CurrentMemoryUsedGB));
                OnPropertyChanged(nameof(MemoryUsagePercentage));
            }
        }
    }

    public double CurrentTemperature
    {
        get => _currentTemperature;
        set
        {
            if (Math.Abs(_currentTemperature - value) > 0.5)
            {
                _currentTemperature = value;
                OnPropertyChanged(nameof(CurrentTemperature));
            }
        }
    }

    public double CurrentFanSpeed
    {
        get => _currentFanSpeed;
        set
        {
            if (Math.Abs(_currentFanSpeed - value) > 50)
            {
                _currentFanSpeed = value;
                OnPropertyChanged(nameof(CurrentFanSpeed));
            }
        }
    }

    // Convenience properties
    public double CurrentMemoryUsedGB => CurrentMemoryUsed / (1024.0 * 1024.0 * 1024.0);
    public double MemoryUsagePercentage => AdapterRamBytes > 0 ? (CurrentMemoryUsed * 100.0) / AdapterRamBytes : 0;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#endregion

#region Storage Models

/// <summary>
/// Complete storage device specifications and health monitoring
/// </summary>
public class StorageDevice : INotifyPropertyChanged
{
    private long _freeSpace;
    private double _temperature;
    private double _readSpeed;
    private double _writeSpeed;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Static specifications
    public string DeviceId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public string DriveType { get; set; } = string.Empty; // SSD, HDD, NVMe
    public string InterfaceType { get; set; } = string.Empty; // SATA, NVMe, USB, etc.
    public string FileSystem { get; set; } = string.Empty;
    public string DriveLetter { get; set; } = string.Empty;
    public int RpmSpeed { get; set; } // For HDDs
    public bool IsSystemDrive { get; set; }
    public bool IsRemovable { get; set; }

    // Real-time metrics
    public long FreeSpace
    {
        get => _freeSpace;
        set
        {
            if (Math.Abs(_freeSpace - value) > 1024 * 1024 * 100) // 100MB threshold
            {
                _freeSpace = value;
                OnPropertyChanged(nameof(FreeSpace));
                OnPropertyChanged(nameof(FreeSpaceGB));
                OnPropertyChanged(nameof(UsagePercentage));
            }
        }
    }

    public double Temperature
    {
        get => _temperature;
        set
        {
            if (Math.Abs(_temperature - value) > 0.5)
            {
                _temperature = value;
                OnPropertyChanged(nameof(Temperature));
            }
        }
    }

    public double ReadSpeedMBps
    {
        get => _readSpeed;
        set
        {
            if (Math.Abs(_readSpeed - value) > 1.0)
            {
                _readSpeed = value;
                OnPropertyChanged(nameof(ReadSpeedMBps));
            }
        }
    }

    public double WriteSpeedMBps
    {
        get => _writeSpeed;
        set
        {
            if (Math.Abs(_writeSpeed - value) > 1.0)
            {
                _writeSpeed = value;
                OnPropertyChanged(nameof(WriteSpeedMBps));
            }
        }
    }

    // Health and SMART data
    public DriveHealth HealthStatus { get; set; } = DriveHealth.Unknown;
    public int HealthPercentage { get; set; } = 100;
    public long PowerOnHours { get; set; }
    public int PowerCycleCount { get; set; }
    public Dictionary<string, object> SmartAttributes { get; set; } = new();

    // Convenience properties
    public double TotalSizeGB => TotalSize / (1024.0 * 1024.0 * 1024.0);
    public double FreeSpaceGB => FreeSpace / (1024.0 * 1024.0 * 1024.0);
    public double UsedSpaceGB => (TotalSize - FreeSpace) / (1024.0 * 1024.0 * 1024.0);
    public double UsagePercentage => TotalSize > 0 ? ((TotalSize - FreeSpace) * 100.0) / TotalSize : 0;
    
    // Formatted display strings for Dashboard
    public string UsageDisplayText => $"{UsedSpaceGB:F1} GB Used / {TotalSizeGB:F1} GB Total";
    public string UsageShortDisplayText => $"{UsedSpaceGB:F1}GB / {TotalSizeGB:F1}GB";
    public string CapacityDisplayText => $"{TotalSizeGB:F0} GB {DriveType}";

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum DriveHealth
{
    Unknown,
    Excellent,
    Good,
    Fair,
    Poor,
    Critical,
    Failing
}

#endregion

#region Rollup Summary Models

/// <summary>
/// System-wide hardware summary and aggregate metrics
/// </summary>
public class HardwareRollupSummary : INotifyPropertyChanged
{
    private double _totalCpuUsage;
    private double _totalMemoryUsage;
    private double _totalGpuUsage;
    private double _averageTemperature;

    public event PropertyChangedEventHandler? PropertyChanged;

    // CPU Summary
    public int TotalCpuCores { get; set; }
    public int TotalCpuThreads { get; set; }
    public double TotalCpuUsage
    {
        get => _totalCpuUsage;
        set
        {
            if (Math.Abs(_totalCpuUsage - value) > 0.1)
            {
                _totalCpuUsage = value;
                OnPropertyChanged(nameof(TotalCpuUsage));
            }
        }
    }

    // Memory Summary
    public long TotalMemoryBytes { get; set; }
    public double TotalMemoryGB => TotalMemoryBytes / (1024.0 * 1024.0 * 1024.0);
    public double TotalMemoryUsage
    {
        get => _totalMemoryUsage;
        set
        {
            if (Math.Abs(_totalMemoryUsage - value) > 0.1)
            {
                _totalMemoryUsage = value;
                OnPropertyChanged(nameof(TotalMemoryUsage));
            }
        }
    }

    // GPU Summary
    public int TotalGpuCount { get; set; }
    public long TotalGpuMemoryBytes { get; set; }
    public double TotalGpuMemoryGB => TotalGpuMemoryBytes / (1024.0 * 1024.0 * 1024.0);
    public double TotalGpuUsage
    {
        get => _totalGpuUsage;
        set
        {
            if (Math.Abs(_totalGpuUsage - value) > 0.1)
            {
                _totalGpuUsage = value;
                OnPropertyChanged(nameof(TotalGpuUsage));
            }
        }
    }

    // Storage Summary
    public int TotalDriveCount { get; set; }
    public long TotalStorageBytes { get; set; }
    public long TotalFreeSpaceBytes { get; set; }
    public double TotalStorageGB => TotalStorageBytes / (1024.0 * 1024.0 * 1024.0);
    public double TotalFreeSpaceGB => TotalFreeSpaceBytes / (1024.0 * 1024.0 * 1024.0);
    public double OverallStorageUsage => TotalStorageBytes > 0 ? ((TotalStorageBytes - TotalFreeSpaceBytes) * 100.0) / TotalStorageBytes : 0;

    // System Health
    public double AverageTemperature
    {
        get => _averageTemperature;
        set
        {
            if (Math.Abs(_averageTemperature - value) > 0.5)
            {
                _averageTemperature = value;
                OnPropertyChanged(nameof(AverageTemperature));
            }
        }
    }

    public SystemHealthStatus OverallHealth { get; set; } = SystemHealthStatus.Good;
    public DateTime LastUpdated { get; set; }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum SystemHealthStatus
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

#endregion