using Stillwake.Game.Features.Atmosphere;
using Stillwake.Game.Features.Pipes;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The mouse buttons as the world reads them: the button tests of apps/game/src/render/camera.test.ts, one for one.</summary>
public class ButtonsTests
{
    private const int Cell = 16;

    /// <summary>A pointer event at (x, y) on the canvas, in pixels, with <paramref name="held"/> held afterwards.</summary>
    private static void Pointer(Buttons buttons, string type, int held, double x, double y) =>
        buttons.OnPointer(type == "down", held, (x / Cell, y / Cell));

    [Fact]
    public void ReadsLeftPressesAndReleasesInOrderAtTheirWorldPoints()
    {
        var buttons = new Buttons();
        Pointer(buttons, "down", Buttons.Left, Cell, Cell);
        Pointer(buttons, "up", 0, 2 * Cell, Cell);
        Assert.Equal([new ButtonEvent(true, (1, 1)), new ButtonEvent(false, (2, 1))], buttons.TakeLeft());
        Assert.Empty(buttons.TakeLeft());
    }

    [Fact]
    public void ReadsRightPressesAndReleasesWhileTheLeftIsHeldAndWhetherTheRightButtonIsHeld()
    {
        var buttons = new Buttons();
        Pointer(buttons, "down", Buttons.Left, 0, 0);
        Pointer(buttons, "move", Buttons.Left | Buttons.Right, Cell, 0);
        Assert.True(buttons.HoldsRight);
        Pointer(buttons, "move", Buttons.Left, 2 * Cell, 0);
        Assert.Equal([new ButtonEvent(true, (1, 0)), new ButtonEvent(false, (2, 0))], buttons.TakeRight());
        Pointer(buttons, "up", 0, 2 * Cell, 0);
        Assert.False(buttons.HoldsRight);
        Assert.Empty(buttons.TakeRight());
        Assert.Equal(2, buttons.TakeLeft().Count);
    }

    [Fact]
    public void ReportsALeftReleaseThatComesAsAMoveWhileTheRightButtonIsHeld()
    {
        var buttons = new Buttons();
        Pointer(buttons, "down", Buttons.Left, 0, 0);
        Pointer(buttons, "move", Buttons.Left | Buttons.Right, 0, 0);
        Pointer(buttons, "move", Buttons.Right, 0, 0);
        Pointer(buttons, "up", 0, 0, 0);
        Assert.Equal([true, false], buttons.TakeLeft().Select(b => b.Down));
    }

    [Fact]
    public void LeavesADragBegunOnAPanelAloneAsItCrossesTheWorldUnread()
    {
        var buttons = new Buttons();
        // Pressed on a crate's slot, carried over the world, let go on the hotbar beyond it.
        Pointer(buttons, "move", Buttons.Left, 10, 10);
        Pointer(buttons, "move", Buttons.Left, 30, 10);
        Assert.False(buttons.HoldsLeft);
        Pointer(buttons, "up", 0, 40, 10);
        Assert.Empty(buttons.TakeLeft());
        // A press on the world after it is the world's again.
        Pointer(buttons, "down", Buttons.Left, 0, 0);
        Assert.True(buttons.HoldsLeft);
        Assert.Equal([true], buttons.TakeLeft().Select(b => b.Down));
    }

    // The left press the hero's hands read (crew/keys.test.ts).

    [Fact]
    public void CountsALeftPressOnTheWorldNotOnAPanelOnce()
    {
        var buttons = new Buttons();
        Pointer(buttons, "move", Buttons.Left, 0, 0);
        Pointer(buttons, "up", 0, 0, 0);
        Assert.False(buttons.TakeClick());
        Pointer(buttons, "down", Buttons.Right, 0, 0);
        Pointer(buttons, "up", 0, 0, 0);
        Assert.False(buttons.TakeClick());
        Pointer(buttons, "down", Buttons.Left, 0, 0);
        // Taken apart from the card's presses: one read leaves the other.
        Assert.Single(buttons.TakeLeft());
        Assert.True(buttons.TakeClick());
        Assert.False(buttons.TakeClick());
    }
}

