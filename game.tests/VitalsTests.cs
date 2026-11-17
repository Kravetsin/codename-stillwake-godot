using System;
using System.Linq;
using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// The vitals block at the screen's bottom-left (hud-vitals, the pick «dials» A1 at 40): six dials over a long air bar, its place,
/// the air fitted short of the edge's keys, the charge dial's arcs.
/// </summary>
public class VitalsTests
{
    private const float Screen = 720, Wide = 1280;

    /// <summary>The edge's keys in the game: overlay, build, research, market, suit, handbook (<c>Main</c>'s edge.Put).</summary>
    private const int Keys = 6;

    public static TheoryData<SlotScale> Scales => new() { SlotScale.Normal, SlotScale.Large };

    private static float KeysLeft(SlotScale scale) => EdgeRow.RimLeft(Wide, Keys, EdgeRow.Grown(Slots.SizeOf(scale)));

    [Fact]
    public void SixEqualCirclesStandAGapApartWithTheOxygenCircleUnderTheFirst()
    {
        Assert.Equal(6, Vitals.Dials);
        for (int i = 1; i < Vitals.Dials; i++)
            Assert.Equal(Vitals.CircleGap, Vitals.Column(i) - Vitals.Dial / 2 - (Vitals.Column(i - 1) + Vitals.Dial / 2), 4);
        Assert.Equal((40f, 17.5f, 26f, 16f), (Vitals.Dial, Vitals.RingRadius, Vitals.Icon, Vitals.CircleGap));
        // The oxygen circle in the charge's column, a gap under it; the block's foot its bottom.
        Assert.Equal(Vitals.SignX, Vitals.Column(Vitals.ChargeDial));
        Assert.Equal(Vitals.CircleGap, Vitals.AirY - Vitals.Dial / 2 - (Vitals.RowY + Vitals.Dial / 2), 4);
        Assert.Equal(Vitals.AirY + Vitals.Dial / 2, Vitals.Foot);
        Assert.Equal(320, Vitals.RowRight);
        // The numbers in the gap between the rows; the air's pieces start right of the oxygen circle.
        Assert.Equal((Vitals.RowY + Vitals.Dial / 2 + Vitals.AirY - Vitals.Dial / 2) / 2, Vitals.NumberY, 4);
        Assert.True(Vitals.RowLeft > Vitals.Dial);
    }

    [Fact]
    public void TheDialsAreChargeTemperatureThreeMealsAndThePurge() =>
        Assert.Equal((0, 1, 2, 5), (Vitals.ChargeDial, Vitals.TemperatureDial, Vitals.FirstMealDial, Vitals.PurgeDial));

    [Theory]
    [InlineData(SlotScale.Normal, 513.2f)]
    [InlineData(SlotScale.Large, 483.5f)]
    public void TheAirEndsAGapShortOfTheEdgesKeysAtEverySize(SlotScale scale, float keys)
    {
        // The page's measures at 1280×720: the first of the six keys over the rim.
        Assert.Equal(keys, KeysLeft(scale), 1);
        int slot = Slots.SizeOf(scale);
        float end = Vitals.Origin(Screen, slot).X + Vitals.AirEnd(KeysLeft(scale), slot) * Vitals.ScaleOf(slot);
        Assert.Equal(KeysLeft(scale) - Vitals.KeysGap, end, 3);
    }

    [Fact]
    public void AtTheNormalSizeThePagesAirReachesWhereItEndedThere()
    {
        int slot = Slots.SizeOf(SlotScale.Normal);
        float end = Vitals.AirEnd(KeysLeft(SlotScale.Normal), slot);
        Assert.Equal(Vitals.AirReach, end, 0);
        // Its 60 units in three parts, unshrunk, end at the reach.
        Assert.Equal(Vitals.AirReach, Vitals.RowLeft + Vitals.AirLength(60, 3, Vitals.AirReach) + 2 * Vitals.Gap, 3);
    }

