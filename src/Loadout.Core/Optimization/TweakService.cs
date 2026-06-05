using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Loadout.Core.Optimization;

public enum RegistryRoot { LocalMachine, CurrentUser }

/// <summary>
/// Declarative definition of a single-DWORD registry tweak.
/// </summary>
public sealed record TweakDefinition(
    string Id,
    string Category,
    string Name,
    string Description,
    RegistryRoot Root,
    string SubKey,
    string ValueName,
    int EnabledValue,
    int DefaultValue,
    bool RequiresReboot = false);

/// <summary>
/// Applies and restores **reversible** registry optimizations. Before a key is
/// modified for the first time, its original value is saved to a JSON backup;
/// reverting restores that exact value (or deletes the value if it did not exist).
/// </summary>
public sealed class TweakService
{
    private readonly ILogger<TweakService> _logger;
    private readonly string _backupPath;
    private readonly Dictionary<string, int?> _backups;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private const string MultimediaProfile =
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";

    public TweakService(ILogger<TweakService> logger)
    {
        _logger = logger;

        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loadout");
        Directory.CreateDirectory(dir);
        _backupPath = Path.Combine(dir, "tweaks-backup.json");
        _backups = LoadBackups();
    }

    /// <summary>The catalog of tweaks, all documented and reversible.</summary>
    public IReadOnlyList<TweakDefinition> Definitions { get; } = new List<TweakDefinition>
    {
        // ---------------------------- Performance ----------------------------
        new("game-dvr", "Performance",
            "Disable Game DVR",
            "Turns off the Xbox Game Bar background recording that eats CPU/GPU.",
            RegistryRoot.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, 1),

        new("game-dvr-policy", "Performance",
            "Disable Game DVR (system policy)",
            "Forces Game DVR off machine-wide.",
            RegistryRoot.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, 1),

        new("hags", "Performance",
            "Hardware-accelerated GPU scheduling",
            "Lets the GPU manage its own queue, lowering latency. Requires a reboot.",
            RegistryRoot.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode",
            2, 1, RequiresReboot: true),

        new("system-responsiveness", "Performance",
            "Game-oriented system responsiveness",
            "Reserves 0% of the CPU for background multimedia tasks (default 20%).",
            RegistryRoot.LocalMachine, MultimediaProfile, "SystemResponsiveness", 0, 20),

        new("network-throttling", "Performance",
            "Disable network throttling",
            "Removes the throughput cap Windows applies to multimedia apps.",
            RegistryRoot.LocalMachine, MultimediaProfile, "NetworkThrottlingIndex",
            unchecked((int)0xFFFFFFFF), 10),

        new("games-priority", "Performance",
            "High priority for games",
            "Raises the scheduling priority of the 'Games' task category.",
            RegistryRoot.LocalMachine, MultimediaProfile + @"\Tasks\Games", "Priority", 6, 2),

        new("startup-delay", "Performance",
            "Remove startup app delay",
            "Skips the artificial delay Windows adds before launching startup apps.",
            RegistryRoot.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0, 1),

        new("background-apps", "Performance",
            "Disable background apps",
            "Stops Store apps from running in the background, freeing CPU and RAM.",
            RegistryRoot.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1, 0),

        // ------------------------------ Privacy ------------------------------
        new("telemetry", "Privacy",
            "Disable telemetry",
            "Sets diagnostic data collection to the lowest level allowed.",
            RegistryRoot.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, 1),

        new("advertising-id", "Privacy",
            "Disable advertising ID",
            "Prevents apps from using your advertising ID to profile you.",
            RegistryRoot.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, 1),

        new("cortana", "Privacy",
            "Disable Cortana",
            "Disables the Cortana assistant and its background services.",
            RegistryRoot.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, 1),

        new("bing-search", "Privacy",
            "Disable web search in Start menu",
            "Stops the Start menu from sending your searches to Bing.",
            RegistryRoot.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0, 1),

        new("activity-history", "Privacy",
            "Disable activity history",
            "Stops Windows from collecting your app and document timeline.",
            RegistryRoot.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0, 1),

        new("suggestions", "Privacy",
            "Disable tips & suggestions",
            "Turns off suggested content and ads in the Start menu and Settings.",
            RegistryRoot.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, 1),

        // ----------------------------- Interface -----------------------------
        new("show-extensions", "Interface",
            "Show file extensions",
            "Always reveals file extensions in Explorer (safer and clearer).",
            RegistryRoot.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0, 1),

        new("transparency", "Interface",
            "Disable transparency effects",
            "Turns off Acrylic/transparency for a small GPU and battery saving.",
            RegistryRoot.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, 1),

        new("widgets", "Interface",
            "Disable Widgets",
            "Removes the Windows 11 Widgets / News and Interests panel.",
            RegistryRoot.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0, 1),
    };