/// <summary>Oxygen as the pipes overlay shows it flowing (0109): along spans, away from where it comes in.</summary>
public class FlowTests
{
    // Five wide: a span along the middle row to a joint at its third cell, on to its end, and a branch down from the joint.
    private const int W = 5;
    private static int At(int x, int y) => y * W + x;
    private static readonly (int, int)[] Spans = [(At(0, 1), At(2, 1)), (At(2, 1), At(4, 1)), (At(2, 1), At(2, 3))];

    [Fact]
    public void FlowsAwayFromTheSourcesIntoEveryBranchCountingHowFarAlongEachStarts()
    {
        var runs = Flow.RunsOf(Spans, W, [At(0, 1), At(9, 9)], _ => true);
        Assert.Equal(3, runs.Count);
        Assert.Contains(new Run(0.5, 1.5, 1, 0, 2, 0), runs);
        Assert.Contains(new Run(2.5, 1.5, 0, 1, 2, 2), runs);
        Assert.Contains(new Run(2.5, 1.5, 1, 0, 2, 2), runs);
        Assert.Empty(Flow.RunsOf(Spans, W, [], _ => true));
    }

    [Fact]
    public void StopsAtANodeThatCarriesNothing()
    {
        Assert.Empty(Flow.RunsOf(Spans, W, [At(0, 1)], c => c != At(2, 1)));
        Assert.Equal([(At(0, 1), At(2, 1))], Flow.SpansIn(Spans, [At(0, 1)]));
    }

    [Fact]
    public void MovesItsDotsOnSmoothlyOneEverySoManyCells()
    {
        var runs = Flow.RunsOf([(At(0, 0), At(4, 0))], W, [At(0, 0)], _ => true);
        Assert.Equal([(0.5, 0.5), (2.5, 0.5)], Flow.DotsOf(runs, 0, 2).Order());
        Assert.Equal([(1.0, 0.5), (3.0, 0.5)], Flow.DotsOf(runs, 0.5, 2).Order());
    }
}

/// <summary>What the air overlay says: apps/game/src/features/atmosphere/explain.test.ts and draining.test.ts, one for one.</summary>
public class ExplainTests
{
    private const int Minute = 60 * World.TicksPerSecond;

    // Rates below are as the sim counts them, every ten minutes: ten times a minute's. Oxygen is the liquid behind the room's
    // vaporizers, null with none feeding it (liquid-oxygen); one vaporizer at full power makes 12000.
    private static RoomAir Room(RoomStatus status = RoomStatus.Vacuum, int aired = 0, int leak = 0, int? oxygen = null,
        int made = 0, int? lasts = null, string? problem = null, int room = 1) =>
        new(room, status, aired, leak, oxygen, made, lasts, problem);

    [Fact]
    public void GivesSpansInSecondsUnderAMinuteAndWholeMinutesPastIt()
    {
        Assert.Equal("1 s", Explain.Span(1));
        Assert.Equal("59 s", Explain.Span(59 * World.TicksPerSecond));
        Assert.Equal("6 min", Explain.Span(6 * Minute + 30 * World.TicksPerSecond));
    }

    [Fact]
    public void TellsAnAiredRoomHowLongItsPipesHoldItItsLeakCountedInOrThatTheyAreDry()
    {
        // 1200 made and 198 leaked a minute: 1002 to spare, and the 1280 of liquid leave at the leak, 7757 ticks.
        var aired = Room(RoomStatus.Aired, aired: 96, leak: 1980, oxygen: 1280, made: 12000, lasts: 7757);
        Assert.Equal(["Aired · 96 cells", "Vaporizer keeps up · +1002/min · tanks last 6 min"], Explain.Of(aired, 96));
        // With no supply, its own 96 cells go through the hull: 96 of 198 a minute. So with the tanks dry, the vaporizer
        // unpowered, or none feeding the room.
        Assert.Equal("No supply · lasts 30 s", Explain.Of(aired with { Oxygen = 0, Lasts = 0 }, 96)[1]);
        Assert.Equal("No supply · lasts 30 s", Explain.Of(aired with { Made = 0, Lasts = null }, 96)[1]);
        Assert.Equal("No supply · lasts 30 s", Explain.Of(aired with { Oxygen = null, Made = 0, Lasts = null }, 96)[1]);
    }

