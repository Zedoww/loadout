namespace Opti.Core.Optimization;

/// <summary>Résultat d'une opération d'optimisation, à afficher à l'utilisateur.</summary>
public sealed record OptimizationResult(bool Success, string Message, long BytesFreed = 0)
{
    public static OptimizationResult Ok(string message, long bytesFreed = 0) =>
        new(true, message, bytesFreed);

    public static OptimizationResult Fail(string message) =>
        new(false, message);
}
