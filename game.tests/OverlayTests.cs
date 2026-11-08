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

/// <summary>
/// What the air overlay says: apps/game/src/features/atmosphere/explain.test.ts and draining.test.ts, then in the bubble's
/// terms (tip-signs): a room's share of its vents' making and its leak a minute, the liquid behind it, why it falls short.
/// </summary>
public class ExplainTests
{
    private const int Minute = 60 * World.TicksPerSecond;

    // Rates below are as the sim counts them, every ten minutes: ten times a minute's. Oxygen is the liquid behind the room's
    // vaporizers, null with none feeding it (liquid-oxygen); one vaporizer at full power makes 12000.
    private static RoomAir Room(RoomStatus status = RoomStatus.Vacuum, int aired = 0, int leak = 0, int? oxygen = null,
        int made = 0, int budget = 0, int? lasts = null, string? problem = null, int room = 1) =>
        new(room, status, aired, leak, oxygen, made, budget, lasts, problem);

    private static TipLine Made(string n) => new($"+{n} l/min", Sign: Sign.Oxygen);
    private static TipLine Leak(string n, Tone tone = Tone.Plain) => new($"−{n} l/min", tone, Sign: Sign.Leak);
    private static TipLine Liquid(string text, Tone tone = Tone.Plain) => new(text, tone, "canister");
    private static TipLine Bad(string text) => new(text, Tone.Bad);

    [Fact]
    public void GivesSpansInSecondsUnderAMinuteAndWholeMinutesPastIt()
    {
        Assert.Equal("1 s", Explain.Span(1));
        Assert.Equal("59 s", Explain.Span(59 * World.TicksPerSecond));
        Assert.Equal("6 min", Explain.Span(6 * Minute + 30 * World.TicksPerSecond));
    }

    [Fact]
    public void TellsAnAiredRoomItsShareOfTheMakingItsLeakAndTheLiquidBehindItAMinuteEach()
    {
        // 1200 made and 198 leaked a minute; the 1280 of liquid leave at the leak, 7757 ticks.
        var aired = Room(RoomStatus.Aired, aired: 96, leak: 1980, oxygen: 1280, made: 12000, budget: 12000, lasts: 7757);
        Assert.Equal([new("Aired · 96 cells"), Made("1.200"), Leak("0.198"), Liquid("1.280 l · 6 min")], Explain.Of(aired, 96));
        // The pod's air never runs out: no liquid to speak of.
        Assert.Equal([new("Aired · 96 cells"), Made("1.200"), Leak("0.198")], Explain.Of(aired with { Oxygen = null, Lasts = null }, 96));
        // A rate that is no whole number of units a minute reads to a tenth, rounded down.
        Assert.Equal(Made("0.0485"), Explain.Of(aired with { Budget = 485 }, 96)[1]);
        Assert.Equal(Made("0.0489"), Explain.Of(aired with { Budget = 489 }, 96)[1]);
    }

    [Fact]
    public void TellsARoomSharingItsVaporizerItsOwnShareNotTheWholeMaking() =>
        Assert.Equal(Made("0.600"), Explain.Of(Room(RoomStatus.Filling, aired: 40, leak: 800, oxygen: 900, made: 12000, budget: 6000, lasts: 13500), 96)[1]);

    [Fact]
    public void TellsARoomItsWallsHoldShortOfItsVolumeTheLeakInRedAndWhy()
    {
        var held = Room(RoomStatus.Filling, aired: 90, leak: 5970, oxygen: 900, made: 12000, budget: 6000, lasts: 1800, problem: "atmosphere.leaky");
        Assert.Equal(
            [new("Filling · 90/96 cells"), Made("0.600"), Leak("0.597", Tone.Bad), Liquid("0.900 l · 1 min"),
                Bad("The walls leak more than the vaporizers make: build better walls or another vaporizer")],
            Explain.Of(held, 96));
    }

    [Fact]
    public void TellsARoomInVacuumWhyItsLiquidRedWhenDryAndNoRatesWithNoShare()
    {
        Assert.Equal([new("No air · 48 cells"), Liquid("0.000 l", Tone.Bad), Bad("The tank behind the vaporizer is empty: load a canister")],
            Explain.Of(Room(oxygen: 0, problem: "atmosphere.dry"), 48));
        // Its vaporizer unpowered: the liquid waits behind it, drawn by nothing.
        Assert.Equal([new("No air · 96 cells"), Liquid("0.900 l"), Bad("Vaporizer has no power")], Explain.Of(Room(oxygen: 900, problem: "atmosphere.unpowered"), 96));
        // Its air fading from the far edge: how much is left, and why.
        Assert.Equal([new("Some air · 30/48 cells"), Bad("No vent here")], Explain.Of(Room(aired: 30, leak: 600, problem: "atmosphere.noVent"), 48));
    }

    [Fact]
    public void TellsAVentingRoomHowSoonItsPipesDrainIfItHasAPipedVentToDrainThem()
    {
        // Its vent drains 8 a tick, but liquid leaves at most at its vaporizer's 1: 450 ticks.
        var venting = Room(RoomStatus.Venting, oxygen: 450, made: 12000, lasts: 450);
        Assert.Equal([new("Venting to space"), Liquid("0.450 l · 23 s")], Explain.Of(venting, 96));
        // No vaporizer feeding it: nothing drains.
        Assert.Equal([new("Venting to space")], Explain.Of(venting with { Oxygen = null, Made = 0, Lasts = null }, 96));
    }

    [Fact]
    public void SpeaksOnlyOverABareBackWallNeverOverAStructureAMachineAPlanOrNoBackWall()
    {
        const int empty = Stillwake.Content.Structure.Empty;
        Assert.True(Explain.SpeaksOver(empty, machine: false, back: true, planned: false));
        Assert.False(Explain.SpeaksOver(empty, machine: true, back: true, planned: false));
        // A blueprint or a mark under the pointer: its cost speaks there, the overlay left on while building.
        Assert.False(Explain.SpeaksOver(empty, machine: false, back: true, planned: true));
        Assert.False(Explain.SpeaksOver(empty, machine: false, back: false, planned: false));
        foreach (int structure in new[] { Stillwake.Content.Structure.Ladder, Stillwake.Content.Structure.DoorOpen, Stillwake.Content.Structure.DoorClosed, Stillwake.Content.Structure.Grate })
            Assert.False(Explain.SpeaksOver(structure, machine: false, back: true, planned: false));
    }

    [Fact]
    public void DrainingRoomsAreTheRoomsVentingThroughAPipedVentDrainedDryOrNot() =>
        Assert.Equal(
            new HashSet<int> { 1, 2, 6 },
            Explain.Draining([
                Room(RoomStatus.Venting, lasts: 4, room: 1),
                Room(RoomStatus.Venting, lasts: 0, room: 2),
                Room(RoomStatus.Venting, room: 3),
                new RoomAir(6, RoomStatus.Venting, 10, 5, 0, 12000, 0, null, null),
                Room(RoomStatus.Aired, lasts: 0, room: 4),
                Room(problem: "atmosphere.dry", room: 5),
            ]));
}
