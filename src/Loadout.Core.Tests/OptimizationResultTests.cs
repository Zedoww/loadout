using Loadout.Core.Optimization;

namespace Loadout.Core.Tests;

public class OptimizationResultTests
{
    [Fact]
    public void Ok_cree_un_resultat_reussi()
    {
        var r = OptimizationResult.Ok("fait", 2048);

        Assert.True(r.Success);
        Assert.Equal("fait", r.Message);
        Assert.Equal(2048, r.BytesFreed);
    }

    [Fact]
    public void Fail_cree_un_resultat_en_echec()
    {
        var r = OptimizationResult.Fail("raté");

        Assert.False(r.Success);
        Assert.Equal("raté", r.Message);
        Assert.Equal(0, r.BytesFreed);
    }
}
