namespace Loadout.Core.Optimization.Cleanup;

/// <summary>
/// Clears the on-disk cache of Chromium browsers (Chrome, Edge) and Firefox.
/// <para>
/// Safety guarantee: this only ever touches dedicated <b>cache</b> sub-folders
/// (<c>Cache</c>, <c>Code Cache</c>, <c>GPUCache</c>, Firefox <c>cache2</c>). It
/// never deletes cookies, history, saved passwords or sessions, so the user stays
/// logged in everywhere. That is deliberately safer than CCleaner's defaults.
/// </para>
/// </summary>
public sealed class BrowserCacheTarget : ICleanupTarget
{
    // The only sub-folder names we are ever allowed to delete inside a profile.
    private static readonly string[] ChromiumCacheDirs = { "Cache", "Code Cache", "GPUCache" };

    private readonly IReadOnlyList<string>? _overrideCacheDirs;

    public BrowserCacheTarget() { }

    /// <summary>Test constructor: targets explicit cache directories directly.</summary>
    public BrowserCacheTarget(IReadOnlyList<string> cacheDirectories) => _overrideCacheDirs = cacheDirectories;

    public CleanupCategory Category { get; } = new(
        Id: "browser-cache",
        Name: "Browser caches",
        Description: "Cached images and files for Chrome, Edge and Firefox. Cookies, " +
                     "history and saved logins are never touched — you stay signed in.",
        Risk: CleanupRisk.Caution,
        SelectedByDefault: false);

    /// <summary>Resolves the concrete cache directories to act on.</summary>
    private IReadOnlyList<string> CacheDirectories()
    {
        if (_overrideCacheDirs is not null) return _overrideCacheDirs;

        var dirs = new List<string>();
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Chromium-based: one set of cache folders per user profile.
        AddChromium(dirs, Path.Combine(local, @"Google\Chrome\User Data"));
        AddChromium(dirs, Path.Combine(local, @"Microsoft\Edge\User Data"));

        // Firefox: <profile>\cache2 lives under LocalAppData; profiles are listed
        // under Roaming. cache2 may also sit under Local on modern installs.
        AddFirefox(dirs, Path.Combine(local, @"Mozilla\Firefox\Profiles"));
        AddFirefox(dirs, Path.Combine(roaming, @"Mozilla\Firefox\Profiles"));

        return dirs;
    }

    private static void AddChromium(List<string> dirs, string userDataRoot)
    {
        if (!Directory.Exists(userDataRoot)) return;

        foreach (var profile in ProfileDirs(userDataRoot))
            foreach (var cache in ChromiumCacheDirs)
            {
                string path = Path.Combine(profile, cache);
                if (Directory.Exists(path)) dirs.Add(path);
            }
    }

    private static void AddFirefox(List<string> dirs, string profilesRoot)
    {
        if (!Directory.Exists(profilesRoot)) return;

        foreach (var profile in SafeFileSystem.GetDirectories(profilesRoot))
        {
            string path = Path.Combine(profile, "cache2");
            if (Directory.Exists(path)) dirs.Add(path);
        }
    }

    // Chromium profile folders: "Default" plus "Profile 1", "Profile 2"…
    private static IEnumerable<string> ProfileDirs(string userDataRoot)
    {
        foreach (var dir in SafeFileSystem.GetDirectories(userDataRoot))
        {
            string name = Path.GetFileName(dir);
            if (name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                yield return dir;
        }
    }

    public long Scan()
    {
        long total = 0;
        foreach (var dir in CacheDirectories())
            total += SafeFileSystem.DirectorySize(dir);
        return total;
    }

    public OptimizationResult Clean()
    {
        long freed = 0;
        int deleted = 0, skipped = 0;

        foreach (var dir in CacheDirectories())
            SafeFileSystem.DeleteContents(dir, ref freed, ref deleted, ref skipped);

        string note = skipped > 0
            ? $"{deleted} cache files deleted ({skipped} skipped — browser open?)."
            : $"{deleted} cache files deleted.";
        return OptimizationResult.Ok(note, freed);
    }
}
