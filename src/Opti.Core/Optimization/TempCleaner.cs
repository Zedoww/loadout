namespace Opti.Core.Optimization;

/// <summary>
/// Nettoie les dossiers temporaires. Ne touche qu'aux emplacements temporaires
/// connus et ignore tout fichier verrouillé (utilisé par un programme actif),
/// ce qui rend l'opération sûre.
/// </summary>
public sealed class TempCleaner
{
    private static IEnumerable<string> TempDirectories()
    {
        yield return Path.GetTempPath();                                    // %TEMP% utilisateur
        yield return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Temp");
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(local))
            yield return Path.Combine(local, "Temp");
    }

    /// <summary>Calcule l'espace récupérable (octets) sans rien supprimer.</summary>
    public long Scan()
    {
        long total = 0;
        foreach (var dir in TempDirectories().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in EnumerateFilesSafe(dir))
            {
                try { total += new FileInfo(file).Length; }
                catch { /* inaccessible */ }
            }
        }
        return total;
    }

    /// <summary>Supprime les fichiers temporaires accessibles. Renvoie l'espace libéré.</summary>
    public OptimizationResult Clean()
    {
        long freed = 0;
        int files = 0, skipped = 0;

        foreach (var dir in TempDirectories().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var file in EnumerateFilesSafe(dir))
            {
                try
                {
                    long size = new FileInfo(file).Length;
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    freed += size;
                    files++;
                }
                catch
                {
                    skipped++; // verrouillé ou protégé : on laisse tel quel.
                }
            }

            // Supprime les sous-dossiers désormais vides.
            foreach (var sub in SafeGetDirectories(dir))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(sub).Any())
                        Directory.Delete(sub);
                }
                catch { /* ignore */ }
            }
        }

        return OptimizationResult.Ok(
            $"{files} fichiers supprimés ({skipped} ignorés car en cours d'utilisation).", freed);
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string current = pending.Pop();
            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(current); } catch { }
            foreach (var f in files) yield return f;

            foreach (var sub in SafeGetDirectories(current)) pending.Push(sub);
        }
    }

    private static string[] SafeGetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch { return Array.Empty<string>(); }
    }
}
