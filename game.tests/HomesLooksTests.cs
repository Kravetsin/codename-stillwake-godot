using Xunit;
using Anchor = Stillwake.Content.Flora.Anchor;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>How the arc makers' homes look (0091): every piece has its pictures, and stands, hangs and leans where the page puts it.</summary>
public class HomesLooksTests
{
    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art");

    [Fact]
    public void EveryPieceHasItsPicturesEachVariantItsDarkLookWhereItsLampMayGoOutAndTheirGlows()
    {
        Assert.Equal(Spec.Ruins.Pieces.Select(p => p.Id).Order(StringComparer.Ordinal), HomesArt.Of.Keys.Order(StringComparer.Ordinal));
        foreach (var (kind, art) in HomesArt.Of)
            for (int v = 0; v < art.Variants; v++)
            {
                Assert.True(File.Exists(Path.Combine(Art, "homes", $"{kind}_{v}.png")), $"{kind}_{v}");
                Assert.Equal(art.Dark, File.Exists(Path.Combine(Art, "homes", $"{kind}_{v}_dark.png")));
            }
        // What gives light glows: a lamp's picture has its glow beside it.
        Assert.All(HomesArt.Of.Where(a => a.Value.Lights.Length > 0), a => Assert.True(File.Exists(Path.Combine(Art, "homes", $"{a.Key}_0_glow.png")), a.Key));
        // The homes' plaster and the new items' icons.
        Assert.All((string[])["wall", "back", "top", "detail"], p => Assert.True(File.Exists(Path.Combine(Art, "planet", $"{p}_plaster.png")), p));
        Assert.All((string[])["cloth", "arcLamp"], i => Assert.True(File.Exists(Path.Combine(Art, "items", $"{i}.png")), i));
    }

    [Fact]
    public void APieceStandsOnTheFloorHangsFromTheCeilingAndSitsInTheMiddleOfItsBackWallsCells()
    {
        // The table, 32 by 18, its two columns from (7, 9): its bottom on the floor's top, its middle the columns'.
        Assert.Equal((7 * 16, 10 * 16 - 18), HomesLooks.BoxAt("table", Anchor.Floor, 7, 9, 2, 1));
        // The dishes on it, a pixel into its top.
        Assert.Equal(10 * 16 - 18 + 1 - 9, HomesLooks.BoxAt("dishes", Anchor.Floor, 7, 8, 1, 1, standsOn: 10 * 16 - 18 + HomesLooks.SinksInto("table")).Y);
        // The ceiling lamp from the underside of the ceiling over (10, 5); a frame in the middle of its two cells' height.
        Assert.Equal(5 * 16, HomesLooks.BoxAt("ceilLamp", Anchor.Ceiling, 10, 5, 1, 1).Y);
        Assert.Equal(6 * 16 + (16 - 18) / 2, HomesLooks.BoxAt("frame", Anchor.Back, 13, 6, 2, 1).Y);
        // The fallen beam lies on the floor under its walls, its high end over the room, as the page draws it.
        var beam = HomesLooks.BoxAt("fallenBeam", Anchor.Rock, 22, 9, 2, 1);
        Assert.Equal((340, 159 - 30), beam);
    }

    [Fact]
    public void WhatNeverGoesOutAlwaysBurnsAndTwoLampsInThreeThatMayAreLit()
    {
        Assert.True(HomesLooks.Lit("hearth", 12345));
        var lamps = Enumerable.Range(0, 3000).Count(cell => HomesLooks.Lit("ceilLamp", cell));
        Assert.InRange(lamps, 1800, 2200);
        Assert.Equal("homes/ceilLamp_0_dark", HomesLooks.PictureOf("ceilLamp", 7, lit: false));
        Assert.Equal("homes/hearth_0", HomesLooks.PictureOf("hearth", 7, lit: false));
    }

    [Fact]
    public void ClothSwaysFromItsTopWholePixelsAtMostItsMostAtTheBottom()
    {
        Assert.Equal(0, HomesLooks.SwayAt(0, 30, 1.0, 0));
        var bottoms = Enumerable.Range(0, 100).Select(i => HomesLooks.SwayAt(30, 30, i * 0.1, 0)).ToList();
        Assert.Contains(1, bottoms);
        Assert.Contains(-1, bottoms);
        Assert.All(bottoms, b => Assert.InRange(b, -(int)Math.Ceiling(HomesLooks.SwayMost), (int)Math.Ceiling(HomesLooks.SwayMost)));
    }
}
