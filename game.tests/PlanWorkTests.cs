using Stillwake.Game.Features.Building;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The left button held on a plan: the plan tests of apps/game/src/features/crew/aim.test.ts, one for
/// one, as far as plans go; doors, valves, tanks and crates are E's, in the crew feature's tests.
/// </summary>
public class PlanWorkTests
{
    private static readonly HashSet<int> None = [];

    /// <summary>A hall nine wide with rows enough inside for <paramref name="cells"/> cells, seven to a row.</summary>
    private static string Hall(int cells) =>
        string.Join("\n", new[] { "#########" }.Concat(Enumerable.Repeat("#.......#", (cells + 6) / 7)).Append("#########"));

    /// <summary>The <paramref name="i"/>-th cell inside a hall, row by row.</summary>
    private static int Inside(int i) => (1 + i / 7) * 9 + 1 + i % 7;

    [Fact]
    public void WorksTheOldestPlanInACell()
    {
        var w = new TestWorld("""
            ########
            #..D.v.#
            ########
            """);
        int door = 8 + 3;
        w.Step(new Deconstruct(door, "structure"), new PlacePlan(door, new Placement("conduit", "pipe")));
        Assert.Equal(new PlanAim(1, null), PlanWork.AimAt(w.M.Building, 0, door, None));
    }

    [Fact]
    public void CarriesTheSimsReasonWhenWorkOnABlueprintWouldBeRefused()
    {
        // An iron wall planned for a hero carrying nothing.
        var w = new TestWorld(Hall(1));
        w.Step(new PlacePlan(Inside(0), Placement.Wall("iron")));
        Assert.Equal(new PlanAim(1, "items.notEnough", Delivers: true), PlanWork.AimAt(w.M.Building, 0, Inside(0), None));
    }

    [Fact]
    public void WorksAPlanItCanBeforeAnOlderOneItCannot()
    {
        // Nine wide. A tank is planned over a pipe, and the pipe is marked: two plans in cell 15.
        var w = new TestWorld(Layout.Parse(["#########", "#.....-.#", "#.......#", "#########"]));
        w.Step(new PlacePlan(15, new Placement("machine", "tank")), new Deconstruct(15, "conduit"));
        var q = w.M.Building;
        Assert.Equal([(1, "items.notEnough"), (2, (string?)null)], q.PlansAt(15).Select(p => (p.Id, q.WorkProblem(p.Id, 0))));
        Assert.Equal(new PlanAim(2, null), PlanWork.AimAt(q, 0, 15, None));
    }

    [Fact]
    public void PassesOverABlueprintDeliveredToInThisHoldToTheNextInItsCell()
    {
        // A wall and a pipe through it, both planned in one cell, for a hero carrying scrap.
        var w = new TestWorld(Hall(1)).Give(0, ("scrap", 360), ("iron", 60));
        w.Step(new PlacePlan(Inside(0), Placement.Wall("scrap")), new PlacePlan(Inside(0), new Placement("conduit", "pipe")));
        Assert.Equal(new PlanAim(1, null, Delivers: true), PlanWork.AimAt(w.M.Building, 0, Inside(0), None));
        Assert.Equal(new PlanAim(2, null, Delivers: true), PlanWork.AimAt(w.M.Building, 0, Inside(0), new HashSet<int> { 1 }));
    }

    private static readonly PlanAim OnPlan = new(7, null);
    private static readonly PlanAim ToPlan = new(7, null, Delivers: true);

    [Fact]
    public void DeliversToABlueprintOnceNamingItSoTheSweepPassesItBy()
    {
        var uses = PlanWork.UsesOf(ToPlan, inReach: true, held: true, tick: 5, workedOnTick: -1);
        Assert.Equal([new Work(7)], uses.Commands);
        Assert.Equal((7, -1), (uses.Served, uses.WorkedOnTick));
    }

    [Fact]
    public void SendsOneWorkForEveryTickReleasedWhileHeldOnAPlanWithAllItNeeds()
    {
        int worked = -1;
        var sent = new List<int>();
        foreach (int tick in new[] { 5, 5, 6, 6, 8 })
        {
            var uses = PlanWork.UsesOf(OnPlan, true, true, tick, worked);
            worked = uses.WorkedOnTick;
            sent.Add(uses.Commands.Count);
        }
        // One when the hold begins, none until the next tick, then one per tick, two after a skip.
        Assert.Equal([1, 0, 1, 0, 2], sent);
    }

    [Fact]
    public void CatchesUpAfterAStallAFewTicksAtMostAndStartsAfreshWhenALoadTakesTheTickBack()
    {
        Assert.Equal(5, PlanWork.UsesOf(OnPlan, true, true, 100, 10).Commands.Count);
        var fresh = PlanWork.UsesOf(OnPlan, true, true, 3, 50);
        Assert.Equal([new Work(7)], fresh.Commands);
        Assert.Equal(3, fresh.WorkedOnTick);
    }

    [Fact]
    public void SendsNothingOutOfReachForAPlanTheSimWouldRefuseOrWithTheButtonUp()
    {
        var far = PlanWork.UsesOf(OnPlan, false, true, 5, 4);
        Assert.Empty(far.Commands);
        Assert.Equal(-1, far.WorkedOnTick);
        Assert.Empty(PlanWork.UsesOf(new PlanAim(7, "items.notEnough"), true, true, 5, -1).Commands);
        var up = PlanWork.UsesOf(OnPlan, true, false, 5, 4);
        Assert.Empty(up.Commands);
        Assert.Equal(-1, up.WorkedOnTick);
    }
}
