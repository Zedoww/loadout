using System.Text.RegularExpressions;
using Loadout.Core.Common;

namespace Loadout.Core.Optimization;

public sealed record PowerPlan(Guid Guid, string Name, bool IsActive);

/// <summary>
/// Gère les plans d'alimentation Windows via <c>powercfg</c>.
/// Toutes les opérations sont réversibles : on mémorise le plan actif
/// avant d'en imposer un autre.
/// </summary>
public sealed class PowerPlanService
{
    // GUID standards Windows.
    public static readonly Guid Balanced = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    public static readonly Guid HighPerformance = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    public static readonly Guid UltimatePerformance = new("e9a42b02-d5df-448d-aa00-03f14749eb61");

    private static readonly Regex SchemeLine =
        new(@"GUID.*?:\s*([0-9a-fA-F\-]{36})\s*\((.*?)\)(\s*\*?)", RegexOptions.Compiled);

    public IReadOnlyList<PowerPlan> ListPlans() =>
        ParsePlans(ProcessRunner.Run("powercfg", "/list").StdOut);

    public Guid? GetActivePlan() =>
        ParseActivePlan(ProcessRunner.Run("powercfg", "/getactivescheme").StdOut);

    /// <summary>Parse la sortie de <c>powercfg /list</c> (logique pure, testable).</summary>
    internal static IReadOnlyList<PowerPlan> ParsePlans(string powercfgOutput)
    {
        var plans = new List<PowerPlan>();
        foreach (Match m in SchemeLine.Matches(powercfgOutput))
        {
            if (Guid.TryParse(m.Groups[1].Value, out var guid))
            {
                bool active = m.Value.TrimEnd().EndsWith('*');
                plans.Add(new PowerPlan(guid, m.Groups[2].Value.Trim(), active));
            }
        }
        return plans;
    }

    /// <summary>Parse la sortie de <c>powercfg /getactivescheme</c> (logique pure, testable).</summary>
    internal static Guid? ParseActivePlan(string powercfgOutput)
    {
        var m = Regex.Match(powercfgOutput, @"([0-9a-fA-F\-]{36})");
        return m.Success && Guid.TryParse(m.Value, out var g) ? g : null;
    }

    public OptimizationResult SetActivePlan(Guid guid)
    {
        var result = ProcessRunner.Run("powercfg", $"/setactive {guid}");
        return result.Success
            ? OptimizationResult.Ok($"Plan d'alimentation activé : {guid}")
            : OptimizationResult.Fail($"Échec activation du plan : {result.StdErr}");
    }

    /// <summary>
    /// S'assure que le plan « Performances élevées » est disponible et l'active.
    /// Retourne le GUID du plan effectivement activé.
    /// </summary>
    public OptimizationResult ActivateHighPerformance()
    {
        // Le plan Performances élevées est presque toujours présent ; sinon on le crée.
        if (!ListPlans().Any(p => p.Guid == HighPerformance))
            ProcessRunner.Run("powercfg", $"-duplicatescheme {HighPerformance}");

        return SetActivePlan(HighPerformance);
    }
}
