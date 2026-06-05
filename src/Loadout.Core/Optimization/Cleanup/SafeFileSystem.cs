namespace Loadout.Core.Optimization.Cleanup;

/// <summary>
/// File-system helpers that never throw on inaccessible paths. Shared by the
/// file-based cleanup targets so locked or protected entries are skipped, not
/// fatal. Extracted from the original TempCleaner.
/// </summary>
internal static class SafeFileSystem
{
    /// <summary>Recursively yields every file under <paramref name="root"/>,
    /// silently skipping directories it cannot read.</summary>
    public static IEnumerable<string> EnumerateFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string current = pending.Pop();
            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(current); } catch { }
            foreach (var f in files) yield return f;

            foreach (var sub in GetDirectories(current)) pending.Push(sub);
        }
    }

    public static string[] GetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch { return Array.Empty<string>(); }
    }

    /// <summary>Sum of file sizes under <paramref name="dir"/>; 0 if it is missing.</summary>
    public static long DirectorySize(string dir)
    {
        if (!Directory.Exists(dir)) return 0;
        long total = 0;
        foreach (var file in EnumerateFiles(dir))
        {
            try { total += new FileInfo(file).Length; }
            catch { /* inaccessible */ }
        }
        return total;
    }

    /// <summary>Deletes every accessible file under <paramref name="dir"/>, then
    /// any now-empty sub-directories. Locked files are counted as skipped.</summary>
    public static void DeleteContents(string dir, ref long freed, ref int deleted, ref int skipped)
    {
        if (!Directory.Exists(dir)) return;

        foreach (var file in EnumerateFiles(dir))
        {
            try
            {
                long size = new FileInfo(file).Length;
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                freed += size;
                deleted++;
            }
            catch
            {
                skipped++; // in use or protected: leave it alone.
            }
        }

        foreach (var sub in GetDirectories(dir))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(sub).Any())
                    Directory.Delete(sub);
            }
            catch { /* ignore */ }
        }
    }
}
