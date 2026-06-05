using Loadout.Core.Optimization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Loadout.Core.Tests;

public class ProcessServiceTests
{
    private static ProcessService CreateSut() => new(NullLogger<ProcessService>.Instance);

    [Theory]
    [InlineData("chrome", true)]
    [InlineData("notepad", true)]
    [InlineData("explorer", false)]
    [InlineData("csrss", false)]
    [InlineData("System", false)]
    [InlineData("lsass", false)]
    [InlineData("Loadout", false)]
    public void IsSuspendable_protects_critical_processes(string name, bool expected)
    {
        Assert.Equal(expected, CreateSut().IsSuspendable(name));
    }

    [Fact]
    public void ListTopByMemory_respects_the_limit()
    {
        var list = CreateSut().ListTopByMemory(10);
        Assert.True(list.Count <= 10);
    }

    [Fact]
    public void ListTopByMemory_excludes_critical_processes()
    {
        var sut = CreateSut();
        var list = sut.ListTopByMemory();

        Assert.All(list, g => Assert.True(sut.IsSuspendable(g.Name)));
    }

    [Fact]
    public void ListTopByMemory_sorts_by_descending_memory()
    {
        var list = CreateSut().ListTopByMemory();

        for (int i = 1; i < list.Count; i++)
            Assert.True(list[i - 1].WorkingSetBytes >= list[i].WorkingSetBytes);
    }

    [Fact]
    public void Suspend_rejects_a_critical_process()
    {
        var result = CreateSut().Suspend("csrss");
        Assert.False(result.Success);
    }
}