    public int? ReadCurrent(TweakDefinition def)
    {
        try
        {
            using var baseKey = OpenBase(def.Root);
            using var key = baseKey.OpenSubKey(def.SubKey);
            return key?.GetValue(def.ValueName) is int v ? v : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read tweak {Id}.", def.Id);
            return null;
        }
    }

    public bool IsApplied(TweakDefinition def) => ReadCurrent(def) == def.EnabledValue;

    public OptimizationResult Apply(TweakDefinition def)
    {
        try
        {
            // Save the original value once.
            if (!_backups.ContainsKey(def.Id))
            {
                _backups[def.Id] = ReadCurrent(def);
                SaveBackups();
            }

            using var baseKey = OpenBase(def.Root);
            using var key = baseKey.CreateSubKey(def.SubKey, writable: true);
            key.SetValue(def.ValueName, def.EnabledValue, RegistryValueKind.DWord);

            _logger.LogInformation("Tweak {Id} applied.", def.Id);
            return OptimizationResult.Ok(def.RequiresReboot
                ? $"'{def.Name}' applied (reboot required)."
                : $"'{def.Name}' applied.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply tweak {Id}.", def.Id);
            return OptimizationResult.Fail($"Failed: {ex.Message}");
        }
    }

    public OptimizationResult Revert(TweakDefinition def)
    {
        try
        {
            using var baseKey = OpenBase(def.Root);

            if (_backups.TryGetValue(def.Id, out int? original))
            {
                if (original is null)
                {
                    // The value did not exist originally: remove it.
                    using var key = baseKey.OpenSubKey(def.SubKey, writable: true);
                    key?.DeleteValue(def.ValueName, throwOnMissingValue: false);
                }
                else
                {
                    using var key = baseKey.CreateSubKey(def.SubKey, writable: true);
                    key.SetValue(def.ValueName, original.Value, RegistryValueKind.DWord);
                }

                _backups.Remove(def.Id);
                SaveBackups();
            }
            else
            {
                // No backup: fall back to the Windows default value.
                using var key = baseKey.CreateSubKey(def.SubKey, writable: true);
                key.SetValue(def.ValueName, def.DefaultValue, RegistryValueKind.DWord);
            }

            _logger.LogInformation("Tweak {Id} reverted.", def.Id);
            return OptimizationResult.Ok($"'{def.Name}' reverted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert tweak {Id}.", def.Id);
            return OptimizationResult.Fail($"Failed: {ex.Message}");
        }
    }

    private static RegistryKey OpenBase(RegistryRoot root) =>
        RegistryKey.OpenBaseKey(
            root == RegistryRoot.LocalMachine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            RegistryView.Registry64);

    private Dictionary<string, int?> LoadBackups()
    {
        try
        {
            return File.Exists(_backupPath)
                ? JsonSerializer.Deserialize<Dictionary<string, int?>>(File.ReadAllText(_backupPath)) ?? new()
                : new();
        }
        catch { return new(); }
    }

    private void SaveBackups()
    {
        try { File.WriteAllText(_backupPath, JsonSerializer.Serialize(_backups, JsonOpts)); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not save tweak backups."); }
    }
}
