using Stillwake.Game.Features.Atmosphere;
using Stillwake.Game.Features.Pipes;
using Stillwake.Game.Overlays;
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
    private static void Pointer(Buttons buttons, string type, int held, double x, double y, bool shift = false) =>
        buttons.OnPointer(type == "down", held, (x / Cell, y / Cell), shift);

    [Fact]
    public void ReadsLeftPressesAndReleasesInOrderAtTheirWorldPoints()
    {
        var buttons = new Buttons();
        Pointer(buttons, "down", Buttons.Left, Cell, Cell);
        Pointer(buttons, "up", 0, 2 * Cell, Cell);
        Assert.Equal([new ButtonEvent(true, (1, 1), false), new ButtonEvent(false, (2, 1), false)], buttons.TakeLeft());
        Assert.Empty(buttons.TakeLeft());
    }

    [Fact]
    public void ReadsRightPressesAndReleasesMiddlePressesAndShiftHeldAtEach()
    {
        var buttons = new Buttons();
        Pointer(buttons, "down", Buttons.Right, 0, 0, shift: true);
        Pointer(buttons, "move", Buttons.Right | Buttons.Middle, Cell, 0);
        Pointer(buttons, "move", Buttons.Right, Cell, 0);
        Pointer(buttons, "up", 0, 2 * Cell, 0);
        Assert.Equal([new ButtonEvent(true, (0, 0), true), new ButtonEvent(false, (2, 0), false)], buttons.TakeRight());
        Assert.Equal([(1.0, 0.0)], buttons.TakeMiddle());
        Assert.Empty(buttons.TakeRight());
        Assert.Empty(buttons.TakeMiddle());
        Assert.Empty(buttons.TakeLeft());
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

/// <summary>The overlay switch: apps/game/src/overlays/switch.test.ts, one for one.</summary>
public class OverlaySwitchTests
{
    private static OverlaySwitch SetUp(Store<string?> on) =>
        new([new Overlay("air", "overlay.air", "O"), new Overlay("pipes", "overlay.pipes", "P")], on);

    [Fact]
    public void TurnsAnOverlayOnWithItsKeyAndOffWithItAgain()
    {
        var overlays = SetUp(new Store<string?>(null));
        Assert.Null(overlays.Active);
        overlays.Press("O");
        Assert.Equal("air", overlays.Active);
        overlays.Press("O");
        Assert.Null(overlays.Active);
    }

    [Fact]
    public void SwapsToAnotherOverlayOnThatOnesKeyAndIgnoresOtherKeysAndRepeats()
    {
        var overlays = SetUp(new Store<string?>(null));
        overlays.Press("O");
        overlays.Press("P");
        Assert.Equal("pipes", overlays.Active);
        overlays.Press("E");
        overlays.Press("P", repeat: true);
        Assert.Equal("pipes", overlays.Active);
    }

    [Fact]
    public void TellsItsListenersOfEachChange()
    {
        var on = new Store<string?>(null);
        var overlays = SetUp(on);
        int heard = 0;
        on.Subscribe(() => heard++);
        overlays.Press("O");
        overlays.Press("E");
        Assert.Equal(1, heard);
    }
}

/// <summary>Oxygen as the pipes overlay shows it flowing: apps/game/src/features/pipes/flow.test.ts, one for one.</summary>
public class FlowTests
{
    // Five wide: a run along the middle row with a branch down from its third cell.
    private const int W = 5;
    private static int At(int x, int y) => y * W + x;
    private static readonly int[] Pipes = [At(0, 1), At(1, 1), At(2, 1), At(3, 1), At(4, 1), At(2, 2), At(2, 3)];

    [Fact]
    public void CountsEachCellItsFewestStepsFromASourceAndLeavesOutWhatNoneReaches()
    {
        var steps = Flow.StepsFrom(Pipes, W, [At(0, 1), At(9, 9)]);
        Assert.Equal(0, steps[At(0, 1)]);
        Assert.Equal(4, steps[At(4, 1)]);
        Assert.Equal(4, steps[At(2, 3)]);
        Assert.Empty(Flow.StepsFrom(Pipes, W, []));
    }

    [Fact]
    public void FlowsAwayFromTheSourcesIntoEveryBranch()
    {
        var runs = Flow.RunsOf(Flow.StepsFrom(Pipes, W, [At(0, 1)]), W);
        Assert.Equal(Pipes.Length - 1, runs.Count);
        Assert.Contains(new Run(2.5, 1.5, 0, 1, 2), runs);
        Assert.DoesNotContain(runs, r => r.X == 1.5 && r.Dx == -1);
    }

    [Fact]
    public void MovesItsDotsOnSmoothlyOneEverySoManyCells()
    {
        var runs = Flow.RunsOf(Flow.StepsFrom([At(0, 0), At(1, 0), At(2, 0), At(3, 0), At(4, 0)], W, [At(0, 0)]), W);
        Assert.Equal([(0.5, 0.5), (2.5, 0.5)], Flow.DotsOf(runs, 0, 2).Order());
        Assert.Equal([(1.0, 0.5), (3.0, 0.5)], Flow.DotsOf(runs, 0.5, 2).Order());
    }
}

/// <summary>What the air overlay says: apps/game/src/features/atmosphere/explain.test.ts and draining.test.ts, one for one.</summary>
public class ExplainTests
{
    private const int Minute = 60 * World.TicksPerSecond;

    // Rates below are as the sim counts them, every ten minutes: ten times a minute's.
    private static RoomAir Room(RoomStatus status = RoomStatus.Vacuum, int aired = 0, int leak = 0, int oxygen = 0,
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
        var aired = Room(RoomStatus.Aired, aired: 96, leak: 1980, oxygen: 1280, lasts: 6 * Minute);
        Assert.Equal(["Aired · 96 cells", "O₂ in pipes: 1280 · lasts 6 min"], Explain.Of(aired, 96));
        // With no supply, its own 96 cells go through the hull: 96 of 198 a minute.
        Assert.Equal("No supply · lasts 30 s", Explain.Of(aired with { Oxygen = 0, Lasts = 0 }, 96)[1]);
    }

    [Fact]
    public void CountsWhoBreathesInTheRoomAUnitABreathIntoHowLongTheAirLasts()
    {
        var aired = Room(RoomStatus.Aired, aired: 96, leak: 1980, oxygen: 1280);
        // 198 leaked and 60 breathed a minute: 1280 last a little under five minutes.
        Assert.Equal("O₂ in pipes: 1280 · lasts 4 min", Explain.Of(aired, 96, 1)[1]);
        Assert.Equal("O₂ in pipes: 1280 · lasts ∞", Explain.Of(aired with { Leak = 0 }, 96)[1]);
    }

    [Fact]
    public void TellsARoomHoldingAirWithNoSupplyHowMuchItHoldsAndHowLongItLasts() =>
        Assert.Equal(
            ["Some air · 30/48 cells", "No supply · lasts 30 s", "No vent here"],
            Explain.Of(Room(aired: 30, leak: 600, problem: "atmosphere.noVent"), 48));

    [Fact]
    public void TellsARoomFedByAGeneratorWhetherItKeepsUpAndHowLongTheAirLastsIfNot()
    {
        var fed = Room(RoomStatus.Aired, aired: 32, leak: 480, oxygen: 40, made: 2000);
        // 200 made, 48 leaked and 60 breathed a minute each: 32 to spare with two, 28 short with three.
        Assert.Equal(["Aired · 32 cells", "Generator keeps up · +32/min"], Explain.Of(fed, 32, 2));
        Assert.Equal("Generator falls short · lasts 2 min", Explain.Of(fed, 32, 3)[1]);
        Assert.Equal(["Filling · 10/32 cells", "Generator keeps up · +152/min"], Explain.Of(fed with { Status = RoomStatus.Filling, Aired = 10 }, 32));
        // A rate that is no whole number of units a minute reads to a tenth.
        Assert.Equal("Generator keeps up · +31.5/min", Explain.Of(fed with { Leak = 485 }, 32, 2)[1]);
    }

    [Fact]
    public void TellsAVentingRoomHowSoonItsPipesDrainIfItHasAPipedVentToDrainThem()
    {
        var venting = Room(RoomStatus.Venting, oxygen: 450, lasts: 2);
        Assert.Equal(["Venting to space", "O₂ in pipes: 450 · gone in 1 s"], Explain.Of(venting, 96));
        Assert.Equal(["Venting to space"], Explain.Of(venting with { Oxygen = 0, Lasts = null }, 96));
    }

    [Fact]
    public void TellsAFillingRoomHowFarItIsAndARoomInVacuumWhy()
    {
        Assert.Equal(["Filling · 40/96 cells", "O₂ in pipes: 900"], Explain.Of(Room(RoomStatus.Filling, aired: 40, oxygen: 900), 96));
        Assert.Equal(["No air · 48 cells", "Pipes empty: load a canister into a tank"], Explain.Of(Room(problem: "atmosphere.dry"), 48));
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
