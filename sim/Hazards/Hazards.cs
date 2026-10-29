using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Hazards;

/// <summary>A meteor seen coming (2026-10-28-hull-meteors): it strikes <c>Cell</c> with <c>Power</c> once <c>Left</c> more ticks have run.</summary>
public sealed record Meteor(int Cell, int Power, int Left);

/// <summary>
/// The hazards slice. Public so modules can be typed by it, its members internal: others read it through
/// <see cref="HazardsModule"/>'s queries.
/// </summary>
public sealed class HazardsState
{
    /// <summary>A world whose map did not ask for meteors (<c>Layout.Meteors</c>): a test's, a dev world's. Off, not written.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal bool Calm { get; set; }
    /// <summary>Ticks till the next meteor is seen coming; 0 before the first is drawn.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int NextIn { get; set; }
    /// <summary>The meteor seen coming, if one is.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal Meteor? Incoming { get; set; }
}

/// <summary>A meteor hits the wall at <paramref name="Cell"/>: from the dev panel, to try a breach and its repair. It always breaks it.</summary>
public sealed record MeteorHit(int Cell) : Command;

/// <summary>Meteors breaking the hull (packages/sim/src/modules/hazards; 2026-10-28-hull-meteors).</summary>
public sealed class HazardsModule : Module<HazardsState>
{
    private readonly IGridQueries _grid;
    private readonly IGridOps _ops;
    private readonly List<int> _targets = [], _blast = [], _backs = [];
    private int[] _spent = [];
    private List<int>[] _byCost = [];

    public override string Id => "hazards";

    public HazardsModule(IGridQueries grid, IGridOps ops)
    {
        _grid = grid;
        _ops = ops;
        Handles<MeteorHit>(c => HitProblem(c.Cell), c => Break(c.Cell));
        // In construction, as a command would: building drops a mark on the broken wall and the rooms see the hole on its tick.
        System(Phase.Construction, Fall);
    }

    /// <summary>The meteor seen coming, for its streak; null while none is.</summary>
    public Meteor? Incoming => State.Incoming;

