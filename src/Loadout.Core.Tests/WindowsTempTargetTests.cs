using Loadout.Core.Optimization.Cleanup;

namespace Loadout.Core.Tests;

public class WindowsTempTargetTests : IDisposable
{
    private readonly string _root;

    public WindowsTempTargetTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "loadout-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    private long SeedFiles()
    {
        File.WriteAllText(Path.Combine(_root, "a.tmp"), new string('x', 1000));
        File.WriteAllText(Path.Combine(_root, "b.log"), new string('y', 500));
        string sub = Path.Combine(_root, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "c.dat"), new string('z', 250));
        return 1750;
    }

    [Fact]
    public void Scan_computes_the_total_size()
    {
        long expected = SeedFiles();
        var target = new WindowsTempTarget(new[] { _root });

        Assert.Equal(expected, target.Scan());
    }

    [Fact]
    public void Scan_zero_when_empty()
    {
        var target = new WindowsTempTarget(new[] { _root });
        Assert.Equal(0, target.Scan());
    }

    [Fact]
    public void Clean_deletes_files_and_returns_freed_space()
    {
        long expected = SeedFiles();
        var target = new WindowsTempTarget(new[] { _root });

        var result = target.Clean();

        Assert.True(result.Success);
        Assert.Equal(expected, result.BytesFreed);
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Clean_ignores_a_missing_directory()
    {
        var target = new WindowsTempTarget(new[] { Path.Combine(_root, "nope") });
        var result = target.Clean();

        Assert.True(result.Success);
        Assert.Equal(0, result.BytesFreed);
    }

    [Fact]
    public void Category_is_safe_and_selected_by_default()
    {
        var target = new WindowsTempTarget();
        Assert.Equal("windows-temp", target.Category.Id);
        Assert.Equal(CleanupRisk.Safe, target.Category.Risk);
        Assert.True(target.Category.SelectedByDefault);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
}
