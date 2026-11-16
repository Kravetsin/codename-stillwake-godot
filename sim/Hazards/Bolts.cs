using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using BoltRule = Stillwake.Content.Stretches.BoltRule;
using Span = Stillwake.Content.Stretches.Span;

namespace Stillwake.Sim.Hazards;

/// <summary>
/// A bolt of lightning glowing over <c>Cell</c> (docs/plans/surface-storms.md): it strikes once <c>Left</c> more ticks have run, every
/// hero under the open sky within <c>Spread</c> columns of it shocked.
/// </summary>
public sealed record Strike(int Cell, int Left, int Spread);

/// <summary>
/// A bolt aimed near the sender's hero now, from the dev panel, storm or not: by the running storm's rule, else the first of this
/// surface's kind. Refused <c>hazards.noBolt</c> where no stretch of the kind has a bolt, <c>hazards.boltGlowing</c> while one glows,
/// <c>hazards.noHero</c> with no hero of the sender here. With nothing to strike near the hero, nothing glows.
/// </summary>
public sealed record BoltNow : Command;

/// <summary>
/// A storm's one bolt of lightning (docs/plans/surface-storms.md): drawn as it starts, aimed near one hero drawn, glowing over the
/// highest open-sky wall cell there, then striking it: a crater and a shock to every hero under the open sky in its zone.
/// </summary>
public sealed partial class HazardsModule
{
    /// <summary>Salts of the bolts' forks of the world's Rng: one, then one per surface and one per fork drawn; odd, apart from the sky's.</summary>
    private const uint BoltSalt = 0xb01d_5a17, HereSalt = 0x27d4_eb2f, DrawnSalt = 0x1656_67b1;

    private readonly List<int> _drawable = [];

    /// <summary>The bolt glowing, its cell, ticks left and zone, for the client's glow; null while none glows.</summary>
    public Strike? Strike => State.Strike;

    /// <summary>The bolt of the storm running here, else the first of this surface's kind; null where the kind has none.</summary>
    private BoltRule? BoltRules
    {
        get
        {
            if (_sky.Now.Stretch is { } id && Rules.StretchOf(id)?.Bolt is { } running) return running;
            foreach (var stretch in Rules.Stretches)
                if (stretch.Bolt is { } bolt) return bolt;
            return null;
        }
    }

    /// <summary>
    /// A tick of the bolts: one glowing counts down and strikes as it reaches 0, calm or not. Then, while a storm with a bolt runs and
    /// the world is not calm: on its first tick the wait drawn; counted down while nothing glows, and at 0 a hero drawn and the bolt
    /// aimed near it. With no such storm running, a wait drawn is dropped.
    /// </summary>
    private void Bolt()
    {
        var s = State;
        if (s.Strike is { } strike)
        {
            if (strike.Left > 1) s.Strike = strike with { Left = strike.Left - 1 };
            else
            {
                s.Strike = null;
                StrikeBolt(strike);
            }
        }
        var now = _sky.Now;
        if (now.Stretch is not { } id || Rules.StretchOf(id)?.Bolt is not { } bolt)
        {
            s.BoltIn = 0;
            return;
        }
        if (s.Calm) return;
        if (now.Ran == 0)
        {
            s.BoltIn = Draw(bolt.At);
            return;
        }
        if (s.BoltIn == 0 || s.Strike != null || --s.BoltIn > 0) return;
        Aim(bolt);
    }

    /// <summary>
    /// One hero drawn from those here awake on the grid, in id order, and the bolt aimed near it (<see cref="Target"/>). No hero, nothing
    /// drawn; nothing to strike near it, no bolt this storm.
    /// </summary>
    private void Aim(BoltRule bolt)
    {
        int size = _grid.Width * _grid.Height;
        _drawable.Clear();
        foreach (var hero in _crew.Heroes)
            if (hero.Out == 0 && hero.Cell >= 0 && hero.Cell < size) _drawable.Add(hero.Cell);
        if (_drawable.Count == 0) return;
        int near = _drawable[(int)Fork().Below((uint)_drawable.Count)];
        if (Target(near, bolt.Columns) is int cell) State.Strike = new Strike(cell, bolt.Glow, bolt.Spread);
    }

