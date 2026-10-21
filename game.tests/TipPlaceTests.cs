using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>An item's card moving with the pointer beside the slot under it, on whichever side it fits, never over the slot nor the pointer.</summary>
public class TipPlaceTests
{
    private static readonly (float W, float H) Screen = (1280, 720), Card = (234, 240);
    private const float Gap = 8, Drop = 8, Edge = 6, Side = 40;

    private static (float X, float Y) Near((float X, float Y) pointer, (float X, float Y, float W, float H) slot) =>
        TipPlace.Near(pointer, slot, Card, Screen, Gap, Drop, Edge);

    private static bool Covers((float X, float Y) at, (float X, float Y, float W, float H) slot) =>
        at.X < slot.X + slot.W && at.X + Card.W > slot.X && at.Y < slot.Y + slot.H && at.Y + Card.H > slot.Y;

    private static bool Inside((float X, float Y) at) =>
        at.Y >= Edge && at.Y + Card.H <= Screen.H - Edge && at.X >= 0 && at.X + Card.W <= Screen.W;

    [Fact]
    public void PlaceTheCardRightOfThePointerPastItsSlotAndBelowIt()
    {
        var slot = (100f, 60f, Side, Side);
        Assert.Equal((110f + Side + Gap, 70f + Drop), Near((110, 70), slot));
    }

    [Fact]
    public void MoveTheCardWithThePointerOneToOne()
    {
        var slot = (300f, 200f, Side, Side);
        var (x0, y0) = Near((302, 203), slot);
        var (x1, y1) = Near((337, 236), slot);
        Assert.Equal((35f, 33f), (x1 - x0, y1 - y0));
    }

    [Fact]
    public void FlipTheCardLeftOfThePointerAtTheScreensRightEdge()
    {
        // The equipment's belt batteries, at the right edge: a card once clamped back over its own slot.
        var slot = (1220f, 300f, Side, Side);
        var at = Near((1250, 310), slot);
        Assert.Equal(1250f - Side - Gap - Card.W, at.X);
        Assert.True(at.X + Card.W <= slot.Item1 - Gap);
    }

    [Fact]
    public void FlipTheCardAboveThePointerAtTheScreensFoot()
    {
        // The hotbar along the screen's foot.
        var slot = (600f, 660f, Side, Side);
        var at = Near((620, 680), slot);
        Assert.Equal(680f - Drop - Card.H, at.Y);
    }

    [Fact]
    public void KeepTheCardInsideTheScreenWhereNeitherAboveNorBelowFits()
    {
        var tall = (W: 234f, H: 700f);
        var at = TipPlace.Near((620, 300), (600f, 290f, Side, Side), tall, Screen, Gap, Drop, Edge);
        Assert.Equal(Edge, at.Y);
        Assert.True(at.Y + tall.H <= Screen.H - Edge);
    }

    [Fact]
    public void NeverCoverTheSlotNorThePointerAnywhereOnTheScreen()
    {
        for (float x = 0; x + Side <= Screen.W; x += 20)
            for (float y = 0; y + Side <= Screen.H; y += 20)
            {
                var slot = (x, y, Side, Side);
                foreach (var (dx, dy) in new[] { (0f, 0f), (Side, Side), (Side, 0f), (0f, Side), (Side / 2, Side / 2) })
                {
                    var pointer = (x + dx, y + dy);
                    var at = Near(pointer, slot);
                    Assert.False(Covers(at, slot), $"slot {x},{y} pointer +{dx},{dy}");
                    Assert.False(Covers(at, (pointer.Item1, pointer.Item2, 1, 1)), $"pointer {pointer}");
                    Assert.True(Inside(at), $"slot {x},{y} pointer +{dx},{dy}: {at}");
                }
            }
    }
}
