namespace Loadout.Core.Optimization.Cleanup;

/// <summary>One category's measured reclaimable size (result of a scan).</summary>
public sealed record CleanupScanItem(CleanupCategory Category, long Bytes);

/// <summary>One category's cleanup outcome.</summary>
public sealed record CleanupCleanItem(CleanupCategory Category, OptimizationResult Result);

/// <summary>
/// Orchestrates the cleanup categories: previews their sizes and cleans only the
/// ones the user selected. Holds every <see cref="ICleanupTarget"/>.
/// </summary>
public sealed class CleanupService
{
    private readonly IReadOnlyList<ICleanupTarget> _targets;

    public CleanupService(IEnumerable<ICleanupTarget> targets) => _targets = targets.ToList();

    /// <summary>The categories, in display order.</summary>
    public IReadOnlyList<CleanupCategory> Categories => _targets.Select(t => t.Category).ToList();

    /// <summary>Scans every category. Read-only — deletes nothing.</summary>
    public IReadOnlyList<CleanupScanItem> ScanAll() =>
        _targets.Select(t => new CleanupScanItem(t.Category, t.Scan())).ToList();

    /// <summary>Cleans only the categories whose id is in <paramref name="selectedIds"/>.</summary>
    public IReadOnlyList<CleanupCleanItem> Clean(IEnumerable<string> selectedIds)
    {
        var wanted = new HashSet<string>(selectedIds, StringComparer.OrdinalIgnoreCase);
        return _targets
            .Where(t => wanted.Contains(t.Category.Id))
            .Select(t => new CleanupCleanItem(t.Category, t.Clean()))
            .ToList();
    }
}
