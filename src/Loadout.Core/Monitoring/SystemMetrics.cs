namespace Loadout.Core.Monitoring;

/// <summary>
/// Snapshot of hardware measurements at a given instant.
/// Unavailable values (missing sensor) are <c>null</c>.
/// </summary>
public sealed record SystemMetrics
{
    public float? CpuLoad { get; init; }
    public float? CpuTemperature { get; init; }
    public string? CpuName { get; init; }

    public float? GpuLoad { get; init; }
    public float? GpuTemperature { get; init; }
    public string? GpuName { get; init; }

    /// <summary>Memory used, in GB.</summary>
    public float? MemoryUsedGb { get; init; }
    /// <summary>Total memory, in GB.</summary>
    public float? MemoryTotalGb { get; init; }
    public float? MemoryLoad { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.Now;
}
