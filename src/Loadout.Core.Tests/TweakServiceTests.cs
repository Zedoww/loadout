using Loadout.Core.Optimization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Loadout.Core.Tests;

public class TweakServiceTests
{
    private static TweakService CreateSut() => new(NullLogger<TweakService>.Instance);

    [Fact]
    public void The_catalog_is_not_empty()
    {
        Assert.NotEmpty(CreateSut().Definitions);
    }

    [Fact]
    public void The_ids_are_unique()
    {
        var ids = CreateSut().Definitions.Select(d => d.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Every_tweak_has_a_real_effect()
    {
        // Enabled value differs from the default, otherwise the tweak is pointless.
        Assert.All(CreateSut().Definitions,
            d => Assert.NotEqual(d.EnabledValue, d.DefaultValue));
    }

    [Fact]
    public void Every_tweak_is_documented()
    {
        Assert.All(CreateSut().Definitions, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Name));
            Assert.False(string.IsNullOrWhiteSpace(d.Description));
            Assert.False(string.IsNullOrWhiteSpace(d.SubKey));
            Assert.False(string.IsNullOrWhiteSpace(d.ValueName));
        });
    }
}
