using Loadout.Core.Optimization;

namespace Loadout.Core.Tests;

public class PowerPlanServiceTests
{
    private const string ListOutput = """
        Existing Power Schemes (* Active)
        -----------------------------------

        Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced) *
        Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (High performance)
        Power Scheme GUID: a1841308-3541-4fab-bc81-f71556f20b4a  (Power saver)
        """;

    [Fact]
    public void ParsePlans_extracts_every_plan()
    {
        var plans = PowerPlanService.ParsePlans(ListOutput);
        Assert.Equal(3, plans.Count);
    }

    [Fact]
    public void ParsePlans_identifies_the_active_plan()
    {
        var plans = PowerPlanService.ParsePlans(ListOutput);

        var active = Assert.Single(plans, p => p.IsActive);
        Assert.Equal(PowerPlanService.Balanced, active.Guid);
        Assert.Equal("Balanced", active.Name);
    }

    [Fact]
    public void ParsePlans_reads_guids_and_names()
    {
        var plans = PowerPlanService.ParsePlans(ListOutput);

        Assert.Contains(plans, p => p.Guid == PowerPlanService.HighPerformance
                                    && p.Name == "High performance"
                                    && !p.IsActive);
    }

    [Fact]
    public void ParsePlans_empty_string_does_not_throw()
    {
        Assert.Empty(PowerPlanService.ParsePlans(string.Empty));
    }

    [Fact]
    public void ParseActivePlan_extracts_the_active_guid()
    {
        const string output =
            "Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (High performance)";

        Assert.Equal(PowerPlanService.HighPerformance, PowerPlanService.ParseActivePlan(output));
    }

    [Fact]
    public void ParseActivePlan_returns_null_when_absent()
    {
        Assert.Null(PowerPlanService.ParseActivePlan("no guid here"));
    }
}