    /// <summary>
    /// Why a meteor at <paramref name="cell"/> breaks nothing: no wall there (<c>hazards.noWall</c>), or one that never breaks, bedrock
    /// or lava, or in a planet's fog, out of the grid's zone (<c>hazards.neverBreaks</c>).
    /// </summary>
    private string? HitProblem(int cell)
    {
        if (_grid.WallAt(cell) is not { } wall) return "hazards.noWall";
        bool zone = _grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width);
        return wall.Armour == Material.Unbreakable || !zone ? "hazards.neverBreaks" : null;
    }

    /// <summary>
    /// A tick of the place's shower: the meteor seen coming strikes when its time is up (<see cref="Strike"/>); else, when the next
    /// is due, one is drawn at a wall open to the drift.
    /// </summary>
    private void Fall()
    {
        if (State.Calm || Rules.Meteors is not { } shower) return;
        var s = State;
        if (s.Incoming is { } meteor)
        {
            if (meteor.Left > 1)
            {
                s.Incoming = meteor with { Left = meteor.Left - 1 };
                return;
            }
            s.Incoming = null;
            Strike(meteor, shower);
            s.NextIn = Interval(shower);
            return;
        }
        if (s.NextIn == 0) s.NextIn = Interval(shower);
        if (--s.NextIn > 0) return;
        Targets();
        if (_targets.Count == 0)
        {
            s.NextIn = Interval(shower);
            return;
        }
        int cell = _targets[(int)Random.Below((uint)_targets.Count)];
        s.Incoming = new Meteor(cell, PowerOf(shower), shower.Warning);
    }

    /// <summary>
    /// <paramref name="meteor"/> strikes (2026-10-29-meteor-wave): if the wall it meets still stands and its power reaches the wall's
    /// strength, a blast of <see cref="Charge"/> spreads from it as a wave, cheapest way first. A cell costs it <c>Step</c> to reach
    /// (<c>Diagonal</c> aslant) and a wall <c>PerStrength</c> a point of its strength more, which breaks it: what is left goes on, so
    /// walls soak the blast up and a strong one spares what lies behind it. It goes through open cells and breakable walls only; a
    /// door, window, ladder, machine or a wall that never breaks stops it. A back wall behind a cell it reached breaks when what is
    /// left there pays for its strength, spending nothing. A wall that holds the meteor stops it whole. All found, then broken in
    /// cell order.
    /// </summary>
    private void Strike(Meteor meteor, Meteors.Shower shower)
    {
        if (HitProblem(meteor.Cell) != null || meteor.Power < _grid.WallAt(meteor.Cell)!.Armour) return;
        int charge = Charge(meteor.Power, shower), most = Farthest(meteor.Power, shower), side = 2 * most + 1;
        int width = _grid.Width, cx = meteor.Cell % width, cy = meteor.Cell / width;
        // What reaching each cell of the box round the strike spent, cheapest found; past the charge, not reached.
        if (_spent.Length < side * side) _spent = new int[side * side];
        Array.Fill(_spent, int.MaxValue, 0, side * side);
        if (_byCost.Length < charge + 1) _byCost = [.. Enumerable.Range(0, charge + 1).Select(_ => new List<int>())];
        foreach (var bucket in _byCost) bucket.Clear();
        int Box(int cell) => (cell / width - cy + most) * side + cell % width - cx + most;
        int first = Meteors.PerStrength * _grid.WallAt(meteor.Cell)!.Armour;
        _spent[Box(meteor.Cell)] = first;
        _byCost[first].Add(meteor.Cell);
        _blast.Clear();
        _backs.Clear();
        for (int cost = first; cost <= charge; cost++)
            for (int i = 0; i < _byCost[cost].Count; i++)
            {
                int cell = _byCost[cost][i];
                if (_spent[Box(cell)] != cost) continue;
                if (_grid.WallAt(cell) != null) _blast.Add(cell);
                if (_grid.BackAt(cell) is { } back && back.Armour != Material.Unbreakable
                    && charge - cost >= Meteors.PerStrength * back.Armour) _backs.Add(cell);
                int x = cell % width, y = cell / width;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (dx == 0 && dy == 0 || nx < 0 || ny < 0 || nx >= width || ny >= _grid.Height) continue;
                        if (Math.Abs(nx - cx) > most || Math.Abs(ny - cy) > most) continue;
                        int next = ny * width + nx, to = cost + (dx != 0 && dy != 0 ? Meteors.Diagonal : Meteors.Step);
                        if (_grid.WallAt(next) != null)
                        {
                            if (HitProblem(next) != null) continue;
                            to += Meteors.PerStrength * _grid.WallAt(next)!.Armour;
                        }
                        else if (_grid.StructureAt(next) != Structure.Empty || _grid.MachineAt(next) != null) continue;
                        if (to > charge || to >= _spent[Box(next)]) continue;
                        _spent[Box(next)] = to;
                        _byCost[to].Add(next);
                    }
            }
        _blast.Sort();
        _backs.Sort();
        foreach (int cell in _blast) Break(cell);
        foreach (int cell in _backs) _ops.Clear(cell, "back");
    }

    /// <summary>
    /// The wall at <paramref name="cell"/> broken (2026-10-29-holes-not-planned): lost, refunding nothing, and left unplanned, for the
    /// player to patch with what they have. Pipes are spans, running through it untouched (0109); a mark on it is dropped by building.
    /// </summary>
    private void Break(int cell) => _ops.Clear(cell, "structure");

    /// <summary>The blast a meteor of <paramref name="power"/> sets off: what it may spend reaching cells and breaking walls.</summary>
    public static int Charge(int power, Meteors.Shower shower) => shower.Charge + power * shower.PerPower;

    /// <summary>How far, in cells either way, a meteor of <paramref name="power"/>'s blast may reach: its charge a step a cell, the cheapest.</summary>
    public static int Farthest(int power, Meteors.Shower shower) => Charge(power, shower) / Meteors.Step;

    /// <summary>Ticks from one meteor to the next seen coming, drawn evenly between the shower's least and most.</summary>
    private int Interval(Meteors.Shower shower) => shower.EveryMin + (int)Random.Below((uint)(shower.EveryMax - shower.EveryMin + 1));

    /// <summary>A meteor's power, drawn by the shower's weights.</summary>
    private int PowerOf(Meteors.Shower shower)
    {
        int total = 0;
        foreach (var (_, weight) in shower.Powers) total += weight;
        int roll = (int)Random.Below((uint)total);
        foreach (var (power, weight) in shower.Powers)
        {
            if (roll < weight) return power;
            roll -= weight;
        }
        throw new InvalidOperationException("A roll below the total weight falls on a power.");
    }

    /// <summary>
    /// The walls open to the drift, top row first: in each row of what is built, inside the zone, the first thing met walking
    /// left from the right, when it is a wall that breaks. A door, window, ladder or machine met first shields its row.
    /// </summary>
    private void Targets()
    {
        _targets.Clear();
        var built = _grid.Built;
        if (built == Box.None) return;
        var zone = _grid.Zone;
        int width = _grid.Width;
        int top = Math.Max(built.Top, zone.Top), bottom = Math.Min(built.Bottom, zone.Bottom);
        int left = Math.Max(built.Left, zone.Left), right = Math.Min(built.Right, zone.Right);
        for (int y = top; y <= bottom; y++)
            for (int x = right; x >= left; x--)
            {
                int cell = y * width + x;
                if (_grid.StructureAt(cell) == Structure.Empty && _grid.MachineAt(cell) == null) continue;
                if (HitProblem(cell) == null) _targets.Add(cell);
                break;
            }
    }

    protected override HazardsState Init(Layout layout) => new() { Calm = !layout.Meteors };

    /// <summary>
    /// Meteors by hand, rarely: every hit opens the world a little more. Mostly at a standing wall, now
    /// and then anywhere: off the grid, into space, at a wall already gone.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(64) != 0) return null;
        var structure = _grid.StructureLayer;
        var walls = new List<int>();
        for (int c = 0; c < structure.Length; c++) if (Structures.Is(structure[c], Trait.Terrain)) walls.Add(c);
        int cell = rng.Below(3) != 0 && walls.Count > 0
            ? walls[(int)rng.Below((uint)walls.Count)]
            : (int)rng.Below((uint)structure.Length + 2) - 1;
        return new MeteorHit(cell) { From = from };
    }
}
