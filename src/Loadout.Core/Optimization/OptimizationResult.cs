namespace Loadout.Core.Optimization;

/// <summary>Result of an optimization operation, to be shown to the user.</summary>
public sealed record OptimizationResult(bool Success, string Message, long BytesFreed = 0)
{
    public static OptimizationResult Ok(string message, long bytesFreed = 0) =>
        new(true, message, bytesFreed);

    public static OptimizationResult Fail(string message) =>
        new(false, message);
}
