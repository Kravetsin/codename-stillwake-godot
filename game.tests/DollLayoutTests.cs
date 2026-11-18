using System.Linq;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The equipment's doll (gear-tabs): the slots round the hero, at every slot size.</summary>
public class DollLayoutTests
{
    public static TheoryData<int> Sizes => [Slots.SizeOf(SlotScale.Normal), Slots.SizeOf(SlotScale.Large), 52];

    private static (int X, int Y, int W, int H) Box(DollLayout layout, DollSpot spot)
    {
        var (x, y) = layout.SlotAt(spot);
        return (x, y, layout.Slot, layout.Slot);
    }

    // Apart by at least a slot gap.
    private static bool Overlap((int X, int Y, int W, int H) a, (int X, int Y, int W, int H) b) =>
        a.X < b.X + b.W + Slots.Gap && b.X < a.X + a.W + Slots.Gap && a.Y < b.Y + b.H + Slots.Gap && b.Y < a.Y + a.H + Slots.Gap;

    private static bool Inside(DollLayout layout, (int X, int Y, int W, int H) box) =>
        box.X >= 0 && box.Y >= 0 && box.X + box.W <= layout.Width && box.Y + box.H <= layout.Height;

    [Theory]
    [MemberData(nameof(Sizes))]
    public void EveryTabPlacesEachSpotOnceInsideTheScreenNoTwoTouching(int slot)
    {
        var layout = DollLayout.Of(slot);
        foreach (var tab in new[] { DollLayout.Gear, DollLayout.Looks })
        {
            Assert.Equal(tab.Count, tab.Distinct().Count());
            foreach (var spot in tab) Assert.True(Inside(layout, Box(layout, spot)), $"{spot} out of the screen at {slot}");
            for (int i = 0; i < tab.Count; i++)
                for (int j = i + 1; j < tab.Count; j++)
                    Assert.False(Overlap(Box(layout, tab[i]), Box(layout, tab[j])), $"{tab[i]} and {tab[j]} touch at {slot}");
        }
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void EveryLineReachesTheBodyInTheOpenNeverUnderASlot(int slot)
    {
        var layout = DollLayout.Of(slot);
        foreach (var tab in new[] { DollLayout.Gear, DollLayout.Looks })
            foreach (var spot in tab)
            {
                if (layout.LeadTo(spot) is not var (x, y))
                {
                    Assert.Contains(spot, new[] { DollSpot.Belt2, DollSpot.Belt3 });
                    continue;
                }
                Assert.InRange(x, 0, layout.Width);
                Assert.InRange(y, layout.Feet.Y - layout.Tall, layout.Feet.Y);
                foreach (var other in tab)
                {
                    var b = Box(layout, other);
                    Assert.False(x >= b.X && x <= b.X + b.W && y >= b.Y && y <= b.Y + b.H, $"{spot}'s dot under {other}");
                }
            }
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void LeavesRoomRightOfTheSuitForTheClothesUnderIt(int slot)
    {
        var layout = DollLayout.Of(slot);
        var (x, y) = layout.SlotAt(DollSpot.Suit);
        var clothes = (x + slot + Slots.Gap, y, slot, slot);
        Assert.True(Inside(layout, clothes));
        Assert.All(DollLayout.Gear.Where(s => s != DollSpot.Suit), s => Assert.False(Overlap(clothes, Box(layout, s)), $"{s}"));
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void TheHeroStandsInTheScreenBetweenTheColumns(int slot)
    {
        var layout = DollLayout.Of(slot);
        Assert.InRange(layout.Feet.Y, layout.Tall, layout.Height);
        Assert.InRange(layout.Feet.X, layout.SlotAt(DollSpot.Head).X + slot, layout.SlotAt(DollSpot.Suit).X);
    }

    [Fact]
    public void ScalesWithTheSlot()
    {
        var (normal, large) = (DollLayout.Of(44), DollLayout.Of(60));
        Assert.True(large.Width > normal.Width && large.Height > normal.Height && large.Tall > normal.Tall);
        Assert.Equal((12, 24), normal.SlotAt(DollSpot.Head));
    }
}
