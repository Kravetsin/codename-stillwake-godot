using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>How planet types look (0095): every type, and every kind under a sky, has its look, its pictures and its growths' looks.</summary>
public class PlanetLooksTests
{
    [Fact]
    public void EveryKindUnderASkyHasItsLookAndItsBackdropsPictures()
    {
        var skies = Spec.Surfaces.All.Where(k => k.Sky != null).Select(k => k.Id).Order(StringComparer.Ordinal);
        Assert.Equal(skies, PlanetLooks.Of.Keys.Order(StringComparer.Ordinal));
        foreach (var look in PlanetLooks.Of.Values)
            Assert.All(PlanetLooks.BackdropPictures, p => Assert.True(Png.Has($"{look.Backdrop}/{p}"), p));
    }

    /// <summary>Every material drawn by band, stone and each vein through it, has a picture in every band a planet has: a vein without one throws each frame it shows.</summary>
    [Fact]
    public void EveryBandedMaterialHasAPictureInEveryBand()
    {
        int bands = Spec.PlanetTypes.All.Max(t => new Stillwake.Game.Features.Grid.Strata(t, PlanetLooks.Of[t.Id].StrataFrom).Bands);
        foreach (var material in Spec.Materials.All.Where(Stillwake.Game.Features.Grid.Strata.Banded))
            for (int band = 0; band < bands; band++) Assert.True(Png.Has($"wall_{material.Id}_b{band}"), $"{material.Id}: band {band}");
    }

    [Fact]
    public void EveryTypeHasStoneForEachBandAndEveryGrowthOfItALook()
    {
        foreach (var type in Spec.PlanetTypes.All)
        {
            var strata = new Stillwake.Game.Features.Grid.Strata(type, PlanetLooks.Of[type.Id].StrataFrom);
            for (int band = 0; band < strata.Bands; band++) Assert.True(Png.Has($"wall_{type.Stone}_b{band}"), $"{type.Id}: band {band}");
            Assert.InRange(type.Bands.Count + 1, 1, CaveLooks.Bands);
            Assert.All(type.Plants, k => Assert.True(FloraLooks.Of.ContainsKey(k.Id), $"{type.Id}: {k.Id}"));
            Assert.All(type.Caves, k => Assert.True(CaveLooks.Of.ContainsKey(k.Id), $"{type.Id}: {k.Id}"));
        }
    }
}
