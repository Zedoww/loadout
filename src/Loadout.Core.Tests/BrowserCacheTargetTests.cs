using Loadout.Core.Optimization.Cleanup;

namespace Loadout.Core.Tests;

public class BrowserCacheTargetTests : IDisposable
{
    private readonly string _root;

    public BrowserCacheTargetTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "loadout-cache-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Clean_deletes_cache_but_never_touches_credentials()
    {
        // A realistic Chromium profile: a Cache folder plus credential files that
        // live OUTSIDE the cache directory and must survive.
        string cache = Path.Combine(_root, "Cache");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "data_0"), new string('x', 4096));

        string cookies = Path.Combine(_root, "Cookies");
        string history = Path.Combine(_root, "History");
        File.WriteAllText(cookies, "secret-session");
        File.WriteAllText(history, "browsing-history");

        // The target is pointed only at the cache directory (as the resolver would).
        var target = new BrowserCacheTarget(new[] { cache });

        long scanned = target.Scan();
        var result = target.Clean();

        Assert.Equal(4096, scanned);
        Assert.True(result.Success);
        Assert.Equal(4096, result.BytesFreed);

        // The crucial safety guarantee: credentials untouched.
        Assert.True(File.Exists(cookies));
        Assert.True(File.Exists(history));
        Assert.Equal("secret-session", File.ReadAllText(cookies));
        Assert.Empty(Directory.GetFiles(cache));
    }

    [Fact]
    public void Category_is_caution_and_not_selected_by_default()
    {
        var target = new BrowserCacheTarget();
        Assert.Equal("browser-cache", target.Category.Id);
        Assert.Equal(CleanupRisk.Caution, target.Category.Risk);
        Assert.False(target.Category.SelectedByDefault);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
}
