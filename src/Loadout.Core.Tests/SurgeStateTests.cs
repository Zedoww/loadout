using System.Text.Json;
using Loadout.Core.Optimization;

namespace Loadout.Core.Tests;

public class SurgeStateTests
{
    [Fact]
    public void SurgeState_survit_a_un_aller_retour_json()
    {
        var original = new SurgeState
        {
            PreviousPowerPlan = PowerPlanService.Balanced,
            AppliedAt = new DateTime(2026, 6, 4, 23, 0, 0, DateTimeKind.Local),
        };

        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SurgeState>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.PreviousPowerPlan, restored!.PreviousPowerPlan);
        Assert.Equal(original.AppliedAt, restored.AppliedAt);
    }

    [Fact]
    public void SurgeState_gere_un_plan_precedent_absent()
    {
        var original = new SurgeState { PreviousPowerPlan = null };

        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SurgeState>(json);

        Assert.NotNull(restored);
        Assert.Null(restored!.PreviousPowerPlan);
    }
}
