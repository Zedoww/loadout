using Loadout.Core.Optimization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Loadout.Core.Tests;

public class TweakServiceTests
{
    private static TweakService CreateSut() => new(NullLogger<TweakService>.Instance);

    [Fact]
    public void Le_catalogue_n_est_pas_vide()
    {
        Assert.NotEmpty(CreateSut().Definitions);
    }

    [Fact]
    public void Les_identifiants_sont_uniques()
    {
        var ids = CreateSut().Definitions.Select(d => d.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Chaque_tweak_a_un_effet_reel()
    {
        // Valeur activée différente du défaut, sinon le tweak ne sert à rien.
        Assert.All(CreateSut().Definitions,
            d => Assert.NotEqual(d.EnabledValue, d.DefaultValue));
    }

    [Fact]
    public void Chaque_tweak_est_documente()
    {
        Assert.All(CreateSut().Definitions, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Name));
            Assert.False(string.IsNullOrWhiteSpace(d.Description));
            Assert.False(string.IsNullOrWhiteSpace(d.SubKey));
            Assert.False(string.IsNullOrWhiteSpace(d.ValueName));
        });
    }
}
