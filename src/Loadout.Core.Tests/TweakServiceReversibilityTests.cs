using Loadout.Core.Optimization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

namespace Loadout.Core.Tests;

/// <summary>
/// Proves the non-negotiable promise: a tweak restores the system to its exact
/// prior state. Uses a throwaway HKCU key and an isolated backup directory.
/// </summary>
public class TweakServiceReversibilityTests : IDisposable
{
    private readonly string _dataDir;
    private readonly string _subKey;
    private readonly TweakDefinition _def;

    public TweakServiceReversibilityTests()
    {
        _dataDir = Path.Combine(Path.GetTempPath(), "loadout-tweak-tests-" + Guid.NewGuid().ToString("N"));
        _subKey = @"Software\Loadout.Tests\" + Guid.NewGuid().ToString("N");
        _def = new TweakDefinition(
            Id: "test-tweak",
            Category: "Test",
            Name: "Test tweak",
            Description: "Throwaway tweak for reversibility tests.",
            Root: RegistryRoot.CurrentUser,
            SubKey: _subKey,
            ValueName: "TestValue",
            EnabledValue: 1,
            DefaultValue: 0);
    }

    private TweakService NewService() => new(NullLogger<TweakService>.Instance, _dataDir);

    [Fact]
    public void Apply_then_Revert_removes_a_value_that_did_not_exist()
    {
        var service = NewService();
        Assert.Null(service.ReadCurrent(_def));          // nothing there initially

        Assert.True(service.Apply(_def).Success);
        Assert.True(service.IsApplied(_def));
        Assert.Equal(1, service.ReadCurrent(_def));

        Assert.True(service.Revert(_def).Success);
        Assert.Null(service.ReadCurrent(_def));           // restored to "absent"
        Assert.False(service.IsApplied(_def));
    }

    [Fact]
    public void Apply_then_Revert_restores_the_exact_original_value()
    {
        // Seed a pre-existing value of 5.
        using (var key = Registry.CurrentUser.CreateSubKey(_subKey, writable: true))
            key.SetValue(_def.ValueName, 5, RegistryValueKind.DWord);

        var service = NewService();
        Assert.True(service.Apply(_def).Success);
        Assert.Equal(1, service.ReadCurrent(_def));

        Assert.True(service.Revert(_def).Success);
        Assert.Equal(5, service.ReadCurrent(_def));       // exact original, not the default
    }

    public void Dispose()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Loadout.Tests", throwOnMissingSubKey: false); } catch { }
        try { Directory.Delete(_dataDir, recursive: true); } catch { }
    }
}
