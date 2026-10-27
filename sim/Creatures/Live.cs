using Stillwake.Content;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The creatures' system (0102): the killed swept out; each creature, in id order, kept or gone by where the heroes are, then
/// its body freed, its drive or act's clock and its body's steering and move; then, on the beat, a spawn round each hero that
/// has room. The drives, acts and bodies are rules (2026-10-26-creatures-are-composed); this is what they share.
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>Where each hero on the grid is this tick, by its reported cell, in id order; refilled every tick.</summary>
    private readonly List<(int X, int Y)> _heroes = [];

    /// <summary>Whether each hero of <see cref="_heroes"/> is awake, not passed out.</summary>
    private readonly List<bool> _awake = [];

    /// <summary>The habitats of this surface's type: none aboard, on a drawn map, or on a lifeless type.</summary>
    private IReadOnlyList<Spec.Habitat> Habitats => Rules is PlanetType type ? type.Creatures : [];

    /// <summary>The world's random numbers, for the rules.</summary>
    internal Rng Rand => Random;

    internal int GridWidth => _grid.Width;

    internal int GridHeight => _grid.Height;

    /// <summary>Ticks this module has run: what a rule stamps what it did with.</summary>
    internal int Clock => State.Clock;

    /// <summary>The debris, for the rules of a creature that clings to a piece.</summary>
    internal IDebrisQueries Debris => _debris;

    internal IDebrisOps DebrisOps => _debrisOps;

    /// <summary>Peer <paramref name="peer"/>'s hero's reported cell as a column and row; null for none on the grid.</summary>
    internal (int X, int Y)? HeroAt(int peer, bool awake = false) =>
        _crew.HeroOf(peer) is { } hero && (!awake || hero.Out == 0) && hero.Cell >= 0 && hero.Cell < _grid.Width * _grid.Height
            ? (hero.Cell % _grid.Width, hero.Cell / _grid.Width) : null;

    private void Live()
    {
        var s = State;
        Linger();
        // Those killed by this tick's commands go first, as if gone the moment they died.
        s.Creatures.RemoveAll(c => c.Dead);
        if (Rules.Guards.Count > 0 && _debris.Pieces.Count > 0 && _debris.Pieces[^1].Id > s.Watermark) GuardNewPieces();
        // A surface with no life and none let out runs nothing and draws nothing: aboard, only a guarded kind drifting in draws.
        if (s.Creatures.Count == 0 && Habitats.Count == 0) return;
        s.Clock += 1;
        FindHeroes();
        var all = s.Creatures;
        int kept = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var creature = all[i];
            if (!Kept(creature)) continue;
            Behave(creature, Spec.Get(creature.Species));
            all[kept++] = creature;
        }
        all.RemoveRange(kept, all.Count - kept);
        if (Habitats.Count > 0 && s.Clock % Spec.Beat == 0) Spawn();
    }

    /// <summary>Every hero's reported cell, in id order, as a column and row; one off the grid, which only the fuzz holds, is nowhere.</summary>
    private void FindHeroes()
    {
        _heroes.Clear();
        _awake.Clear();
        int width = _grid.Width, cells = width * _grid.Height;
        var heroes = _crew.Heroes;
        for (int i = 0; i < heroes.Count; i++)
        {
            int cell = heroes[i].Cell;
            if (cell < 0 || cell >= cells) continue;
            _heroes.Add((cell % width, cell / width));
            _awake.Add(heroes[i].Out == 0);
        }
    }

    private static bool Within(int x, int y, (int X, int Y) at, HalfBox box) => Math.Abs(x - at.X) <= box.X && Math.Abs(y - at.Y) <= box.Y;

    /// <summary>
    /// Whether <paramref name="c"/> stays, by the cell under its middle: in some hero's keep box, seen there this tick; else within
    /// some hero's hard box and seen less than <c>Creatures.Linger</c> ago. With no hero on the surface, none stays.
    /// </summary>
    private bool Kept(Creature c)
    {
        int x = Down(c.X, Unit), y = Down(c.Y - 1, Unit);
        bool near = false;
        foreach (var at in _heroes)
        {
            if (Within(x, y, at, Spec.Keep))
            {
                c.SeenAt = State.Clock;
                return true;
            }
            near |= Within(x, y, at, Spec.Hard);
        }
        return near && State.Clock - c.SeenAt < Spec.Linger;
    }

    /// <summary>
    /// A tick of <paramref name="c"/>: freed by its body, else its act's clock, or the drives above the one in charge asked — one that
    /// takes over starts its clock next tick — and its drive's clock; then its body's steering and move.
    /// </summary>
    private void Behave(Creature c, Spec.Species spec)
    {
        var body = RuleBook.Body(spec.Body);
        if (body.Unstick(this, c, spec.Body)) return;
        bool grounded = body.Grounded(this, c, spec.Body);
        for (int i = 0; i < spec.Brain.Count; i++) RuleBook.Drive(spec.Brain[i]).Reflex(this, c, spec, spec.Brain[i]);
        if (c.Act is { } act)
        {
            var mv = spec.Moves[act.Move];
            RuleBook.Move(mv).Count(this, c, spec, mv, grounded);
        }
        else
        {
            var now = spec.Brain[c.Drive];
            if (FirstWanting(c, spec, below: c.Drive) is int over and >= 0) TakeOver(c, spec, over);
            else if (!RuleBook.Drive(now).Run(this, c, spec, now)) TakeOver(c, spec, Fallback(spec, c));
            // Its drive may have begun an act this tick: the act's clock starts next tick.
            else if (c.Act == null)
            {
                if (c.DriveLeft > 0) c.DriveLeft -= 1;
                var d = spec.Brain[c.Drive];
                if (c.DriveLeft == 0 && !RuleBook.Drive(d).Next(this, c, spec, d)) TakeOver(c, spec, Fallback(spec, c));
            }
        }
        body.Step(this, c, spec, grounded);
    }

    /// <summary>The first drive from the top of <paramref name="spec"/>'s brain, above <paramref name="below"/>, that wants <paramref name="c"/>; -1 for none.</summary>
    internal static int FirstWanting(Creature c, Spec.Species spec, int below)
    {
        for (int i = 0; i < below; i++)
            if (RuleBook.Drive(spec.Brain[i]).Wants(c, spec.Brain[i])) return i;
        return -1;
    }

    /// <summary>The drive that takes <paramref name="c"/> when the one in charge yields, or when it is born: the first that wants it.</summary>
    internal static int Fallback(Spec.Species spec, Creature c) =>
        FirstWanting(c, spec, spec.Brain.Count) is int at and >= 0 ? at : throw new InvalidOperationException($"creatures: nothing in {spec.Id}'s brain wants it");

    private void TakeOver(Creature c, Spec.Species spec, int at)
    {
        (c.Drive, c.Step) = (at, 0);
        RuleBook.Drive(spec.Brain[at]).Enter(this, c, spec.Brain[at]);
    }

    /// <summary>Drive <paramref name="at"/> claims <paramref name="c"/> for <paramref name="left"/> ticks.</summary>
    internal void Claim(Creature c, int at, int left) => (c.Drive, c.Step, c.DriveLeft) = (at, 0, left);

    /// <summary><paramref name="c"/> bolting <paramref name="away"/> at its run, or faster if the knock already sends it so.</summary>
    internal void Bolt(Creature c, Spec.Species spec, int away) => c.VX = away * Math.Max(Math.Abs(c.VX), ((Spec.Walker)spec.Body).Run);

    /// <summary>
    /// <paramref name="c"/>'s act ended: the drive that began it is told, <paramref name="c"/> to go <paramref name="way"/>,
    /// hopping back when <paramref name="hop"/>.
    /// </summary>
    internal void EndAct(Creature c, Spec.Species spec, int way, bool hop)
    {
        c.Act = null;
        var d = spec.Brain[c.Drive];
        RuleBook.Drive(d).Ended(this, c, spec, d, way, hop);
    }

    /// <summary>
    /// <paramref name="c"/> escaping <paramref name="way"/> at its run, its move spent (0103), hopping when <paramref name="hop"/>:
    /// recoiling off the hero it struck.
    /// </summary>
    internal void Escape(Creature c, Spec.Species spec, int way, bool hop)
    {
        int at = IndexOf<Spec.Escape>(spec);
        Claim(c, at, ((Spec.Escape)spec.Brain[at]).Ticks);
        c.VX = way * ((Spec.Walker)spec.Body).Run;
        if (hop && c.VY >= 0) c.VY = -spec.Life.Hop;
    }

    /// <summary>The place in <paramref name="spec"/>'s brain of its first drive of kind <typeparamref name="D"/>.</summary>
    internal static int IndexOf<D>(Spec.Species spec) where D : Spec.Drive
    {
        for (int i = 0; i < spec.Brain.Count; i++)
            if (spec.Brain[i] is D) return i;
        throw new InvalidOperationException($"creatures: {spec.Id} has no {typeof(D).Name}");
    }

    /// <summary>
    /// Whether <paramref name="c"/>, turning from <paramref name="way"/>, is cornered (0103): the nearest hero within
    /// <paramref name="reach"/> cells by box distance, and on the side it would turn to.
    /// </summary>
    internal bool Cornered(Creature c, int way, int reach) =>
        Nearest(c) is ({ } hero, int d) && d <= reach && Math.Sign(Middle(hero.X) - c.X) == -way;

    /// <summary>The middle of column <paramref name="x"/>, in thousandths of a cell.</summary>
    internal int Middle(int x) => x * Unit + Unit / 2;

    /// <summary>The hero nearest <paramref name="c"/> by box distance in cells, the first in id order at a tie, and how far; none with none on the grid.</summary>
    internal ((int X, int Y)? Hero, int Distance) Nearest(Creature c, bool awake = false)
    {
        int x = Down(c.X, Unit), y = Down(c.Y - 1, Unit);
        (int X, int Y)? nearest = null;
        int best = int.MaxValue;
        for (int i = 0; i < _heroes.Count; i++)
        {
            var at = _heroes[i];
            if (awake && !_awake[i]) continue;
            int d = Math.Max(Math.Abs(x - at.X), Math.Abs(y - at.Y));
            if (d >= best) continue;
            (best, nearest) = (d, at);
        }
        return (nearest, best);
    }

    /// <summary>
    /// Away from the hero nearest <paramref name="c"/> by box distance in cells, the first in id order at a tie; level with it, or
    /// with none, the way it was going.
    /// </summary>
    internal int FleeWay(Creature c)
    {
        if (Nearest(c).Hero is { } hero && c.X != Middle(hero.X)) return Math.Sign(c.X - Middle(hero.X));
        return c.VX >= 0 ? 1 : -1;
    }

    /// <summary>Whether a body of <paramref name="s"/> with its bottom middle at (<paramref name="x"/>, <paramref name="y"/>) is in anything solid or past the grid's edge.</summary>
    internal bool Overlaps(Spec.Shape s, int x, int y)
    {
        int half = s.Width * Unit / 2;
        int left = Down(x - half, Unit), right = Down(x + half - 1, Unit);
        int top = Down(y - s.Height * Unit, Unit), bottom = Down(y - 1, Unit);
        for (int row = top; row <= bottom; row++)
            for (int column = left; column <= right; column++)
                if (Solid(column, row)) return true;
        return false;
    }

    /// <summary>
    /// What stops a creature: anything solid (a wall, a floor, a shut doorway), as for a stack, or
    /// past the grid's edge; on a planet, every column outside the build zone, the fog at its sides
    /// (fog-edges), so walking, leaping, a knock or a release never carries one in. A machine is not, as for a stack.
    /// </summary>
    internal bool Solid(int cell) => Solid(cell % _grid.Width, cell / _grid.Width);

    /// <inheritdoc cref="Solid(int)"/>
    internal bool Solid(int x, int y) =>
        x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height
        || Rules is PlanetType && (x < _grid.Zone.Left || x > _grid.Zone.Right)
        || _grid.Solid(y * _grid.Width + x);
}