    [Fact]
    public void CountsWhoBreathesInTheRoomAUnitABreathIntoHowLongTheAirLasts()
    {
        var aired = Room(RoomStatus.Aired, aired: 96, leak: 1980, oxygen: 1280, made: 12000);
        // 198 leaked and 60 breathed a minute: 942 to spare, and 1280 last a little under five minutes.
        Assert.Equal("Vaporizer keeps up · +942/min · tanks last 4 min", Explain.Of(aired, 96, 1)[1]);
        Assert.Equal("Vaporizer keeps up · +1200/min · tanks last ∞", Explain.Of(aired with { Leak = 0 }, 96)[1]);
    }

    [Fact]
    public void TellsARoomHoldingAirWithNoSupplyHowMuchItHoldsAndHowLongItLasts() =>
        Assert.Equal(
            ["Some air · 30/48 cells", "No supply · lasts 30 s", "No vent here"],
            Explain.Of(Room(aired: 30, leak: 600, problem: "atmosphere.noVent"), 48));

    [Fact]
    public void TellsARoomFedByAVaporizerWhetherItKeepsUpHowLongItsTanksLastAndHowLongTheAirLastsIfNot()
    {
        var fed = Room(RoomStatus.Aired, aired: 32, leak: 480, oxygen: 40, made: 2000);
        // 200 made, 48 leaked and 60 breathed a minute each: 32 to spare with two, and the 40 of liquid leave at the 168 drawn.
        Assert.Equal(["Aired · 32 cells", "Vaporizer keeps up · +32/min · tanks last 15 s"], Explain.Of(fed, 32, 2));
        // 28 short with three: the room's own 32 cells pay it.
        Assert.Equal("Vaporizer falls short · lasts 1 min", Explain.Of(fed, 32, 3)[1]);
        Assert.Equal(["Filling · 10/32 cells", "Vaporizer keeps up · +152/min · tanks last 50 s"], Explain.Of(fed with { Status = RoomStatus.Filling, Aired = 10 }, 32));
        // A rate that is no whole number of units a minute reads to a tenth.
        Assert.Equal("Vaporizer keeps up · +31.5/min · tanks last 15 s", Explain.Of(fed with { Leak = 485 }, 32, 2)[1]);
    }

    [Fact]
    public void TellsAVentingRoomHowSoonItsPipesDrainIfItHasAPipedVentToDrainThem()
    {
        // Its vent drains 8 a tick, but liquid leaves at most at its vaporizer's 1: 450 ticks.
        var venting = Room(RoomStatus.Venting, oxygen: 450, made: 12000, lasts: 450);
        Assert.Equal(["Venting to space", "Liquid O₂ in tanks: 450 · gone in 23 s"], Explain.Of(venting, 96));
        // No vaporizer feeding it: nothing drains.
        Assert.Equal(["Venting to space"], Explain.Of(venting with { Oxygen = null, Made = 0, Lasts = null }, 96));
    }

    [Fact]
    public void TellsAFillingRoomHowFarItIsAndARoomInVacuumWhy()
    {
        // Its vaporizer unpowered: the liquid waiting behind it.
        Assert.Equal(["Filling · 40/96 cells", "Liquid O₂ in tanks: 900"], Explain.Of(Room(RoomStatus.Filling, aired: 40, oxygen: 900), 96));
        Assert.Equal(["No air · 48 cells", "The tank behind the vaporizer is empty: load a canister"], Explain.Of(Room(oxygen: 0, problem: "atmosphere.dry"), 48));
    }

    [Fact]
    public void DrainingRoomsAreTheRoomsVentingThroughAPipedVentDrainedDryOrNot() =>
        Assert.Equal(
            new HashSet<int> { 1, 2 },
            Explain.Draining([
                Room(RoomStatus.Venting, lasts: 4, room: 1),
                Room(RoomStatus.Venting, lasts: 0, room: 2),
                Room(RoomStatus.Venting, room: 3),
                Room(RoomStatus.Aired, lasts: 0, room: 4),
                Room(problem: "atmosphere.dry", room: 5),
            ]));
}
