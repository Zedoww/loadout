using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Loadout.Core.Optimization;

public enum RegistryRoot { LocalMachine, CurrentUser }

/// <summary>
/// Définition déclarative d'un tweak registre à valeur DWORD unique.
/// </summary>
public sealed record TweakDefinition(
    string Id,
    string Name,
    string Description,
    RegistryRoot Root,
    string SubKey,
    string ValueName,
    int EnabledValue,
    int DefaultValue,
    bool RequiresReboot = false);

/// <summary>
/// Applique et restaure des optimisations registre **réversibles**. Avant la
/// première modification d'une clé, sa valeur d'origine est sauvegardée dans un
/// fichier JSON ; la restauration remet exactement cette valeur (ou supprime la
/// valeur si elle n'existait pas).
/// </summary>
public sealed class TweakService
{
    private readonly ILogger<TweakService> _logger;
    private readonly string _backupPath;
    private readonly Dictionary<string, int?> _backups;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public TweakService(ILogger<TweakService> logger)
    {
        _logger = logger;

        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loadout");
        Directory.CreateDirectory(dir);
        _backupPath = Path.Combine(dir, "tweaks-backup.json");
        _backups = LoadBackups();
    }

    /// <summary>Catalogue des tweaks proposés, tous documentés et réversibles.</summary>
    public IReadOnlyList<TweakDefinition> Definitions { get; } = new List<TweakDefinition>
    {
        new("game-dvr",
            "Désactiver Game DVR",
            "Coupe l'enregistrement en arrière-plan de la Xbox Game Bar, qui consomme CPU/GPU.",
            RegistryRoot.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled",
            EnabledValue: 0, DefaultValue: 1),

        new("game-dvr-policy",
            "Désactiver Game DVR (stratégie système)",
            "Force la désactivation de Game DVR au niveau machine.",
            RegistryRoot.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR",
            EnabledValue: 0, DefaultValue: 1),

        new("hags",
            "Planification GPU accélérée par le matériel",
            "Active le HAGS : le GPU gère sa propre file, réduisant la latence. Redémarrage requis.",
            RegistryRoot.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode",
            EnabledValue: 2, DefaultValue: 1, RequiresReboot: true),

        new("system-responsiveness",
            "Réactivité système orientée jeu",
            "Réserve 0 % du CPU aux tâches d'arrière-plan multimédia (défaut : 20 %).",
            RegistryRoot.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness",
            EnabledValue: 0, DefaultValue: 20),

        new("network-throttling",
            "Désactiver le bridage réseau",
            "Supprime la limite de débit réseau imposée aux applications multimédia.",
            RegistryRoot.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex",
            EnabledValue: unchecked((int)0xFFFFFFFF), DefaultValue: 10),

        new("games-priority",
            "Priorité élevée aux jeux",
            "Augmente la priorité de planification de la catégorie de tâches « Games ».",
            RegistryRoot.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority",
            EnabledValue: 6, DefaultValue: 2),
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
            _logger.LogWarning(ex, "Lecture du tweak {Id} impossible.", def.Id);
            return null;
        }
    }

    public bool IsApplied(TweakDefinition def) => ReadCurrent(def) == def.EnabledValue;

    public OptimizationResult Apply(TweakDefinition def)
    {
        try
        {
            // Sauvegarde de la valeur d'origine (une seule fois).
            if (!_backups.ContainsKey(def.Id))
            {
                _backups[def.Id] = ReadCurrent(def);
                SaveBackups();
            }

            using var baseKey = OpenBase(def.Root);
            using var key = baseKey.CreateSubKey(def.SubKey, writable: true);
            key.SetValue(def.ValueName, def.EnabledValue, RegistryValueKind.DWord);

            _logger.LogInformation("Tweak {Id} appliqué.", def.Id);
            return OptimizationResult.Ok(def.RequiresReboot
                ? $"« {def.Name} » appliqué (redémarrage requis)."
                : $"« {def.Name} » appliqué.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec d'application du tweak {Id}.", def.Id);
            return OptimizationResult.Fail($"Échec : {ex.Message}");
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
                    // La valeur n'existait pas à l'origine : on la supprime.
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
                // Pas de sauvegarde : on remet la valeur par défaut de Windows.
                using var key = baseKey.CreateSubKey(def.SubKey, writable: true);
                key.SetValue(def.ValueName, def.DefaultValue, RegistryValueKind.DWord);
            }

            _logger.LogInformation("Tweak {Id} restauré.", def.Id);
            return OptimizationResult.Ok($"« {def.Name} » restauré.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec de restauration du tweak {Id}.", def.Id);
            return OptimizationResult.Fail($"Échec : {ex.Message}");
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
        catch (Exception ex) { _logger.LogWarning(ex, "Sauvegarde des tweaks impossible."); }
    }
}
