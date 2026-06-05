using Loadout.Core.Optimization;

namespace Loadout.Core.Tests;

public class PowerPlanServiceTests
{
    private const string ListOutput = """
        Paramètres d'alimentation existants (* = Actif)
        -----------------------------------

        GUID du mode d'alimentation : 381b4222-f694-41f0-9685-ff5bb260df2e  (Utilisation normale) *
        GUID du mode d'alimentation : 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (Performances élevées)
        GUID du mode d'alimentation : a1841308-3541-4fab-bc81-f71556f20b4a  (Économie d'énergie)
        """;

    [Fact]
    public void ParsePlans_extrait_tous_les_plans()
    {
        var plans = PowerPlanService.ParsePlans(ListOutput);
        Assert.Equal(3, plans.Count);
    }

    [Fact]
    public void ParsePlans_identifie_le_plan_actif()
    {
        var plans = PowerPlanService.ParsePlans(ListOutput);

        var active = Assert.Single(plans, p => p.IsActive);
        Assert.Equal(PowerPlanService.Balanced, active.Guid);
        Assert.Equal("Utilisation normale", active.Name);
    }

    [Fact]
    public void ParsePlans_lit_les_guid_et_noms()
    {
        var plans = PowerPlanService.ParsePlans(ListOutput);

        Assert.Contains(plans, p => p.Guid == PowerPlanService.HighPerformance
                                    && p.Name == "Performances élevées"
                                    && !p.IsActive);
    }

    [Fact]
    public void ParsePlans_chaine_vide_ne_jette_pas()
    {
        Assert.Empty(PowerPlanService.ParsePlans(string.Empty));
    }

    [Fact]
    public void ParseActivePlan_extrait_le_guid_actif()
    {
        const string output =
            "GUID du mode d'alimentation actif : 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (Performances élevées)";

        Assert.Equal(PowerPlanService.HighPerformance, PowerPlanService.ParseActivePlan(output));
    }

    [Fact]
    public void ParseActivePlan_retourne_null_si_absent()
    {
        Assert.Null(PowerPlanService.ParseActivePlan("aucun guid ici"));
    }
}
