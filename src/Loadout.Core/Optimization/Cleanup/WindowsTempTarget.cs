namespace Loadout.Core.Optimization.Cleanup;

/// <summary>
/// Cleans Windows temporary folders. Touches only known temp locations and skips
/// any locked file (in use by a running program), which keeps the operation safe.
/// </summary>
public sealed class WindowsTempTarget : ICleanupTarget
{
    private readonly IReadOnlyList<string>? _overrideDirectories;

    public WindowsTempTarget() { }

    /// <summary>Test constructor: targets specific directories.</summary>
    public WindowsTempTarget(IReadOnlyList<string> directories) => _overrideDirectories = directories;

    public CleanupCategory Category { get; } = new(
        Id: "windows-temp",
        Name: "Windows temporary files",
        Description: "Leftover files in %TEMP% and the Windows temp folders. Safe to delete.",
        Risk: CleanupRisk.Safe,
        SelectedByDefault: true);

    private IEnumerable<string> Directories()
    {
        if (_overrideDirectories is not null)
        {
            foreach (var d in _overrideDirectories) yield return d;
            yield break;
        }

        yield return Path.GetTempPath();                                    // user %TEMP%
        yield return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Temp");
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(local))
            yield return Path.Combine(local, "Temp");
    }

    public long Scan()
    {
        long total = 0;
        foreach (var dir in Directories().Distinct(StringComparer.OrdinalIgnoreCase))
            total += SafeFileSystem.DirectorySize(dir);
        return total;
    }

    public OptimizationResult Clean()
    {
        long freed = 0;
        int deleted = 0, skipped = 0;

        foreach (var dir in Directories().Distinct(StringComparer.OrdinalIgnoreCase))
            SafeFileSystem.DeleteContents(dir, ref freed, ref deleted, ref skipped);

        return OptimizationResult.Ok(
            $"{deleted} files deleted ({skipped} skipped, in use).", freed);
    }
}
