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
        // Walls cut to every shape (0069), and a shape where none may be, refused.
        .. Materials.All.SelectMany(m => Shape.Cut.Select(shape => Placement.Wall(m.Id) with { Shape = shape })),
        new("structure", "floor", Shape: Shape.BottomLeft),
        Placement.BackWall() with { Shape = Shape.TopRight },
        new("machine", "crate", Shape: Shape.TopLeft),
    ];
    private static readonly string[] FuzzLayers = ["structure", "conduit", "machine", "back"];

    /// <summary>How far round what is built cells are mostly drawn: as players build, by their ship.</summary>
    private const int FuzzMargin = 16;

    /// <summary>
    /// Draws every command, refused ones included: off the grid, on nothing, twice on one thing, E on a
    /// plan that is gone or a blueprint the hero has nothing for, a dig at hull, bedrock or nothing, or with a drill not running.
    /// </summary>
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
        switch (Rules.Ground != null && Below(2) == 0 ? 5 : Below(6))
        {
            case 5:
                {
                    // Mostly on a crack, so walls break within a run; else anywhere, mostly on the ground a planet is made of.
                    // Now and then on a plant (0079), so they are felled, and under one, so their ground goes.
                    int cell = _plants.Count > 0 && Below(8) == 0 ? _plants[Below(_plants.Count)].Cell + (Below(2) == 0 ? 0 : _grid.Width)
                        : State.Cracks.Count > 0 && Below(2) == 0 ? State.Cracks[Below(State.Cracks.Count)].Cell : Cell();
                    return new Dig(cell, Weapons.All[Below(Weapons.All.Count)].Item) { From = from };
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
