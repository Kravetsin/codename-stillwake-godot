using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>How planet types look (0095): every type, and every kind under a sky, has its look, its pictures and its growths' looks.</summary>
public class PlanetLooksTests
{
    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art");

    [Fact]
    public void EveryKindUnderASkyHasItsLookAndItsBackdropsPictures()
    {
        var skies = Spec.Surfaces.All.Where(k => k.Sky != null).Select(k => k.Id).Order(StringComparer.Ordinal);
        Assert.Equal(skies, PlanetLooks.Of.Keys.Order(StringComparer.Ordinal));
        foreach (var look in PlanetLooks.Of.Values)
            Assert.All(PlanetLooks.BackdropPictures, p => Assert.True(File.Exists(Path.Combine(Art, look.Backdrop, $"{p}.png")), p));
    }

    [Fact]
    public void EveryTypeHasATintForEachBandAndEveryGrowthOfItALook()
    {
        foreach (var type in Spec.PlanetTypes.All)
        {
            Assert.Equal(type.Bands.Count + 1, PlanetLooks.Of[type.Id].Tint.Count);
            Assert.InRange(type.Bands.Count + 1, 1, CaveLooks.Bands);
            Assert.All(type.Plants, k => Assert.True(FloraLooks.Of.ContainsKey(k.Id), $"{type.Id}: {k.Id}"));
            Assert.All(type.Caves, k => Assert.True(CaveLooks.Of.ContainsKey(k.Id), $"{type.Id}: {k.Id}"));
        }
    }
}
