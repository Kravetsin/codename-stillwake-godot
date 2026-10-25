using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Landing;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The drop pod's first slice, the bell: a machine four cells by six standing only on a launch platform, a hero boarding it
/// and leaving it, one hero a pod, its one slot taking biofuel only. Nothing launches yet.
/// </summary>
public class DropPodTests
{
    /// <summary>A room four characters tall over a run of two platform pieces in its floor, space under it.</summary>
    private static readonly string[] Bare =
    [
        "##########",
        "#........#",
        "#........#",
        "#........#",
        "#........#",
        "###__#####",
        "          ",
    ];

    /// <summary>The same room, a drop pod standing on its run.</summary>
    private static readonly string[] Stood =
    [
        "##########",
        "#........#",
        "#..XX....#",
        "#..XX....#",
        "#..XX....#",
        "###__#####",
        "          ",
    ];

    private const int Wide = 20;
    private static int At(int x, int y) => y * Wide + x;
    private static readonly Placement Pod = new("machine", "dropPod");
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Scenario World(string[] rows) => new(Layout.Parse(rows), doubled: true);
    private static int PodId(Scenario w) => Assert.Single(w.M.Grid.MachinesWith(MachineTrait.Seats)).Id;

    [Fact]
    public void IsABellFourByCellsSixOfFramesPlatesGlassTubesAProcessorAndADisplayOnTheShipTab()
    {
        var spec = Machines.Get("dropPod");
        Assert.Equal((4, 6), (spec.Width, spec.Height));
        Assert.Equal([("frame", 4), ("plate", 8), ("glass", 2), ("tube", 3), ("processor", 1), ("display", 1)], spec.Cost);
        Assert.Equal(MachineTrait.Seats | MachineTrait.Lands, spec.Traits);
        Assert.Equal(1, spec.Slots);
        Assert.Equal(["biofuel"], spec.Takes!);
        Assert.Equal(MenuTabs.Ship, spec.Menu!.Tab);
        // Its blueprint opens with the computer's parts analysed: late in space.
        Assert.DoesNotContain("dropPod", Content.Research.StartKnown);
    }

    [Fact]
    public void IsPlannedOnlyWithALaunchPlatformUnderAllOfItsBottomRow()
    {
        var w = World(Bare);
        // Over the run: the two pieces, cells 6 to 9 of row 10, its bottom row just above them.
        Assert.Null(w.M.Grid.PlacementProblem(At(6, 4), Pod));
        // Half over the run, beside it, and a row too high.
        Assert.Equal("grid.noPlatform", w.M.Grid.PlacementProblem(At(4, 4), Pod));
        Assert.Equal("grid.noPlatform", w.M.Grid.PlacementProblem(At(10, 4), Pod));
        Assert.Equal("grid.noPlatform", w.M.Grid.PlacementProblem(At(6, 3), Pod));
        w.Do(new PlacePlan(At(4, 4), Pod)).Do(new PlacePlan(At(6, 4), Pod)).Run();
        Assert.Equal(["grid.noPlatform"], Reasons(w));
        Assert.Equal(At(6, 4), Assert.Single(w.M.Building.Plans).Cell);
    }

    [Fact]
    public void StandsOnAnOpenRunTooButNeverWhereTheGroundIsAnythingElse()
    {
        // A floor laid where the pod would stand is no platform.
        var floored = World([.. Bare[..5], "###==#####", Bare[6]]);
        Assert.Equal("grid.noPlatform", floored.M.Grid.PlacementProblem(At(6, 4), Pod));
        // An open run is a platform still: the pod may be planned over it.
        var open = World([.. Bare[..5], "###~~#####", Bare[6]]);
        Assert.Equal(Structure.PlatformOpen, open.M.Grid.StructureAt(At(6, 10)));
        Assert.Null(open.M.Grid.PlacementProblem(At(6, 4), Pod));
    }

    [Fact]
    public void SeatsTheHeroThatBoardsItAndLetsItLeave()
    {
        var w = World(Stood).Do(new Enter(At(12, 9))).Run();
        int pod = PodId(w);
        Assert.Null(w.M.Landing.SeatOf(0));
        w.Do(new Board(pod)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(pod, w.M.Landing.SeatOf(0));
        Assert.Equal(0, w.M.Landing.RiderOf(pod));
        w.Do(new Alight()).Run();
        Assert.Null(w.M.Landing.SeatOf(0));
        Assert.Null(w.M.Landing.RiderOf(pod));
    }

    [Fact]
    public void RefusesAnotherHeroInATakenPodASeatedOneANonePodAndLeavingUnseated()
    {
        var w = World(Stood).Do(new Enter(At(12, 9))).Do(new Enter(At(13, 9)) { From = 1 }).Run();
        int pod = PodId(w), crate = pod + 100;
        w.Do(new Alight()).Do(new Board(pod)).Do(new Board(pod) { From = 1 }).Do(new Board(pod)).Do(new Board(crate) { From = 1 }).Run();
        Assert.Equal(["landing.notSeated", "landing.taken", "landing.seated", "landing.noPod"], Reasons(w));
        Assert.Equal(pod, w.M.Landing.SeatOf(0));
        Assert.Null(w.M.Landing.SeatOf(1));
    }

    [Fact]
    public void FreesTheSeatOfAHeroThatLeftThePodTakenApartOrAHeroPassedOut()
    {
        var w = World(Stood).Do(new Enter(At(12, 9))).Run();
        int pod = PodId(w);
        w.Do(new Board(pod)).Do(new Leave()).Run();
        Assert.Null(w.M.Landing.RiderOf(pod));
        w.Do(new Enter(At(12, 9))).Do(new Board(pod)).Run();
        Assert.Equal(0, w.M.Landing.RiderOf(pod));
        w.Do(new SetCreative(true)).Do(new Deconstruct(At(6, 4), "machine")).Run();
        for (int i = 0; i < 400 && w.M.Grid.MachineById(pod) != null; i++) w.Do(new Work(w.M.Building.Plans.Single().Id)).Run();
        Assert.Null(w.M.Grid.MachineById(pod));
        Assert.Empty(w.M.Landing.Seats);
    }

    [Fact]
    public void TakesBiofuelIntoItsSlotAndNothingElse()
    {
        var w = World(Stood).Do(new Enter(At(12, 9))).GiveItems(0, ("biofuel", 3), ("scrap", 2)).Run();
        int pod = PodId(w);
        int Slot(string item) => w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        w.Do(new Move(new Stillwake.Sim.Items.Place(Slot("scrap")), new Target(null, pod), 2)).Run();
        Assert.Equal(["items.notTaken"], Reasons(w));
        w.Do(new Move(new Stillwake.Sim.Items.Place(Slot("biofuel")), new Target(null, pod), 3)).Run();
        Assert.Equal(new Stack("biofuel", 3), w.M.Items.Crate(pod)[0]);
    }
}
