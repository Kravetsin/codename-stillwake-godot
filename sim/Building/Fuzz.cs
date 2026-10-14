using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Building;

public sealed partial class BuildingModule
{
    private static readonly Placement[] FuzzPlacements =
    [
        .. Materials.All.Select(m => Placement.Wall(m.Id)),
        .. new[] { "floor", "ladder", "door" }.Select(kind => new Placement("structure", kind)),
        .. new[] { "pipe", "valve" }.Select(kind => new Placement("conduit", kind)),
        .. Machines.All.Select(m => new Placement("machine", m.Kind)),
        .. Materials.All.Select(m => Placement.BackWall(m.Id)),
    ];
    private static readonly string[] FuzzLayers = ["structure", "conduit", "machine", "back"];

    // The module draws without the grid, as TypeScript's generator does. The generic suites fuzz it on the
    // wreck, so cells are drawn from the wreck's size, and marks mostly from the cells where it has something built.
    private static readonly Layout FuzzWreck = Layout.Parse(Wreck.Rows);
    private static readonly int FuzzCells = FuzzWreck.Width * FuzzWreck.Height;
    private static readonly int[] FuzzBuilt = Enumerable.Range(0, FuzzCells)
        .Where(c => "#=HDd-vxTVK.".Contains(FuzzWreck.At(c))).ToArray();

    /// <summary>
    /// Draws every command, refused ones included: off the grid, on nothing, twice on one thing, E on a
    /// plan that is gone or a blueprint the hero has nothing for.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        int Anywhere() => Below(FuzzCells + 2) - 1;
        var marks = State.Plans.Where(p => p.Clear != null).ToList();
        switch (Below(5))
        {
            case 0:
                return new PlacePlan(Anywhere(), FuzzPlacements[Below(FuzzPlacements.Length)]) { From = from };
            case 1:
                {
                    int pick = Below(4);
                    int cell = pick == 0 && marks.Count > 0 ? marks[Below(marks.Count)].Cell
                        : pick < 3 && FuzzBuilt.Length > 0 ? FuzzBuilt[Below(FuzzBuilt.Length)]
                        : Anywhere();
                    return new Deconstruct(cell, FuzzLayers[Below(FuzzLayers.Length)]) { From = from };
                }
            case 2:
                return new CancelPlan(Below(State.NextPlanId + 2)) { From = from };
            default:
                {
                    // Mostly on the oldest plan or a mark, so plans complete within a run: a blueprint only in
                    // creative mode, which the items fuzz switches on now and then, since no hero carries
                    // anything there; otherwise its items.notEnough is drawn.
                    int pick = Below(8);
                    int plan = State.Plans.Count > 0 && pick < 4 ? State.Plans[0].Id
                        : marks.Count > 0 && pick < 7 ? marks[Below(marks.Count)].Id
                        : Below(State.NextPlanId + 2);
                    return new Work(plan) { From = from };
                }
        }
    }
}
