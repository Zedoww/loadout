namespace Loadout.Core.Optimization;

/// <summary>
/// Cleans temporary folders. It only touches known temp locations and skips any
/// locked file (in use by a running program), which keeps the operation safe.
/// </summary>
public sealed class TempCleaner
{
    private readonly IReadOnlyList<string>? _overrideDirectories;

    public TempCleaner() { }

    /// <summary>Test constructor: targets specific directories.</summary>
    public TempCleaner(IReadOnlyList<string> directories) => _overrideDirectories = directories;

    private IEnumerable<string> TempDirectories()
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

    /// <summary>Computes the reclaimable space (bytes) without deleting anything.</summary>
    public long Scan()
    {
        long total = 0;
        foreach (var dir in TempDirectories().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in EnumerateFilesSafe(dir))
            {
                try { total += new FileInfo(file).Length; }
                catch { /* inaccessible */ }
            }
        }
        return total;
    }

    /// <summary>Deletes accessible temporary files. Returns the freed space.</summary>
    public OptimizationResult Clean()
    {
        long freed = 0;
        int files = 0, skipped = 0;

        foreach (var dir in TempDirectories().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var file in EnumerateFilesSafe(dir))
            {
                try
                {
                    long size = new FileInfo(file).Length;
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    freed += size;
                    files++;
                }
                catch
                {
                    skipped++; // locked or protected: leave it as is.
                }
            }

            // Remove now-empty sub-directories.
            foreach (var sub in SafeGetDirectories(dir))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(sub).Any())
                        Directory.Delete(sub);
                }
                catch { /* ignore */ }
            }
        }

        return OptimizationResult.Ok(
            $"{files} files deleted ({skipped} skipped, in use).", freed);
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string current = pending.Pop();
            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(current); } catch { }
            foreach (var f in files) yield return f;

            foreach (var sub in SafeGetDirectories(current)) pending.Push(sub);
        }
    }

    private static string[] SafeGetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch { return Array.Empty<string>(); }
    }
}
