using Microsoft.Extensions.Logging;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// VRAM budget manager enforcing memory allocation discipline
/// </summary>
public class VRAMBudgetManager : IDisposable
{
    private readonly ILogger<VRAMBudgetManager> _logger;
    private readonly List<VRAMAllocation> _allocations = new();
    private readonly long _totalVRAM;
    private readonly object _allocationLock = new();
    private bool _disposed = false;

    public VRAMBudgetManager(ILogger<VRAMBudgetManager> logger, long totalVRAM = 8L * 1024 * 1024 * 1024)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _totalVRAM = totalVRAM;
        _logger.LogInformation("VRAM Budget Manager initialized with {TotalVRAM}MB total VRAM", 
            _totalVRAM / (1024 * 1024));
    }

    /// <summary>
    /// Request VRAM allocation with budget enforcement
    /// </summary>
    public bool RequestVRAMAllocation(string component, long requiredBytes, VRAMPriority priority = VRAMPriority.Normal)
    {
        if (_disposed) return false;

        lock (_allocationLock)
        {
            var currentUsage = _allocations.Sum(a => a.AllocatedBytes);
            var availableVRAM = _totalVRAM - currentUsage;

            // Apply priority-based allocation limits
            var effectiveLimit = CalculateEffectiveLimit(priority, availableVRAM);

            if (requiredBytes > effectiveLimit)
            {
                _logger.LogWarning("VRAM allocation denied: {Component} requested {Required}MB, only {Available}MB available (priority: {Priority})",
                    component, requiredBytes / (1024 * 1024), effectiveLimit / (1024 * 1024), priority);
                
                // Attempt garbage collection for critical allocations
                if (priority == VRAMPriority.Critical)
                {
                    CleanupStaleAllocations();
                    return RequestVRAMAllocation(component, requiredBytes, priority); // Retry once
                }

                return false;
            }

            var allocation = new VRAMAllocation
            {
                Component = component,
                AllocatedBytes = requiredBytes,
                Priority = priority,
                AllocationTime = DateTime.UtcNow,
                AllocationId = Guid.NewGuid()
            };

            _allocations.Add(allocation);

            _logger.LogInformation("VRAM allocated: {Component} = {Allocated}MB, Total Usage: {TotalUsage}MB/{TotalVRAM}MB ({UsagePercent:F1}%)",
                component, requiredBytes / (1024 * 1024), 
                (_allocations.Sum(a => a.AllocatedBytes)) / (1024 * 1024),
                _totalVRAM / (1024 * 1024),
                (double)(_allocations.Sum(a => a.AllocatedBytes)) / _totalVRAM * 100);

            return true;
        }
    }

    /// <summary>
    /// Release VRAM allocation
    /// </summary>
    public bool ReleaseVRAMAllocation(string component, Guid? allocationId = null)
    {
        if (_disposed) return false;

        lock (_allocationLock)
        {
            var allocationsToRemove = allocationId.HasValue
                ? _allocations.Where(a => a.AllocationId == allocationId.Value).ToList()
                : _allocations.Where(a => a.Component == component).ToList();

            if (!allocationsToRemove.Any())
            {
                _logger.LogWarning("No VRAM allocation found for release: {Component} (ID: {AllocationId})", 
                    component, allocationId);
                return false;
            }

            var releasedBytes = allocationsToRemove.Sum(a => a.AllocatedBytes);
            foreach (var allocation in allocationsToRemove)
            {
                _allocations.Remove(allocation);
            }

            _logger.LogInformation("VRAM released: {Component} = {Released}MB, Remaining Usage: {RemainingUsage}MB/{TotalVRAM}MB",
                component, releasedBytes / (1024 * 1024),
                (_allocations.Sum(a => a.AllocatedBytes)) / (1024 * 1024),
                _totalVRAM / (1024 * 1024));

            return true;
        }
    }

    /// <summary>
    /// Get current VRAM allocation statistics
    /// </summary>
    public VRAMAllocationStats GetAllocationStats()
    {
        lock (_allocationLock)
        {
            var totalAllocated = _allocations.Sum(a => a.AllocatedBytes);
            var availableVRAM = _totalVRAM - totalAllocated;

            return new VRAMAllocationStats
            {
                TotalVRAM = _totalVRAM,
                AllocatedVRAM = totalAllocated,
                AvailableVRAM = availableVRAM,
                UsagePercent = (double)totalAllocated / _totalVRAM * 100,
                AllocationCount = _allocations.Count,
                AllocationsByComponent = _allocations
                    .GroupBy(a => a.Component)
                    .ToDictionary(g => g.Key, g => g.Sum(a => a.AllocatedBytes)),
                LargestAllocation = _allocations.Any() ? _allocations.Max(a => a.AllocatedBytes) : 0,
                OldestAllocation = _allocations.Any() ? _allocations.Min(a => a.AllocationTime) : DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Check if VRAM allocation would succeed without actually allocating
    /// </summary>
    public VRAMAllocationFeasibility CheckAllocationFeasibility(long requiredBytes, VRAMPriority priority = VRAMPriority.Normal)
    {
        lock (_allocationLock)
        {
            var currentUsage = _allocations.Sum(a => a.AllocatedBytes);
            var availableVRAM = _totalVRAM - currentUsage;
            var effectiveLimit = CalculateEffectiveLimit(priority, availableVRAM);

            return new VRAMAllocationFeasibility
            {
                CanAllocate = requiredBytes <= effectiveLimit,
                AvailableBytes = effectiveLimit,
                RequiredBytes = requiredBytes,
                CurrentUsagePercent = (double)currentUsage / _totalVRAM * 100,
                PostAllocationUsagePercent = (double)(currentUsage + requiredBytes) / _totalVRAM * 100,
                RecommendedAction = GetRecommendedAction(requiredBytes, effectiveLimit, priority)
            };
        }
    }

    private long CalculateEffectiveLimit(VRAMPriority priority, long availableVRAM)
    {
        return priority switch
        {
            VRAMPriority.Critical => availableVRAM, // Can use all available VRAM
            VRAMPriority.High => (long)(availableVRAM * 0.9), // Can use 90% of available
            VRAMPriority.Normal => (long)(availableVRAM * 0.7), // Can use 70% of available
            VRAMPriority.Low => (long)(availableVRAM * 0.5), // Can use 50% of available
            _ => (long)(availableVRAM * 0.7)
        };
    }

    private void CleanupStaleAllocations()
    {
        var staleThreshold = DateTime.UtcNow.AddMinutes(-30); // 30 minutes old
        var staleAllocations = _allocations.Where(a => a.AllocationTime < staleThreshold).ToList();

        foreach (var allocation in staleAllocations)
        {
            _allocations.Remove(allocation);
            _logger.LogInformation("Cleaned up stale VRAM allocation: {Component} = {Size}MB (age: {Age})",
                allocation.Component, allocation.AllocatedBytes / (1024 * 1024),
                DateTime.UtcNow - allocation.AllocationTime);
        }
    }

    private string GetRecommendedAction(long requiredBytes, long availableBytes, VRAMPriority priority)
    {
        if (requiredBytes <= availableBytes) return "Allocation should succeed";
        
        var deficit = requiredBytes - availableBytes;
        
        if (priority == VRAMPriority.Critical)
        {
            return $"Cleanup stale allocations or upgrade VRAM. Deficit: {deficit / (1024 * 1024)}MB";
        }
        
        if (deficit < availableBytes * 0.2)
        {
            return "Consider reducing allocation size or cleaning up unused allocations";
        }
        
        return "Significant VRAM shortage. Consider model optimization or VRAM upgrade";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            lock (_allocationLock)
            {
                var totalAllocated = _allocations.Sum(a => a.AllocatedBytes);
                _logger.LogInformation("VRAM Budget Manager disposing with {Allocations} active allocations ({TotalMB}MB)",
                    _allocations.Count, totalAllocated / (1024 * 1024));
                _allocations.Clear();
            }
        }
    }
}

/// <summary>
/// VRAM allocation record
/// </summary>
public record VRAMAllocation
{
    public Guid AllocationId { get; init; }
    public string Component { get; init; } = string.Empty;
    public long AllocatedBytes { get; init; }
    public VRAMPriority Priority { get; init; }
    public DateTime AllocationTime { get; init; }
}

/// <summary>
/// VRAM allocation statistics
/// </summary>
public record VRAMAllocationStats
{
    public long TotalVRAM { get; init; }
    public long AllocatedVRAM { get; init; }
    public long AvailableVRAM { get; init; }
    public double UsagePercent { get; init; }
    public int AllocationCount { get; init; }
    public Dictionary<string, long> AllocationsByComponent { get; init; } = new();
    public long LargestAllocation { get; init; }
    public DateTime OldestAllocation { get; init; }
}

/// <summary>
/// VRAM allocation feasibility assessment
/// </summary>
public record VRAMAllocationFeasibility
{
    public bool CanAllocate { get; init; }
    public long AvailableBytes { get; init; }
    public long RequiredBytes { get; init; }
    public double CurrentUsagePercent { get; init; }
    public double PostAllocationUsagePercent { get; init; }
    public string RecommendedAction { get; init; } = string.Empty;
}

/// <summary>
/// VRAM allocation priority levels
/// </summary>
public enum VRAMPriority
{
    Low,
    Normal,
    High,
    Critical
}