using Loadout.Core.Optimization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Loadout.Core.Tests;

public class SurgeServiceTests : IDisposable
{
    private readonly string _dataDir;

    public SurgeServiceTests()
    {
        _dataDir = Path.Combine(Path.GetTempPath(), "loadout-surge-tests-" + Guid.NewGuid().ToString("N"));
    }

    private SurgeService NewService() => new(
        new PowerPlanService(),
        new MemoryCleaner(),
        new ProcessService(NullLogger<ProcessService>.Instance),
        NullLogger<SurgeService>.Instance,
        _dataDir);

    [Fact]
    public void IsActive_is_false_without_a_snapshot()
    {
        Assert.False(NewService().IsActive);
    }

    [Fact]
    public void Restore_without_an_active_surge_fails_gracefully()
    {
        var results = NewService().Restore();

        Assert.Single(results);
        Assert.False(results[0].Success);
        Assert.Contains("No active Surge", results[0].Message);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dataDir, recursive: true); } catch { }
    }
}
