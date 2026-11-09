using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;

namespace Stillwake.Sim.Hazards;

/// <summary>A meteor seen coming (2026-10-28-hull-meteors): it strikes <c>Cell</c> with <c>Power</c> once <c>Left</c> more ticks have run.</summary>
public sealed record Meteor(int Cell, int Power, int Left);

/// <summary>
/// The hazards slice. Public so modules can be typed by it, its members internal: others read it through
/// <see cref="HazardsModule"/>'s queries.
/// </summary>
public sealed class HazardsState
{
    /// <summary>
    /// No meteors, no flares drawn: a world whose map did not ask for hazards (<c>Layout.Meteors</c>), a test's, a dev world's, or one
    /// calmed from the dev panel (<see cref="SetCalm"/>). Off, not written.
    /// </summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal bool Calm { get; set; }
    /// <summary>Ticks till the next meteor is seen coming; 0 before the first is drawn.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int NextIn { get; set; }
    /// <summary>The meteor seen coming, if one is.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal Meteor? Incoming { get; set; }
    /// <summary>Ticks till the magnetic flare drawn strikes, its warning the last of them; 0 while none is drawn.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int FlareIn { get; set; }
}

/// <summary>
/// A meteor hits the wall at <paramref name="Cell"/>: from the dev panel, to try a breach and its repair. With no
/// <paramref name="Power"/> it breaks that wall alone, always, with just what that takes, so its back wall stands but behind a deep
/// crack; with one it strikes as a meteor of the shower falling here would.
/// </summary>
public sealed record MeteorHit(int Cell, int? Power = null) : Command;

/// <summary>
/// The world calmed, or stirred again, from the dev panel: <c>On</c>, no meteors fall and no flares are drawn till it is turned off.
/// Either way the meteor seen coming and the flare drawn are dropped and the countdowns start over, as in a new world.
/// </summary>
public sealed record SetCalm(bool On) : Command;

/// <summary>Meteors breaking the hull (packages/sim/src/modules/hazards; 2026-10-28-hull-meteors), magnetic flares breaking machines (2026-10-30-magnetic-flare).</summary>
public sealed partial class HazardsModule : Module<HazardsState>
{
    private readonly IGridQueries _grid;
    private readonly IPowerQueries _power;
    private readonly IPowerOps _powerOps;
    private readonly IResearchQueries _research;
    private readonly IBuildingOps _building;
    private readonly List<int> _targets = [];

    public override string Id => "hazards";

    public HazardsModule(IGridQueries grid, IPowerQueries power, IPowerOps powerOps, IResearchQueries research, IBuildingOps building)
    {
        _grid = grid;
        _power = power;
        _powerOps = powerOps;
        _research = research;
        _building = building;
        Handles<MeteorHit>(c => HitProblem(c.Cell), c =>
        {
            if (c.Power is int power) Strike(c.Cell, power, Shower ?? Meteors.StartingSpace);
            else _building.Burst(FloorStack.Nobody, c.Cell, _grid.WallAt(c.Cell)!.Strength, 0, Spoils.Lost);
        });
        Handles<FlareNow>(FlareNowProblem, c => State.FlareIn = c.Ticks);
        Handles<SetCalm>(_ => null, c => (State.Calm, State.Incoming, State.NextIn, State.FlareIn) = (c.On, null, 0, 0));
        // In construction, as a command would: building drops a mark on the broken wall and the rooms see the hole on its tick.
        System(Phase.Construction, Fall);
        // A flare too, so Power's derivation sees what it broke on its tick.
        System(Phase.Construction, Flare);
    }

    /// <summary>Whether no meteors fall and no flares are drawn: a calm map, or calmed from the dev panel.</summary>
    public bool Calm => State.Calm;

    /// <summary>The meteor seen coming, for its streak; null while none is.</summary>
    public Meteor? Incoming => State.Incoming;

    /// <summary>
    /// The shower falling here now (2026-10-29-weak-meteors-till-landing): the surface's weaker one till a planet has been opened, its
    /// own after; null for a surface without meteors.
    /// </summary>
    public Meteors.Shower? Shower => Rules.Meteors is not { } shower ? null
        : Surfaces?.Landed != false ? shower : Rules.MeteorsBeforeLanding ?? shower;

