using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Building;

public sealed partial class BuildingModule
{
    private static readonly Placement[] FuzzPlacements =
    [
        .. Materials.All.Select(m => Placement.Wall(m.Id)),
        .. Builds.All.Select(b => new Placement("structure", b.Id)),
        .. Machines.All.Select(m => new Placement("machine", m.Kind)),
        .. Materials.All.Select(m => Placement.BackWall(m.Id)),
    ];
    private static readonly string[] FuzzLayers = ["structure", "machine", "back"];

    /// <summary>How far round what is built cells are mostly drawn: as players build, by their ship.</summary>
    private const int FuzzMargin = 32;

    /// <summary>
    /// Draws every command, refused ones included: off the grid, on nothing, twice on one thing, E on a
    /// plan that is gone or a blueprint the hero has nothing for, a dig at a hull wall or back wall, bedrock or nothing, or with a drill not running.
    /// </summary>
    /// <summary>A standing plant drawn at random, drawn again up to three times while it lies in rock (0080).</summary>
    private Plant FuzzPlant(Func<int, int> below)
    {
        var plant = _plants[below(_plants.Count)];
        for (int i = 0; i < 3 && Kinds[plant.Kind].On == Flora.Anchor.Rock; i++) plant = _plants[below(_plants.Count)];
        return plant;
    }

    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        // Cells mostly round what is built, now and then anywhere on the grid or just off it, so the
        // build zone's edge and the grid's are drawn too; marks mostly within what is built.
        var built = _grid.Built;
        int Anywhere() => Below(_grid.Width * _grid.Height + 2) - 1;
        int Within(int margin)
        {
            if (built.IsEmpty) return Anywhere();
            int left = Math.Max(0, built.Left - margin), right = Math.Min(_grid.Width - 1, built.Right + margin);
            int top = Math.Max(0, built.Top - margin), bottom = Math.Min(_grid.Height - 1, built.Bottom + margin);
            return (top + Below(bottom - top + 1)) * _grid.Width + left + Below(right - left + 1);
        }
        int Cell() => Below(8) == 0 ? Anywhere() : Within(FuzzMargin);
        var marks = State.Plans.Where(p => p.Clear != null).ToList();
        // On grown ground digging is most of what heroes do: half the draws there are digs.
        switch (Rules is Content.PlanetType && Below(2) == 0 ? 5 : Below(6))
        {
            case 5:
                {
                    // A quarter of the time on a plant, one in an open cell sought, or what holds it (0079, 0080), so they fall; else
                    // mostly on a crack, so walls break within a run, or anywhere, mostly on the ground a planet is made of.
                    int cell = _plants.Count > 0 && Below(4) == 0 ? (FuzzPlant(Below) is var p && Below(2) == 0 ? p.Cell : HoldOf(p))
                        : State.Cracks.Count > 0 && Below(2) == 0 ? State.Cracks[Below(State.Cracks.Count)].Cell : Cell();
                    // A quarter of digs hit the back wall (0107), ship and ground alike.
                    return new Dig(cell, Weapons.All[Below(Weapons.All.Count)].Item, Back: Below(4) == 0) { From = from };
                }
            case 0:
                return new PlacePlan(Cell(), FuzzPlacements[Below(FuzzPlacements.Length)]) { From = from };
            case 1:
                {
                    int pick = Below(4);
                    int cell = pick == 0 && marks.Count > 0 ? marks[Below(marks.Count)].Cell
                        : pick < 3 ? Within(0)
                        : Cell();
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
