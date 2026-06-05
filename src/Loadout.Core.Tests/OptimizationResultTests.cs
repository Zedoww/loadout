using Loadout.Core.Optimization;

namespace Loadout.Core.Tests;

public class OptimizationResultTests
{
    [Fact]
    public void Ok_creates_a_successful_result()
    {
        var r = OptimizationResult.Ok("done", 2048);

        Assert.True(r.Success);
        Assert.Equal("done", r.Message);
        Assert.Equal(2048, r.BytesFreed);
    }

    [Fact]
    public void Fail_creates_a_failed_result()
    {
        var r = OptimizationResult.Fail("nope");

        Assert.False(r.Success);
        Assert.Equal("nope", r.Message);
        Assert.Equal(0, r.BytesFreed);
    }
}
