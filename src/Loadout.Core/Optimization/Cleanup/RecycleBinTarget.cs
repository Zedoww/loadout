using System.Runtime.InteropServices;

namespace Loadout.Core.Optimization.Cleanup;

/// <summary>
/// Empties the Windows Recycle Bin via the Shell API. Scanning reports the size
/// currently held; cleaning empties it for every drive.
/// </summary>
public sealed class RecycleBinTarget : ICleanupTarget
{
    public CleanupCategory Category { get; } = new(
        Id: "recycle-bin",
        Name: "Recycle Bin",
        Description: "Permanently empties the Recycle Bin. Deleted files cannot be " +
                     "recovered afterwards.",
        Risk: CleanupRisk.Caution,
        SelectedByDefault: false);

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x01;
    private const uint SHERB_NOPROGRESSUI = 0x02;
    private const uint SHERB_NOSOUND = 0x04;
    private const int S_OK = 0;

    public long Scan()
    {
        try
        {
            var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
            // null root => aggregate across all drives.
            return SHQueryRecycleBin(null, ref info) == S_OK ? info.i64Size : 0;
        }
        catch
        {
            return 0; // Shell API unavailable: report nothing rather than crash.
        }
    }

    public OptimizationResult Clean()
    {
        long before = Scan();
        try
        {
            int hr = SHEmptyRecycleBin(IntPtr.Zero, null,
                SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);

            // S_OK on success; some systems return a non-zero HRESULT when the bin
            // was already empty — treat a now-empty bin as success.
            if (hr == S_OK || Scan() == 0)
                return OptimizationResult.Ok("Recycle Bin emptied.", Math.Max(before, 0));

            return OptimizationResult.Fail($"Could not empty the Recycle Bin (0x{hr:X8}).");
        }
        catch (Exception ex)
        {
            return OptimizationResult.Fail($"Could not empty the Recycle Bin: {ex.Message}");
        }
    }
}
