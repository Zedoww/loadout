using System.Management;
using Loadout.Core.Common;
using Loadout.Core.Optimization;

namespace Loadout.Core.Backup;

/// <summary>
/// Creates Windows System Restore points. This is the main safety net: before
/// any deep optimization, the user can roll back through System Restore.
/// </summary>
public sealed class RestorePointService
{
    /// <summary>
    /// Creates a restore point. Requires System Protection to be enabled on the
    /// system drive; otherwise Windows rejects the operation.
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
                ? OptimizationResult.Ok("Restore point created.")
                : OptimizationResult.Fail(
                    $"Creation refused (code {code}). Is System Protection enabled?");
        }
        catch (Exception ex)
        {
            return OptimizationResult.Fail($"Could not create the restore point: {ex.Message}");
        }
    }

    /// <summary>Attempts to enable System Protection on drive C:.</summary>
    public OptimizationResult EnableSystemProtection()
    {
        var result = ProcessRunner.Run("powershell",
            "-NoProfile -Command \"Enable-ComputerRestore -Drive 'C:\\'\"");
        return result.Success
            ? OptimizationResult.Ok("System Protection enabled on C:.")
            : OptimizationResult.Fail($"Failed: {result.StdErr}");
    }
}
