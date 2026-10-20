using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>An item's card pinned beside the slot it tells of, on whichever side it fits, never over the slot.</summary>
public class TipPlaceTests
{
    private static readonly (float W, float H) Screen = (1280, 720), Card = (234, 240);
    private const float Gap = 8, Edge = 6;

    private static bool Covers((float X, float Y) at, (float X, float Y, float W, float H) slot) =>
        at.X < slot.X + slot.W && at.X + Card.W > slot.X && at.Y < slot.Y + slot.H && at.Y + Card.H > slot.Y;

    [Fact]
    public void PinTheCardRightOfItsSlotItsTopLevelWithTheSlots()
    {
        var slot = (100f, 60f, 40f, 40f);
        Assert.Equal((148f, 60f), TipPlace.Beside(slot, Card, Screen, Gap, Edge));
    }

    [Fact]
    public void FlipTheCardLeftOfASlotAtTheScreensRightEdgeSoItNeverCoversIt()
    {
        // The equipment's belt batteries, at the right edge: the card once clamped back over its own slot.
        var slot = (1220f, 300f, 40f, 40f);
        var at = TipPlace.Beside(slot, Card, Screen, Gap, Edge);
        Assert.Equal(1220f - Gap - Card.W, at.X);
        Assert.False(Covers(at, slot));
    }

    [Fact]
    public void KeepTheCardInsideTheScreenBelowASlotNearItsFoot()
    {
        var slot = (600f, 660f, 40f, 40f);
        var at = TipPlace.Beside(slot, Card, Screen, Gap, Edge);
        Assert.Equal(Screen.H - Edge - Card.H, at.Y);
        Assert.False(Covers(at, slot));
    }

    [Fact]
    public void NeverCoverItsSlotAnywhereOnTheScreen()
    {
        for (float x = 0; x + 40 <= Screen.W; x += 20)
            for (float y = 0; y + 40 <= Screen.H; y += 20)
            {
                var slot = (x, y, 40f, 40f);
                Assert.False(Covers(TipPlace.Beside(slot, Card, Screen, Gap, Edge), slot), $"{x},{y}");
            }
    }
}
