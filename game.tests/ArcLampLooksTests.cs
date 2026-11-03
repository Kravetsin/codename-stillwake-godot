using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How the arc makers' lamp hung aboard looks (0091): the lantern of its icon in every state, its glass alight only while it shines.</summary>
public class ArcLampLooksTests
{
    [Fact]
    public void ItsBodyIsDrawnWholeOffOnAndFlickeringItsGlassAlightAndGlowingAsBrightAsItShines()
    {
        // Off, unpowered, or dark in a flicker: the body alone, never nothing.
        Assert.Equal(new ArcLampLooks.Look(1, 0, 0), ArcLampLooks.Drawn(0));
        // On: the body still, the glass alight over it and its glow.
        Assert.Equal(new ArcLampLooks.Look(1, 1, 1), ArcLampLooks.Drawn(1));
        // Short of power, as bright as it shines; past whole its strength's light, the pictures no more than whole.
        Assert.Equal(new ArcLampLooks.Look(1, 0.4f, 0.4f), ArcLampLooks.Drawn(0.4f));
        Assert.Equal(new ArcLampLooks.Look(1, 1, 1), ArcLampLooks.Drawn(1.3f));
        Assert.All((string[])[ArcLampLooks.Body, ArcLampLooks.Alight, ArcLampLooks.Glow], p => Assert.True(Png.Has(p), p));
        Assert.Equal($"machine_{ArcLampLooks.Kind}", ArcLampLooks.Body);
    }

    [Fact]
    public void WhatLightsIsTheLanternRoundItsGlass()
    {
        var alight = Pixels(ArcLampLooks.Alight);
        var lit = Enumerable.Range(0, 256).Where(i => alight[i] >> 24 != 0).ToArray();
        Assert.InRange((lit.Min(i => i % 16) + lit.Max(i => i % 16) + 1) / 2f - ArcLampLooks.Glass.X, -0.5f, 0.5f);
        Assert.InRange((lit.Min(i => i / 16) + lit.Max(i => i / 16) + 1) / 2f - ArcLampLooks.Glass.Y, -0.5f, 0.5f);
    }

    [Fact]
    public void ItsGlowIsTheCellWithAHalfCellsMarginItsLightRoundTheGlass()
    {
        // The glow's picture is the cell's with a half cell's margin round it, both laid on the cell's middle.
        var (w, h, glow) = Png.Logical(ArcLampLooks.Glow, soft: true);
        Assert.Equal((32, 32), (w, h));
        float sum = 0, x = 0, y = 0;
        for (int i = 0; i < glow.Length; i++) (sum, x, y) = (sum + (glow[i] >> 24), x + (glow[i] >> 24) * (i % 32 + 0.5f), y + (glow[i] >> 24) * (i / 32 + 0.5f));
        Assert.InRange(x / sum - 8 - ArcLampLooks.Glass.X, -1f, 1f);
        Assert.InRange(y / sum - 8 - ArcLampLooks.Glass.Y, -1f, 1f);
    }

    [Fact]
    public void ItCracklesOnlyAtFullPowerTheHomesLanternsArcAcrossItsGlass()
    {
        Assert.True(ArcLampLooks.Crackles(1));
        Assert.False(ArcLampLooks.Crackles(0.6f));
        Assert.False(ArcLampLooks.Crackles(0));
        // The homes' lantern's arc, its length and tilt, struck from glass to glass of this one's, never on its cage.
        var arc = ArcLampLooks.Arc;
        var lantern = HomesArt.Of["lantern"].Emitters.First(e => e.Kind == Emits.Arc);
        Assert.Equal((lantern.Rate, lantern.Life, lantern.Width), (arc.Rate, arc.Life, arc.Width));
        Assert.Equal(lantern.X2 - lantern.X, arc.X2 - arc.X, 3);
        Assert.Equal(lantern.Y2 - lantern.Y, arc.Y2 - arc.Y, 3);
        var alight = Pixels(ArcLampLooks.Alight);
        bool Lit(float x, float y) => alight[(int)y * 16 + (int)x] >> 24 != 0;
        Assert.True(Lit(arc.X, arc.Y) && Lit(arc.X2, arc.Y2) && Lit((arc.X + arc.X2) / 2, (arc.Y + arc.Y2) / 2));
    }

    /// <summary>A square picture <paramref name="size"/> wide at the world's pixels: each pixel's ARGB.</summary>
    private static uint[] Pixels(string name, int size = 16)
    {
        var (w, h, argb) = Png.Logical(name);
        Assert.Equal((size, size), (w, h));
        return argb;
    }
}
