using System.Management;
using Loadout.Core.Common;
using Loadout.Core.Optimization;

namespace Loadout.Core.Backup;

/// <summary>
/// Crée des points de restauration système. C'est le filet de sécurité
/// principal : avant toute optimisation profonde, on peut revenir en arrière
/// via la restauration système Windows.
/// </summary>
public sealed class RestorePointService
{
    /// <summary>
    /// Crée un point de restauration. Nécessite que la protection système soit
    /// activée sur le lecteur système ; sinon Windows refuse l'opération.
    /// </summary>
    public OptimizationResult Create(string description)
    {
        try
        {
            var scope = new ManagementScope(@"\\localhost\root\default");
            using var process = new ManagementClass(scope,
                new ManagementPath("SystemRestore"), null);

            var inParams = process.GetMethodParameters("CreateRestorePoint");
            inParams["Description"] = description;
            inParams["RestorePointType"] = 12; // MODIFY_SETTINGS
            inParams["EventType"] = 100;        // BEGIN_SYSTEM_CHANGE

            var outParams = process.InvokeMethod("CreateRestorePoint", inParams, null);
            uint code = Convert.ToUInt32(outParams?["ReturnValue"] ?? 1u);

            return code == 0
                ? OptimizationResult.Ok("Point de restauration créé.")
                : OptimizationResult.Fail(
                    $"Création refusée (code {code}). La protection système est-elle activée ?");
        }
        catch (Exception ex)
        {
            return OptimizationResult.Fail($"Impossible de créer le point de restauration : {ex.Message}");
        }
    }

    /// <summary>Tente d'activer la protection système sur le lecteur C:.</summary>
    public OptimizationResult EnableSystemProtection()
    {
        var result = ProcessRunner.Run("powershell",
            "-NoProfile -Command \"Enable-ComputerRestore -Drive 'C:\\'\"");
        return result.Success
            ? OptimizationResult.Ok("Protection système activée sur C:.")
            : OptimizationResult.Fail($"Échec : {result.StdErr}");
    }
}
