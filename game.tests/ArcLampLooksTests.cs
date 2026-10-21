using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How the arc makers' lamp hung aboard looks (0090): its picture in every state, its globe alight only while it shines.</summary>
public class ArcLampLooksTests
{
    private static string Art => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art");

    [Fact]
    public void ItsBodyIsDrawnWholeOffOnAndFlickeringItsGlobeAlightAndGlowingAsBrightAsItShines()
    {
        // Off, unpowered, or dark in a flicker: the body alone, never nothing.
        Assert.Equal(new ArcLampLooks.Look(1, 0, 0), ArcLampLooks.Drawn(0));
        // On: the body still, the globe alight over it and its glow.
        Assert.Equal(new ArcLampLooks.Look(1, 1, 1), ArcLampLooks.Drawn(1));
        // Short of power, as bright as it shines; past whole its strength's light, the pictures no more than whole.
        Assert.Equal(new ArcLampLooks.Look(1, 0.4f, 0.4f), ArcLampLooks.Drawn(0.4f));
        Assert.Equal(new ArcLampLooks.Look(1, 1, 1), ArcLampLooks.Drawn(1.3f));
        Assert.All((string[])[ArcLampLooks.Body, ArcLampLooks.Alight, ArcLampLooks.Glow], p => Assert.True(File.Exists(Path.Combine(Art, $"{p}.png")), p));
        Assert.Equal($"machine_{ArcLampLooks.Kind}", ArcLampLooks.Body);
    }

    [Fact]
    public void ItCracklesOnlyAtFullPowerTheCeilingLampsArcAcrossItsOwnGlobe()
    {
        Assert.True(ArcLampLooks.Crackles(1));
        Assert.False(ArcLampLooks.Crackles(0.6f));
        Assert.False(ArcLampLooks.Crackles(0));
        // The ceiling lamp's arc, its globe's width, straight across the middle of this one's globe and inside its cell.
        var arc = ArcLampLooks.Arc;
        var ceiling = HomesArt.Of["ceilLamp"].Emitters.First(e => e.Kind == Emits.Arc);
        Assert.Equal((ceiling.Rate, ceiling.Life, ceiling.X2 - ceiling.X), (arc.Rate, arc.Life, arc.X2 - arc.X));
        Assert.Equal(ArcLampLooks.Globe.X, (arc.X + arc.X2) / 2, 3);
        Assert.Equal(ArcLampLooks.Globe.Y, arc.Y, 3);
        Assert.InRange(arc.X, 0, 16);
        Assert.InRange(arc.X2, 0, 16);
    }
}
