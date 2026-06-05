using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Loadout.Core.Optimization;

/// <summary>
/// Frees RAM by trimming each process's working set. This is risk-free: Windows
/// re-allocates memory to processes as they need it. Nothing permanent changes.
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

    /// <summary>Empties the working set of every accessible process.</summary>
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
                // Protected system process or already exited: ignore.
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
            $"Memory trimmed on {trimmed} processes.", freed);
    }
}
