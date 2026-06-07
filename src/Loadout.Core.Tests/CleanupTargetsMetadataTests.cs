using Loadout.Core.Optimization.Cleanup;

namespace Loadout.Core.Tests;

public class CleanupTargetsMetadataTests
{
    private static CleanupService NewService() => new(new ICleanupTarget[]
    {
        new WindowsTempTarget(new[] { Path.GetTempPath() }),
        new BrowserCacheTarget(Array.Empty<string>()),
        new RecycleBinTarget(),
    });

    [Fact]
    public void Categories_preserve_target_order()
    {
        var ids = NewService().Categories.Select(c => c.Id).ToArray();
        Assert.Equal(new[] { "windows-temp", "browser-cache", "recycle-bin" }, ids);
    }

    [Fact]
    public void Only_windows_temp_is_safe_and_selected_by_default()
    {
        var cats = NewService().Categories;

        Assert.Equal(CleanupRisk.Safe, cats[0].Risk);
        Assert.True(cats[0].SelectedByDefault);

        Assert.All(cats.Skip(1), c =>
        {
            Assert.Equal(CleanupRisk.Caution, c.Risk);
            Assert.False(c.SelectedByDefault);
        });
    }

    [Fact]
    public void RecycleBin_scan_never_throws_and_is_non_negative()
    {
        long size = new RecycleBinTarget().Scan();
        Assert.True(size >= 0);
    }
}