    /// <summary>
    /// The bolt strikes: a burst of the rule's <c>Power</c> from its cell within <c>Reach</c>, nobody's, so in full, what breaks lost; then a
    /// shock of <c>Shock</c> to each hero here, in id order, within <c>Spread</c> columns of it (no wrap) and under the open sky as it stands
    /// after the burst. Crew's <c>Shock</c> spares one out or creative.
    /// </summary>
    private void StrikeBolt(Strike strike)
    {
        if (BoltRules is not { } rule) return;
        _building.Burst(FloorStack.Nobody, strike.Cell, rule.Power, rule.Reach, Spoils.Lost);
        int width = _grid.Width, x = strike.Cell % width;
        var heroes = _crew.Heroes;
        for (int i = 0; i < heroes.Count; i++)
        {
            var hero = heroes[i];
            if (_crew.OpenSky(hero.Cell) && Math.Abs(hero.Cell % width - x) <= strike.Spread) _crewOps.Shock(hero.Id, rule.Shock, Harm.Lightning);
        }
    }

    /// <summary>
    /// Where a bolt strikes near <paramref name="near"/>: of the columns within <paramref name="columns"/> of its, inside the grid's zone, each
    /// one's top wall cell under nothing but open sky (<see cref="TopOf"/>); the highest, ties to the column nearest, then the left one.
    /// Null for none.
    /// </summary>
    private int? Target(int near, int columns)
    {
        int width = _grid.Width;
        if (near < 0 || near >= width * _grid.Height) return null;
        var zone = _grid.Zone;
        int hx = near % width;
        int? best = null;
        int bestRow = int.MaxValue, bestOff = int.MaxValue;
        for (int x = Math.Max(zone.Left, hx - columns); x <= Math.Min(zone.Right, hx + columns); x++)
        {
            if (TopOf(x) is not int cell) continue;
            int row = cell / width, off = Math.Abs(x - hx);
            if (row < bestRow || row == bestRow && off < bestOff) (best, bestRow, bestOff) = (cell, row, off);
        }
        return best;
    }

    /// <summary>
    /// Column <paramref name="x"/>'s topmost wall cell, built or natural, when every cell above it is open sky and it is inside the zone;
    /// null when a back wall or a cave's roof comes first, or no wall.
    /// </summary>
    private int? TopOf(int x)
    {
        int width = _grid.Width, height = _grid.Height;
        for (int y = 0; y < height; y++)
        {
            int cell = y * width + x;
            if (_grid.WallAt(cell) != null) return _grid.Zone.Contains(x, y) ? cell : null;
            if (!_crew.OpenSky(cell)) return null;
        }
        return null;
    }

    private string? BoltNowProblem(BoltNow c)
    {
        if (BoltRules == null) return "hazards.noBolt";
        if (State.Strike != null) return "hazards.boltGlowing";
        return _crew.HeroOf(c.From) == null ? "hazards.noHero" : null;
    }

    private void BringBolt(BoltNow c)
    {
        var rule = BoltRules!;
        if (Target(_crew.HeroOf(c.From)!.Cell, rule.Columns) is int cell) State.Strike = new Strike(cell, rule.Glow, rule.Spread);
    }

    /// <summary>A number of ticks in <paramref name="span"/>, both ends included, drawn by a fork.</summary>
    private int Draw(Span span) => span.Least + (int)Fork().Below((uint)(span.Most - span.Least + 1));

    /// <summary>
    /// A fork of the world's Rng for one draw, salted by this surface and the forks drawn before, as the sky's: the world's own stream
    /// never moves for a bolt, so no draw after one does.
    /// </summary>
    private Rng Fork()
    {
        unchecked
        {
            uint salt = BoltSalt ^ (uint)Surfaces.Here * HereSalt ^ (uint)State.Drawn * DrawnSalt;
            State.Drawn++;
            return Random.Fork(salt);
        }
    }
}