    /// <summary>
    /// Why a meteor at <paramref name="cell"/> breaks nothing: no wall there (<c>hazards.noWall</c>), or one that never breaks, bedrock
    /// or lava, or in a planet's fog, out of the grid's zone (<c>hazards.neverBreaks</c>).
    /// </summary>
    private string? HitProblem(int cell)
    {
        if (_grid.WallAt(cell) is not { } wall) return "hazards.noWall";
        bool zone = _grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width);
        return wall.Strength == Material.Unbreakable || !zone ? "hazards.neverBreaks" : null;
    }

    /// <summary>
    /// A tick of the place's shower: the meteor seen coming strikes when its time is up (<see cref="Strike"/>); else, when the next
    /// is due, one is drawn at a wall open to the drift.
    /// </summary>
    private void Fall()
    {
        if (State.Calm || Shower is not { } shower) return;
        var s = State;
        if (s.Incoming is { } meteor)
        {
            if (meteor.Left > 1)
            {
                s.Incoming = meteor with { Left = meteor.Left - 1 };
                return;
            }
            s.Incoming = null;
            Strike(meteor.Cell, meteor.Power, shower);
            s.NextIn = Interval(shower);
            return;
        }
        if (s.NextIn == 0) s.NextIn = shower.First > 0 ? shower.First : Interval(shower);
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
    /// A meteor of <paramref name="power"/> strikes the wall at <paramref name="cell"/> (unified-damage): a burst of <see cref="Charge"/>
    /// from it within the shower's <c>Reach</c>, what it pays for lost, its back walls too, and what it reaches but cannot pay for,
    /// the wall struck first, cracked. A wall gone since it was seen coming, or one that never breaks, takes nothing.
    /// </summary>
    private void Strike(int cell, int power, Meteors.Shower shower)
    {
        if (HitProblem(cell) == null) _building.Burst(FloorStack.Nobody, cell, Charge(power, shower), shower.Reach, Spoils.Lost);
    }

    /// <summary>The burst a meteor of <paramref name="power"/> sets off, in tenths of strength.</summary>
    public static int Charge(int power, Meteors.Shower shower) => shower.Charge + power * shower.PerPower;

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
    /// left from the right, when it is a wall that breaks. A door, window, ladder or machine met first shields its row, but a
    /// debris net, which the aim passes to the wall it hangs on.
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
        var vessel = _grid.Reserved;
        for (int y = top; y <= bottom; y++)
            for (int x = right; x >= left; x--)
            {
                int cell = y * width + x;
                // A vessel ahead is no shield (generated-vessels): meteors aim at the ship past it.
                if (vessel is { } box && box.Contains(x, y)) continue;
                if (_grid.StructureAt(cell) == Structure.Empty
                    && (_grid.MachineAt(cell) is not { } m || Machines.Is(m.Kind, MachineTrait.Catches))) continue;
                if (HitProblem(cell) == null) _targets.Add(cell);
                break;
            }
    }

    protected override HazardsState Init(Layout layout) => new() { Calm = !layout.Meteors };

    /// <summary>
    /// Meteors by hand, rarely: every hit cracks or opens the world a little more. Mostly at a standing wall, now
    /// and then anywhere: off the grid, into space, at a wall already gone. As rarely a flare striking at once, so the
    /// suites' short runs see machines broken, and the world calmed or stirred.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        ulong roll = rng.Below(64);
        if (roll == 1) return new FlareNow(1) { From = from };
        if (roll == 2) return new SetCalm(rng.Below(2) == 0) { From = from };
        if (roll != 0) return null;
        var structure = _grid.StructureLayer;
        var walls = new List<int>();
        for (int c = 0; c < structure.Length; c++) if (Structures.Is(structure[c], Trait.Terrain)) walls.Add(c);
        int cell = rng.Below(3) != 0 && walls.Count > 0
            ? walls[(int)rng.Below((uint)walls.Count)]
            : (int)rng.Below((uint)structure.Length + 2) - 1;
        // Half of them a meteor's burst, of any power the showers draw or none, so a crack and a breach both come.
        return new MeteorHit(cell, rng.Below(2) == 0 ? null : (int)rng.Below(8)) { From = from };
    }
}
