using Loadout.Core.Optimization;

namespace Loadout.Core.Tests;

public class TempCleanerTests : IDisposable
{
    private readonly string _root;

    public TempCleanerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "loadout-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    private long SeedFiles()
    {
        File.WriteAllText(Path.Combine(_root, "a.tmp"), new string('x', 1000));
        File.WriteAllText(Path.Combine(_root, "b.log"), new string('y', 500));
        string sub = Path.Combine(_root, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "c.dat"), new string('z', 250));
        return 1750;
    }

    [Fact]
    public void Scan_calcule_la_taille_totale()
    {
        long expected = SeedFiles();
        var cleaner = new TempCleaner(new[] { _root });

        Assert.Equal(expected, cleaner.Scan());
    }

    [Fact]
    public void Scan_zero_quand_vide()
    {
        var cleaner = new TempCleaner(new[] { _root });
        Assert.Equal(0, cleaner.Scan());
    }

    [Fact]
    public void Clean_supprime_les_fichiers_et_renvoie_l_espace_libere()
    {
        long expected = SeedFiles();
        var cleaner = new TempCleaner(new[] { _root });

        var result = cleaner.Clean();

        Assert.True(result.Success);
        Assert.Equal(expected, result.BytesFreed);
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Clean_ignore_un_dossier_inexistant()
    {
        var cleaner = new TempCleaner(new[] { Path.Combine(_root, "nope") });
        var result = cleaner.Clean();

        Assert.True(result.Success);
        Assert.Equal(0, result.BytesFreed);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
}
