namespace Loadout.Core.Optimization.Cleanup;

/// <summary>
/// How careful the user should be about a cleanup category. Drives the UI badge
/// and the safe-by-default selection.
/// </summary>
public enum CleanupRisk
{
    /// <summary>Disposable data, no user-visible consequence. Pre-selected.</summary>
    Safe,

    /// <summary>Harmless but noticeable (e.g. browsers re-fill their cache, the
    /// Recycle Bin is emptied for good). Not pre-selected; badged for the user.</summary>
    Caution
}
