namespace Loadout.Core.Monitoring;

/// <summary>
/// Instantané des mesures matérielles à un instant donné.
/// Les valeurs non disponibles (capteur absent) valent <c>null</c>.
/// </summary>
public sealed record SystemMetrics
{
    public float? CpuLoad { get; init; }
    public float? CpuTemperature { get; init; }
    public string? CpuName { get; init; }

    public float? GpuLoad { get; init; }
    public float? GpuTemperature { get; init; }
    public string? GpuName { get; init; }

    /// <summary>Mémoire utilisée en Go.</summary>
    public float? MemoryUsedGb { get; init; }
    /// <summary>Mémoire totale en Go.</summary>
    public float? MemoryTotalGb { get; init; }
    public float? MemoryLoad { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.Now;
}
