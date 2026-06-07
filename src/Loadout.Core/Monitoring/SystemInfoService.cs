using System.Globalization;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;


namespace Loadout.Core.Monitoring;

#region Records

public sealed record SystemInfo
{
    public OsInfo Os { get; init; } = new();
    public CpuInfo Cpu { get; init; } = new();
    public GpuInfo Gpu { get; init; } = new();
    public RamInfo Ram { get; init; } = new();
    public StorageInfo Storage { get; init; } = new();
    public MotherboardInfo Motherboard { get; init; } = new();
    public IReadOnlyList<NetworkAdapterInfo> Network { get; init; } = [];
    public IReadOnlyList<AudioDeviceInfo> Audio { get; init; } = [];
    public DateTime CollectedAt { get; init; } = DateTime.Now;
}

public sealed record OsInfo
{
    public string? Version { get; init; }
    public string? Architecture { get; init; }
    public DateTime? InstallDate { get; init; }
    public DateTime? LastBootTime { get; init; }
    public string? ComputerName { get; init; }
    public string? UserName { get; init; }
    public string? Locale { get; init; }
}

public sealed record CpuInfo
{
    public string? Name { get; init; }
    public string? Architecture { get; init; }
    public int? Cores { get; init; }
    public int? Threads { get; init; }
    public double? BaseClockGhz { get; init; }
    public string? L2Cache { get; init; }
    public string? L3Cache { get; init; }
    public double? CurrentClockGhz { get; init; }
}

public sealed record GpuInfo
{
    public string? Name { get; init; }
    public string? DriverVersion { get; init; }
    public string? Vram { get; init; }
    public string? DriverDate { get; init; }
    public string? Resolution { get; init; }
}

public sealed record RamInfo
{
    public string? TotalPhysical { get; init; }
    public int? DimmCount { get; init; }
    public IReadOnlyList<RamStickInfo> Sticks { get; init; } = [];
}

public sealed record RamStickInfo
{
    public string? Capacity { get; init; }
    public int? SpeedMhz { get; init; }
    public string? MemoryType { get; init; }
    public string? Manufacturer { get; init; }
    public string? FormFactor { get; init; }
}

public sealed record StorageInfo
{
    public IReadOnlyList<PhysicalDriveInfo> Drives { get; init; } = [];
    public IReadOnlyList<LogicalVolumeInfo> Volumes { get; init; } = [];
}

public sealed record PhysicalDriveInfo
{
    public string? Model { get; init; }
    public string? Size { get; init; }
    public string? MediaType { get; init; }
    public string? InterfaceType { get; init; }
    public string? Status { get; init; }
}

public sealed record LogicalVolumeInfo
{
    public string? Letter { get; init; }
    public string? Label { get; init; }
    public string? FileSystem { get; init; }
    public string? TotalSize { get; init; }
    public string? FreeSpace { get; init; }
}

public sealed record MotherboardInfo
{
    public string? Manufacturer { get; init; }
    public string? Product { get; init; }
    public string? BiosVersion { get; init; }
    public string? BiosDate { get; init; }
}

public sealed record NetworkAdapterInfo
{
    public string? Name { get; init; }
    public string? ConnectionName { get; init; }
    public string? MacAddress { get; init; }
    public string? IPv4 { get; init; }
    public string? IPv6 { get; init; }
    public string? Speed { get; init; }
    public string? Type { get; init; }
}

public sealed record AudioDeviceInfo
{
    public string? Name { get; init; }
    public string? Status { get; init; }
}

#endregion

/// <summary>
/// Collects a comprehensive snapshot of the system configuration using WMI,
/// environment variables, and .NET APIs. Each category is queried independently
/// so that partial failures (e.g. limited permissions) do not prevent the rest
/// of the data from being gathered.
/// </summary>
public sealed class SystemInfoService
{
    private readonly ILogger<SystemInfoService> _logger;

    public SystemInfoService(ILogger<SystemInfoService> logger)
    {
        _logger = logger;
    }

    /// <summary>Collects every available piece of system information.</summary>
    public SystemInfo Collect()
    {
        return new SystemInfo
        {
            Os = CollectOs(),
            Cpu = CollectCpu(),
            Gpu = CollectGpu(),
            Ram = CollectRam(),
            Storage = CollectStorage(),
            Motherboard = CollectMotherboard(),
            Network = CollectNetwork(),
            Audio = CollectAudio(),
        };
    }