    [Fact]
    public void TheAirKeepsItsScaleUntilItWouldReachTheEndThenShrinksAlikeKeepingItsGaps()
    {
        // Short of the end: a unit's length each.
        Assert.Equal(45 * Vitals.AirPerUnit, Vitals.AirLength(45, 2, 400), 3);
        // Past it: shrunk to end there, the gaps kept.
        Assert.Equal(400 - Vitals.RowLeft - 2 * Vitals.Gap, Vitals.AirLength(100, 3, 400), 3);
        Assert.Equal(400 - Vitals.RowLeft, Vitals.AirLength(100, 1, 400), 3);
        Assert.Equal(0, Vitals.AirLength(0, 1, 400));
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void TheHeaviestSuitsAirEndsShortOfTheKeys(SlotScale scale)
    {
        int slot = Slots.SizeOf(scale);
        float s = Vitals.ScaleOf(slot), end = Vitals.AirEnd(KeysLeft(scale), slot);
        int heavy = Spec.Crew.Lungs + Spec.Items.All.Max(i => i.Tank);
        float right = Vitals.Origin(Screen, slot).X + (Vitals.RowLeft + Vitals.AirLength(heavy, 3, end) + 2 * Vitals.Gap) * s;
        Assert.True(right <= KeysLeft(scale) - Vitals.KeysGap + 1e-3f, $"{scale}: the air ends at {right}");
        // The row of dials, its numbers and all, stands above the keys' top, however far right it reaches.
        var (artLeft, artWide) = TabletPeek.RimArt(Wide);
        float artTop = TabletPeek.ScreenTop(Screen) - TabletLayout.ScreenTop * TabletLayout.ArtScale;
        float keysTop = EdgeRow.Top(TabletPeek.RowAt(artLeft, artTop, artWide).Top, TabletPeek.KeySide, EdgeRow.Grown(slot));
        float numbers = Vitals.Origin(Screen, slot).Y + (Vitals.NumberY + Vitals.NumberSize / 2) * s;
        Assert.True(numbers < keysTop, $"{scale}: the numbers' foot {numbers} over the keys' top {keysTop}");
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void TheBlockStandsUnderTheOpenBagAndOverTheTabletsRim(SlotScale scale)
    {
        int slot = Slots.SizeOf(scale);
        // The bag at its largest: every slot past the hotbar, a row of the hotbar's length each, in its display's frame.
        int rows = (Spec.Inventory.Slots - Spec.Inventory.Hotbar + Spec.Inventory.Hotbar - 1) / Spec.Inventory.Hotbar;
        float bag = Slots.InventoryTop(slot) + rows * (slot + Slots.Gap) + 2 * Slots.ScreenPadding + WristPiece.Scale * WristPieces.Display.Window.Bottom;
        var (x, top, w, height) = Vitals.Covers(Screen, slot, KeysLeft(scale));
        Assert.True(top > bag, $"{scale}: the block's top {top} under the bag's foot {bag}");
        Assert.Equal(Screen - Vitals.Below, top + height, 3);
        // As wide as the air may reach, the gap short of the keys, or the row of dials if that reaches further.
        Assert.Equal(Math.Max(x + Vitals.RowRight * Vitals.ScaleOf(slot), KeysLeft(scale) - Vitals.KeysGap), x + w, 3);
    }

    [Fact]
    public void TheStoreAToolDrainsFirstIsTheLastArc()
    {
        Assert.Equal([2, 1, 0], Vitals.ChargeOrder(3));
        Assert.Empty(Vitals.ChargeOrder(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void TheChargeRingIsSharedAlikeAGapBetweenEachArcAndAtTheTop(int stores)
    {
        var arcs = Vitals.ChargeArcs(stores);
        Assert.Equal(stores, arcs.Count);
        float gap = Vitals.ArcGap / Vitals.RingRadius, top = -MathF.PI / 2;
        Assert.Equal(top + gap / 2, arcs[0].From, 4);
        Assert.Equal(top + 2 * MathF.PI - gap / 2, arcs[^1].To, 4);
        for (int i = 1; i < stores; i++) Assert.Equal(gap, arcs[i].From - arcs[i - 1].To, 4);
        Assert.All(arcs, a => Assert.Equal(arcs[0].To - arcs[0].From, a.To - a.From, 4));
        Assert.Empty(Vitals.ChargeArcs(0));
    }

    [Fact]
    public void TheBlockGrowsWithTheSlots()
    {
        Assert.Equal(1.2f, Vitals.ScaleOf(Slots.SizeOf(SlotScale.Normal)), 4);
        Assert.Equal(1.5f, Vitals.ScaleOf(Slots.SizeOf(SlotScale.Large)), 4);
    }
}
