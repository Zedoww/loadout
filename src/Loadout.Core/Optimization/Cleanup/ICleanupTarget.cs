namespace Loadout.Core.Optimization.Cleanup;

/// <summary>
/// A single cleanable category. Scanning is read-only (preview the size before
/// deleting); cleaning performs the deletion and reports how much was freed.
/// </summary>
public interface ICleanupTarget
{
    CleanupCategory Category { get; }

    /// <summary>Bytes reclaimable right now. Must not delete anything.</summary>
    long Scan();

    /// <summary>Performs the deletion. Returns freed bytes and a user-facing message.</summary>
    OptimizationResult Clean();
}