    #region OS

    private OsInfo CollectOs()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    string? caption = obj["Caption"]?.ToString()?.Trim();
                    string? version = obj["Version"]?.ToString();
                    string? buildNumber = obj["BuildNumber"]?.ToString();
                    string displayVersion = GetDisplayVersion();

                    string fullVersion = string.Join(" ", new[]
                    {
                        caption, displayVersion, $"Build {buildNumber}"
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    return new OsInfo
                    {
                        Version = fullVersion,
                        Architecture = Environment.Is64BitOperatingSystem
                            ? (IsArm64() ? "ARM64" : "x64")
                            : "x86",
                        InstallDate = ParseWmiDateTime(obj["InstallDate"]?.ToString()),
                        LastBootTime = ParseWmiDateTime(obj["LastBootUpTime"]?.ToString()),
                        ComputerName = Environment.MachineName,
                        UserName = Environment.UserName,
                        Locale = CultureInfo.InstalledUICulture.DisplayName,
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect OS information.");
        }

        // Fallback when WMI fails.
        return new OsInfo
        {
            Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            ComputerName = Environment.MachineName,
            UserName = Environment.UserName,
            Locale = CultureInfo.InstalledUICulture.DisplayName,
        };
    }

    private static string GetDisplayVersion()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return key?.GetValue("DisplayVersion")?.ToString() ?? "";
        }
        catch { return ""; }
    }

    private static bool IsArm64()
    {
        try
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                || RuntimeInformation.OSArchitecture == Architecture.Arm64;
        }
        catch { return false; }
    }

    #endregion

    #region CPU

    private CpuInfo CollectCpu()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    return new CpuInfo
                    {
                        Name = obj["Name"]?.ToString()?.Trim(),
                        Architecture = MapCpuArchitecture(GetInt(obj, "Architecture")),
                        Cores = GetInt(obj, "NumberOfCores"),
                        Threads = GetInt(obj, "NumberOfLogicalProcessors"),
                        BaseClockGhz = GetInt(obj, "MaxClockSpeed") is int mhz and > 0
                            ? Math.Round(mhz / 1000.0, 2) : null,
                        L2Cache = FormatCacheSize(GetLong(obj, "L2CacheSize")),
                        L3Cache = FormatCacheSize(GetLong(obj, "L3CacheSize")),
                        CurrentClockGhz = GetInt(obj, "CurrentClockSpeed") is int cur and > 0
                            ? Math.Round(cur / 1000.0, 2) : null,
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect CPU information.");
        }

        return new CpuInfo();
    }

    private static string? MapCpuArchitecture(int? arch) => arch switch
    {
        0 => "x86",
        5 => "ARM",
        6 => "Itanium",
        9 => "x64",
        12 => "ARM64",
        _ => arch?.ToString(),
    };

    private static string? FormatCacheSize(long? kb)
    {
        if (kb is null or 0) return null;
        return kb >= 1024
            ? $"{kb / 1024} MB"
            : $"{kb} KB";
    }

    #endregion

    #region GPU

    private GpuInfo CollectGpu()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var results = searcher.Get();

            // Pick the adapter with the most VRAM (typically the discrete GPU).
            ManagementObject? best = null;
            long bestVram = -1;

            foreach (ManagementObject obj in results)
            {
                long vram = GetLong(obj, "AdapterRAM") ?? 0;
                if (vram > bestVram)
                {
                    best?.Dispose();
                    best = obj;
                    bestVram = vram;
                }
                else
                {
                    obj.Dispose();
                }
            }

            if (best is null) return new GpuInfo();

            using (best)
            {
                string? driverDate = best["DriverDate"]?.ToString();
                string? hRes = best["CurrentHorizontalResolution"]?.ToString();
                string? vRes = best["CurrentVerticalResolution"]?.ToString();

                return new GpuInfo
                {
                    Name = best["Name"]?.ToString()?.Trim(),
                    DriverVersion = best["DriverVersion"]?.ToString(),
                    Vram = bestVram > 0 ? FormatBytes(bestVram) : null,
                    DriverDate = ParseWmiDateTime(driverDate)?.ToString("yyyy-MM-dd"),
                    Resolution = hRes is not null && vRes is not null
                        ? $"{hRes}x{vRes}" : null,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect GPU information.");
        }

        return new GpuInfo();
    }

    #endregion

    #region RAM

    private RamInfo CollectRam()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            using var results = searcher.Get();

            var sticks = new List<RamStickInfo>();
            long totalBytes = 0;

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    long capacity = GetLong(obj, "Capacity") ?? 0;
                    totalBytes += capacity;

                    sticks.Add(new RamStickInfo
                    {
                        Capacity = capacity > 0 ? FormatBytes(capacity) : null,
                        SpeedMhz = GetInt(obj, "ConfiguredClockSpeed")
                                ?? GetInt(obj, "Speed"),
                        MemoryType = MapMemoryType(GetInt(obj, "SMBIOSMemoryType")),
                        Manufacturer = obj["Manufacturer"]?.ToString()?.Trim(),
                        FormFactor = MapFormFactor(GetInt(obj, "FormFactor")),
                    });
                }
            }

            return new RamInfo
            {
                TotalPhysical = totalBytes > 0 ? FormatBytes(totalBytes) : null,
                DimmCount = sticks.Count,
                Sticks = sticks,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect RAM information.");
        }

        return new RamInfo();
    }

    private static string? MapMemoryType(int? type) => type switch
    {
        20 => "DDR",
        21 => "DDR2",
        22 => "DDR2 FB-DIMM",
        24 => "DDR3",
        26 => "DDR4",
        30 => "LPDDR4",
        34 => "DDR5",
        35 => "LPDDR5",
        _ => type is not null and not 0 ? $"Unknown ({type})" : null,
    };

    private static string? MapFormFactor(int? ff) => ff switch
    {
        1 => "Other",
        2 => "SIP",
        3 => "DIP",
        5 => "SOJ",
        7 => "SIMM",
        8 => "DIMM",
        9 => "TSOP",
        12 => "RIMM",
        13 => "SODIMM",
        14 => "SRIMM",
        15 => "SMD",
        _ => ff is not null and not 0 ? $"Unknown ({ff})" : null,
    };

    #endregion

    #region Storage

    private StorageInfo CollectStorage()
    {
        return new StorageInfo
        {
            Drives = CollectPhysicalDrives(),
            Volumes = CollectLogicalVolumes(),
        };
    }

    private IReadOnlyList<PhysicalDriveInfo> CollectPhysicalDrives()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            using var results = searcher.Get();

            var drives = new List<PhysicalDriveInfo>();
            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    long? size = GetLong(obj, "Size");
                    string? mediaType = obj["MediaType"]?.ToString();

                    drives.Add(new PhysicalDriveInfo
                    {
                        Model = obj["Model"]?.ToString()?.Trim(),
                        Size = size > 0 ? FormatBytes(size.Value) : null,
                        MediaType = ClassifyMediaType(mediaType),
                        InterfaceType = obj["InterfaceType"]?.ToString(),
                        Status = obj["Status"]?.ToString(),
                    });
                }
            }

            return drives;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect physical drive information.");
            return [];
        }
    }

    private static string? ClassifyMediaType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.Contains("SSD", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("Solid", StringComparison.OrdinalIgnoreCase))
            return "SSD";
        if (raw.Contains("HDD", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("Hard", StringComparison.OrdinalIgnoreCase))
            return "HDD";
        return raw;
    }

    private IReadOnlyList<LogicalVolumeInfo> CollectLogicalVolumes()
    {
        try
        {
            // DriveType = 3 → local fixed disks only.
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3");
            using var results = searcher.Get();

            var volumes = new List<LogicalVolumeInfo>();
            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    long? total = GetLong(obj, "Size");
                    long? free = GetLong(obj, "FreeSpace");

                    volumes.Add(new LogicalVolumeInfo
                    {
                        Letter = obj["DeviceID"]?.ToString(),
                        Label = obj["VolumeName"]?.ToString(),
                        FileSystem = obj["FileSystem"]?.ToString(),
                        TotalSize = total > 0 ? FormatBytes(total.Value) : null,
                        FreeSpace = free > 0 ? FormatBytes(free.Value) : null,
                    });
                }
            }

            return volumes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect logical volume information.");
            return [];
        }
    }

    #endregion

    #region Motherboard & BIOS

    private MotherboardInfo CollectMotherboard()
    {
        string? manufacturer = null, product = null, biosVersion = null, biosDate = null;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    manufacturer = obj["Manufacturer"]?.ToString()?.Trim();
                    product = obj["Product"]?.ToString()?.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect motherboard information.");
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    biosVersion = obj["SMBIOSBIOSVersion"]?.ToString()?.Trim();
                    biosDate = ParseWmiDateTime(obj["ReleaseDate"]?.ToString())
                        ?.ToString("yyyy-MM-dd");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect BIOS information.");
        }

        return new MotherboardInfo
        {
            Manufacturer = manufacturer,
            Product = product,
            BiosVersion = biosVersion,
            BiosDate = biosDate,
        };
    }

    #endregion

    #region Network

    private IReadOnlyList<NetworkAdapterInfo> CollectNetwork()
    {
        try
        {
            var adapters = new List<NetworkAdapterInfo>();

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback
                    or NetworkInterfaceType.Tunnel)
                    continue;

                var props = nic.GetIPProperties();
                string? ipv4 = null, ipv6 = null;

                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        ipv4 ??= addr.Address.ToString();
                    else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6
                             && !addr.Address.IsIPv6LinkLocal)
                        ipv6 ??= addr.Address.ToString();
                }

                string type = nic.NetworkInterfaceType switch
                {
                    NetworkInterfaceType.Wireless80211 => "WiFi",
                    NetworkInterfaceType.Ethernet => "Ethernet",
                    _ => nic.NetworkInterfaceType.ToString(),
                };

                long speedBits = nic.Speed;
                string speed = speedBits >= 1_000_000_000
                    ? $"{speedBits / 1_000_000_000} Gbps"
                    : $"{speedBits / 1_000_000} Mbps";

                adapters.Add(new NetworkAdapterInfo
                {
                    Name = nic.Description,
                    ConnectionName = nic.Name,
                    MacAddress = FormatMac(nic.GetPhysicalAddress()),
                    IPv4 = ipv4,
                    IPv6 = ipv6,
                    Speed = speed,
                    Type = type,
                });
            }

            return adapters;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect network adapter information.");
            return [];
        }
    }

    private static string? FormatMac(PhysicalAddress? mac)
    {
        if (mac is null) return null;
        byte[] bytes = mac.GetAddressBytes();
        return bytes.Length == 0
            ? null
            : string.Join(":", bytes.Select(b => b.ToString("X2")));
    }

    #endregion

    #region Audio

    private IReadOnlyList<AudioDeviceInfo> CollectAudio()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
            using var results = searcher.Get();

            var devices = new List<AudioDeviceInfo>();
            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    devices.Add(new AudioDeviceInfo
                    {
                        Name = obj["Name"]?.ToString()?.Trim(),
                        Status = obj["Status"]?.ToString(),
                    });
                }
            }

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect audio device information.");
            return [];
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Converts a WMI datetime string (yyyyMMddHHmmss.ffffff±zzz) to
    /// <see cref="DateTime"/>.
    /// </summary>
    private static DateTime? ParseWmiDateTime(string? wmi)
    {
        if (string.IsNullOrWhiteSpace(wmi)) return null;
        try
        {
            return ManagementDateTimeConverter.ToDateTime(wmi);
        }
        catch
        {
            return null;
        }
    }

    private static int? GetInt(ManagementBaseObject obj, string property)
    {
        try
        {
            object? val = obj[property];
            return val is not null ? Convert.ToInt32(val) : null;
        }
        catch { return null; }
    }

    private static long? GetLong(ManagementBaseObject obj, string property)
    {
        try
        {
            object? val = obj[property];
            return val is not null ? Convert.ToInt64(val) : null;
        }
        catch { return null; }
    }

    /// <summary>Formats a byte count into a human-readable string (KB/MB/GB/TB).</summary>
    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1L << 40 => $"{bytes / (double)(1L << 40):F2} TB",
            >= 1L << 30 => $"{bytes / (double)(1L << 30):F2} GB",
            >= 1L << 20 => $"{bytes / (double)(1L << 20):F2} MB",
            >= 1L << 10 => $"{bytes / (double)(1L << 10):F2} KB",
            _ => $"{bytes} B",
        };
    }

    #endregion
}
