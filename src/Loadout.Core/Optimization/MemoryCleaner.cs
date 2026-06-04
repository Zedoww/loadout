using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Loadout.Core.Optimization;

/// <summary>
/// Libère la mémoire vive en réduisant le « working set » des processus.
/// Opération sans risque : Windows réalloue la mémoire aux processus selon
/// leurs besoins. Rien de permanent n'est modifié.
/// </summary>
public sealed class MemoryCleaner
{
    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    private static ulong GetAvailablePhysicalBytes()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        return GlobalMemoryStatusEx(ref status) ? status.ullAvailPhys : 0;
    }

    /// <summary>Vide le working set de tous les processus accessibles.</summary>
    public OptimizationResult Clean()
    {
        ulong before = GetAvailablePhysicalBytes();
        int trimmed = 0;

        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                EmptyWorkingSet(proc.Handle);
                trimmed++;
            }
            catch
            {
                // Process système protégé ou déjà terminé : on ignore.
            }
            finally
            {
                proc.Dispose();
            }
        }

        ulong after = GetAvailablePhysicalBytes();
        long freed = (long)after - (long)before;
        if (freed < 0) freed = 0;

        return OptimizationResult.Ok(
            $"Mémoire libérée sur {trimmed} processus.", freed);
    }
}
