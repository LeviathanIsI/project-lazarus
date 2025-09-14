using System;
using System.Collections.Generic;

namespace Lazarus.Shared.Contracts
{
    public enum PrecisionType
    {
        FP32,
        FP16,
        BF16,
        INT8,
        INT4
    }

    public sealed class TrainingResources
    {
        public required string Id { get; init; }
        public List<string> GpuIds { get; set; } = new();
        public int BatchSize { get; set; } = 1;
        public int GradientAccumulationSteps { get; set; } = 8;
        public PrecisionType Precision { get; set; } = PrecisionType.FP16;
        public bool UseGradientCheckpointing { get; set; } = true;
        public int? MaxMemoryMB { get; set; }

        // Estimates (computed)
        public long EstimatedVRAMBytes { get; set; }
        public TimeSpan? EstimatedTimePerEpoch { get; set; }

        // Runtime monitoring
        public double CurrentVRAMUsagePercent { get; set; }
        public double CurrentCPUUsagePercent { get; set; }
    }

    public sealed class GpuInfo
    {
        public required string Id { get; init; }
        public required string Name { get; set; }
        public long TotalMemoryBytes { get; set; }
        public long FreeMemoryBytes { get; set; }
        public double UtilizationPercent { get; set; }
        public double TemperatureCelsius { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
