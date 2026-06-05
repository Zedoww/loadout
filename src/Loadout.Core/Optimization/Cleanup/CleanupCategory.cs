namespace Loadout.Core.Optimization.Cleanup;

/// <summary>
/// Describes one cleanable category shown in the Cleanup page. The category is
/// metadata only — the actual scan/clean work lives on its <see cref="ICleanupTarget"/>.
/// </summary>
public sealed record CleanupCategory(
    string Id,
    string Name,
    string Description,
    CleanupRisk Risk,
    bool SelectedByDefault);
