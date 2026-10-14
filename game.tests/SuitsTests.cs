using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>features/crew/suits.test.ts, ported, and the shades the hero's pictures are recoloured by.</summary>
public class SuitsTests
{
    [Fact]
    public void HasOneColourForEachTheSimHandsOutAllDifferent()
    {
        Assert.Equal(Content.Crew.Colours, Suits.All.Length);
        Assert.Equal(Suits.All.Length, Suits.All.Distinct().Count());
    }

    [Fact]
    public void ShadesASuitAsTheTypeScriptPicturesWereDrawn()
    {
        // The drawn suit's light, own, shade and outline, as the exported art/hero_*.png carry them.
        Assert.Equal([0xebc178, 0xe0a030, 0xa17323, 0x5e4314], Suits.ShadesOf(Suits.Drawn));
        // No two suits share a shade, the drawn one included, so it recolours into any other.
        var shades = Suits.All.Append(Suits.Drawn).SelectMany(Suits.ShadesOf).ToList();
        Assert.Equal(shades.Count, shades.Distinct().Count());
    }
}
