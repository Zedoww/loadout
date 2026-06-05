using Loadout.Core.Optimization;
using Loadout.Core.Optimization.Cleanup;

namespace Loadout.Core.Tests;

public class CleanupServiceTests
{
    /// <summary>A test double recording whether it was cleaned.</summary>
    private sealed class FakeTarget : ICleanupTarget
    {
        public FakeTarget(string id, long size) =>
            Category = new CleanupCategory(id, id, "", CleanupRisk.Safe, false);

        public CleanupCategory Category { get; }
        public bool Cleaned { get; private set; }
        public long Scan() => 100;
        public OptimizationResult Clean()
        {
            Cleaned = true;
            return OptimizationResult.Ok("done", 100);
        }
    }

    [Fact]
    public void ScanAll_reports_every_category()
    {
        var service = new CleanupService(new[]
        {
            new FakeTarget("a", 100), new FakeTarget("b", 100)
        });

        var items = service.ScanAll();

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(100, i.Bytes));
    }

    [Fact]
    public void Clean_only_runs_selected_categories()
    {
        var a = new FakeTarget("a", 100);
        var b = new FakeTarget("b", 100);
        var service = new CleanupService(new[] { a, b });

        var results = service.Clean(new[] { "a" });

        Assert.Single(results);
        Assert.Equal("a", results[0].Category.Id);
        Assert.True(a.Cleaned);
        Assert.False(b.Cleaned);
    }

    [Fact]
    public void Clean_with_no_selection_does_nothing()
    {
        var a = new FakeTarget("a", 100);
        var service = new CleanupService(new[] { a });

        var results = service.Clean(Array.Empty<string>());

        Assert.Empty(results);
        Assert.False(a.Cleaned);
    }
}
