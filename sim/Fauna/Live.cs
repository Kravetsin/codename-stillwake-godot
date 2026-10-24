using Stillwake.Content;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Fauna;

/// <summary>
/// The fauna's system (0102): each creature, in id order, kept or gone by where the heroes are, then its mode's clock, its
/// steering and its move; then, on the beat, a spawn round each hero that has room.
/// </summary>
public sealed partial class FaunaModule
{
    /// <summary>Where each hero on the grid is this tick, by its reported cell, in id order; refilled every tick.</summary>
    private readonly List<(int X, int Y)> _heroes = [];

    /// <summary>The habitats of this surface's type: none aboard, on a drawn map, or on a lifeless type.</summary>
    private IReadOnlyList<Spec.Habitat> Habitats => Rules is PlanetType type ? type.Fauna : [];

    private void Live()
    {
        var s = State;
        // A surface with no life and none let out runs nothing and draws nothing: the ship's slice never changes.
        if (s.Creatures.Count == 0 && Habitats.Count == 0) return;
        s.Clock += 1;
        FindHeroes();
        var all = s.Creatures;
        int kept = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var creature = all[i];
            if (!Kept(creature)) continue;
            Act(creature, Spec.Get(creature.Species));
            all[kept++] = creature;
        }
        all.RemoveRange(kept, all.Count - kept);
        if (Habitats.Count > 0 && s.Clock % Spec.Beat == 0) Spawn();
    }

    /// <summary>Every hero's reported cell, in id order, as a column and row; one off the grid, which only the fuzz holds, is nowhere.</summary>
    private void FindHeroes()
    {
        _heroes.Clear();
        int width = _grid.Width, cells = width * _grid.Height;
        var heroes = _crew.Heroes;
        for (int i = 0; i < heroes.Count; i++)
        {
            int cell = heroes[i].Cell;
            if (cell >= 0 && cell < cells) _heroes.Add((cell % width, cell / width));
        }
    }

    private static bool Within(int x, int y, (int X, int Y) at, HalfBox box) => Math.Abs(x - at.X) <= box.X && Math.Abs(y - at.Y) <= box.Y;

    /// <summary>
    /// Whether <paramref name="c"/> stays, by the cell under its middle: in some hero's keep box, seen there this tick; else within
    /// some hero's hard box and seen less than <c>Fauna.Linger</c> ago. With no hero on the surface, none stays.
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
    /// A tick of <paramref name="c"/>: pushed up a cell, still, while something built since stands in its body; else its mode's
    /// clock, then, walking on the ground, its way turned at a wall it cannot step up or a drop too deep, then its move.
    /// </summary>
    private void Act(Creature c, Spec.Species spec)
    {
        if (Overlaps(spec, c.X, c.Y))
        {
            if (c.Y - (spec.Height + 1) * Unit >= 0) c.Y -= Unit;
            (c.VX, c.VY) = (0, 0);
            return;
        }
        // A wind-up begun in the air, a retreat's knock still flying, counts from the ground: its crouch is seen whole.
        if (c.ModeLeft > 0 && !(c.Mode == Mode.WindUp && !Grounded(spec, c))) c.ModeLeft -= 1;
        if (c.ModeLeft == 0) Next(c, spec);
        bool grounded = Grounded(spec, c);
        if (grounded && (c.Mode == Mode.Wander || Fleeing(c.Mode)))
        {
            int way = c.VX >= 0 ? 1 : -1, speed = Fleeing(c.Mode) ? spec.Flee : spec.Wander;
            if (Blocked(spec, c, way, speed))
            {
                // Over the wall or across the gap where a jump lands it on the far side; else back, or it rams: at once when it
                // was to turn on the hero anyway, cornered by a draw, and never once its ram is spent.
                if (JumpFor(spec, c, way, speed) is int jump)
                {
                    (c.VX, c.VY) = (way * Math.Min(speed, spec.JumpAcross), -jump);
                    Move(c, spec, grounded: false);
                    return;
                }
                if (c.Mode == Mode.Retreat || c.Mode == Mode.Flee && Cornered(spec, c, way) && Random.Below(1000) < (uint)spec.RamChance)
                {
                    (c.Mode, c.ModeLeft, c.VX) = (Mode.WindUp, Spec.WindUp, 0);
                    Move(c, spec, grounded);
                    return;
                }
                way = -way;
            }
            c.VX = way * speed;
        }
        else if (grounded && c.Mode == Mode.Charge)
        {
            // A wall it cannot step up or a drop ends the charge, never leaped: it turns from it, as a walker turns. Past the hero
            // it charged by RamPast cells it runs on. Either way it escapes, its ram spent.
            int way = c.VX >= 0 ? 1 : -1;
            if (Blocked(spec, c, way, Spec.Charge)) Flees(c, spec, Mode.Escape, -way);
            else if (Nearest(c).Hero is { } hero && (c.X - (hero.X * Unit + Unit / 2)) * way >= Spec.RamPast * Unit) Flees(c, spec, Mode.Escape, way);
            else c.VX = way * Spec.Charge;
        }
        else if (grounded && c.Mode is Mode.Idle or Mode.WindUp) c.VX = 0;
        Move(c, spec, grounded);
    }

    /// <summary>Whether <paramref name="mode"/> is a flight: steered as one, at the species' flee, turned and leaping alike.</summary>
    public static bool Fleeing(Mode mode) => mode is Mode.Flee or Mode.Retreat or Mode.Escape;

    /// <summary>Whether <paramref name="mode"/> walks its way: a wander, a flight or a charge, kept against a wall till its steering turns it.</summary>
    private static bool Walks(Mode mode) => mode is Mode.Wander or Mode.Charge || Fleeing(mode);

    /// <summary>
    /// Whether <paramref name="c"/>, turning from <paramref name="way"/>, is cornered (0103): a species that rams, the nearest hero
    /// within <c>Fauna.RamReach</c> cells by box distance, and on the side it would turn to.
    /// </summary>
    private bool Cornered(Spec.Species spec, Creature c, int way) =>
        spec.Ram > 0 && Nearest(c) is ({ } hero, int d) && d <= Spec.RamReach && Math.Sign(hero.X * Unit + Unit / 2 - c.X) == -way;

    /// <summary>
    /// The mode after <paramref name="c"/>'s ends: a flinch turns to flight away from the nearest hero; flight and a wander to
    /// idling; idling to a wander either way; a retreat to a wind-up, a wind-up to a charge at the nearest hero, a charge to an
    /// escape. Only an idle or a wander begun draws: its length, and a wander its way.
    /// </summary>
    private void Next(Creature c, Spec.Species spec)
    {
        switch (c.Mode)
        {
            case Mode.Hurt:
                Flees(c, spec, Mode.Flee, FleeWay(c));
                break;
            case Mode.Charge:
                Flees(c, spec, Mode.Escape, FleeWay(c));
                break;
            case Mode.Retreat:
                (c.Mode, c.ModeLeft, c.VX) = (Mode.WindUp, Spec.WindUp, 0);
                break;
            case Mode.WindUp:
                // At the hero nearest now, by its reported cell: none, level with it or past a charge's run, nothing to charge at.
                var (target, far) = Nearest(c);
                int at = target is { } hero && far <= Spec.ChargeTicks * Spec.Charge / Unit ? Math.Sign(hero.X * Unit + Unit / 2 - c.X) : 0;
                if (at == 0) Flees(c, spec, Mode.Escape, FleeWay(c));
                else (c.Mode, c.ModeLeft, c.VX) = (Mode.Charge, Spec.ChargeTicks, at * Spec.Charge);
                break;
            case Mode.Idle:
                c.Mode = Mode.Wander;
                int way = Random.Below(2) == 0 ? 1 : -1;
                c.ModeLeft = Spec.WanderLeast + (int)Random.Below(Spec.WanderMost - Spec.WanderLeast + 1);
                c.VX = way * spec.Wander;
                break;
            case Mode.Wander or Mode.Flee or Mode.Escape:
                c.Mode = Mode.Idle;
                c.ModeLeft = Spec.IdleLeast + (int)Random.Below(Spec.IdleMost - Spec.IdleLeast + 1);
                break;
            default:
                throw new InvalidOperationException($"fauna: no mode follows {c.Mode}");
        }
    }

    /// <summary><paramref name="c"/> in flight <paramref name="mode"/> for <c>FleeTicks</c>, going <paramref name="way"/> at its species' flight.</summary>
    private static void Flees(Creature c, Spec.Species spec, Mode mode, int way)
    {
        c.Mode = mode;
        c.ModeLeft = spec.FleeTicks;
        c.VX = way * spec.Flee;
    }

    /// <summary>The hero nearest <paramref name="c"/> by box distance in cells, the first in id order at a tie, and how far; none with none on the grid.</summary>
    private ((int X, int Y)? Hero, int Distance) Nearest(Creature c)
    {
        int x = Down(c.X, Unit), y = Down(c.Y - 1, Unit);
        (int X, int Y)? nearest = null;
        int best = int.MaxValue;
        foreach (var at in _heroes)
        {
            int d = Math.Max(Math.Abs(x - at.X), Math.Abs(y - at.Y));
            if (d >= best) continue;
            (best, nearest) = (d, at);
        }
        return (nearest, best);
    }

    /// <summary>
    /// Away from the hero nearest <paramref name="c"/> by box distance in cells, the first in id order at a tie; level with it, or
    /// with none, the way it was knocked.
    /// </summary>
    private int FleeWay(Creature c)
    {
        if (Nearest(c).Hero is { } hero && c.X != hero.X * Unit + Unit / 2) return Math.Sign(c.X - (hero.X * Unit + Unit / 2));
        return c.VX >= 0 ? 1 : -1;
    }

    /// <summary>Whether something solid is right under <paramref name="c"/>'s feet: it stands.</summary>
    private bool Grounded(Spec.Species spec, Creature c) => c.VY >= 0 && Overlaps(spec, c.X, c.Y + 1);

    /// <summary>
    /// Whether a walker standing as <paramref name="c"/> stands must turn from <paramref name="way"/>: the grid's edge, a wall in the
    /// column ahead it cannot step up (more than <c>Fauna.StepUp</c> cells high, or no room over its body to rise into), or no floor
    /// there within <c>Fauna.Drop</c> rows under its feet; or such a drop in a column its leading edge reaches going
    /// <paramref name="speed"/> this tick, so a fleeing one turns at the brink rather than past it. A wall is met flush as it moves.
    /// </summary>
    private bool Blocked(Spec.Species spec, Creature c, int way, int speed)
    {
        int half = spec.Width * Unit / 2;
        int column = Down(way > 0 ? c.X + half : c.X - half - 1, Unit);
        if (column < 0 || column >= _grid.Width) return true;
        if (Ahead(spec, c, column)) return true;
        int reached = Down(way > 0 ? c.X + half + speed - 1 : c.X - half - speed, Unit);
        int feet = c.Y / Unit;
        // Only over flat ground: a step up met first is climbed this tick, and a column off the grid is the edge's, met next tick.
        if (Rise(column, feet) > 0) return false;
        for (int at = column + way; at != reached + way; at += way)
        {
            if (at < 0 || at >= _grid.Width || Rise(at, feet) > 0) return false;
            if (Falls(at, feet)) return true;
        }
        return false;
    }

    /// <summary>Whether no floor lies in <paramref name="column"/> within <c>Fauna.Drop</c> rows from <paramref name="feet"/> down.</summary>
    private bool Falls(int column, int feet)
    {
        for (int row = feet; row < feet + Spec.Drop; row++)
            if (Solid(column, row)) return false;
        return true;
    }

    /// <summary>How many rows of <paramref name="column"/> over <paramref name="feet"/>, at most <c>Fauna.StepUp</c>, its highest solid one stands: a step's height, 0 for none.</summary>
    private int Rise(int column, int feet)
    {
        for (int k = Spec.StepUp; k >= 1; k--)
            if (Solid(column, feet - k)) return k;
        return 0;
    }

    /// <summary>Whether the column just ahead of <paramref name="c"/>'s leading edge turns it back: a wall it cannot step up, or a drop.</summary>
    private bool Ahead(Spec.Species spec, Creature c, int column)
    {
        int half = spec.Width * Unit / 2;
        int feet = c.Y / Unit, top = feet - spec.Height, rise = Rise(column, feet);
        if (rise > 0)
        {
            // Up the step: the column ahead free of the body raised, and the rows over the body free where it stands.
            for (int row = top - rise; row < feet - rise; row++)
                if (Solid(column, row)) return true;
            for (int x = Down(c.X - half, Unit); x <= Down(c.X + half - 1, Unit); x++)
                for (int row = top - rise; row < top; row++)
                    if (Solid(x, row)) return true;
            return false;
        }
        for (int row = top; row < feet - 1; row++)
            if (Solid(column, row)) return true;
        return Falls(column, feet);
    }

    /// <summary>
    /// <paramref name="c"/> moved a tick: falling by the surface's gravity, across by its speed, stepping up onto a ledge it walks
    /// into on the ground and, walking, down off one, each up to <c>Fauna.StepUp</c> cells; stopped flush against what is solid,
    /// turned back by a wall while it walks, landed or hitting its head. It moves in parts of under a cell, so no wall a cell
    /// thick is passed at speed (0104).
    /// </summary>
    private void Move(Creature c, Spec.Species spec, bool grounded)
    {
        c.VY = Math.Min(c.VY + Rules.Fall(Spec.Gravity), Spec.MaxFall);
        if (c.VX != 0)
        {
            int x = c.X, y = c.Y;
            bool walks = Walks(c.Mode);
            // Flush against what it met, walking it keeps its way, its steering next tick jumping or turning; in the air it presses
            // on, so a leap carries it over once clear.
            if (Across(spec, ref x, ref y, c.VX, grounded, walks, out bool down) && !walks) c.VX = 0;
            // Down a step as it walks, as up one: a stair is walked, not fallen down.
            if (down) c.VY = 0;
            (c.X, c.Y) = (x, y);
        }
        int to = c.Y;
        if (Upright(spec, c.X, ref to, c.VY)) c.VY = 0;
        c.Y = to;
    }

    /// <summary>
    /// A body of <paramref name="spec"/> at (<paramref name="x"/>, <paramref name="y"/>) moved <paramref name="vx"/> across in parts
    /// of under a cell, on the ground stepping up onto what it walks into and, <paramref name="walks"/>, down off a ledge, each up
    /// to <c>Fauna.StepUp</c> cells. Whether it met what is solid, then flush against the column it would enter; <paramref name="down"/>
    /// whether it stepped down.
    /// </summary>
    private bool Across(Spec.Species spec, ref int x, ref int y, int vx, bool grounded, bool walks, out bool down)
    {
        int half = spec.Width * Unit / 2, parts = Math.Abs(vx) / Unit + 1;
        bool met = false;
        down = false;
        for (int i = 0; i < parts && !met; i++)
        {
            int to = x + Part(vx, i, parts);
            if (!Overlaps(spec, to, y)) x = to;
            else if (grounded && StepOnto(spec, to, y) is int up) (x, y) = (to, y - up);
            else (x, met) = (Flush(to, vx, half), true);
            if (grounded && walks && StepOff(spec, x, y) is var off and > 0) (y, down) = (y + off, true);
        }
        return met;
    }

    /// <summary>
    /// A body of <paramref name="spec"/> at <paramref name="y"/> moved <paramref name="vy"/> down (up, when less than none) in parts
    /// of under a cell: whether it landed or hit its head, then flush against what stopped it.
    /// </summary>
    private bool Upright(Spec.Species spec, int x, ref int y, int vy)
    {
        int parts = Math.Abs(vy) / Unit + 1;
        for (int i = 0; i < parts; i++)
        {
            int to = y + Part(vy, i, parts);
            if (!Overlaps(spec, x, to)) y = to;
            else
            {
                y = vy > 0 ? Down(to - 1, Unit) * Unit : (Down(to - spec.Height * Unit, Unit) + 1 + spec.Height) * Unit;
                return true;
            }
        }
        return false;
    }

    /// <summary>The <paramref name="i"/>th of <paramref name="parts"/> parts of <paramref name="v"/>, whole, the parts summing to it.</summary>
    private static int Part(int v, int i, int parts) => v * (i + 1) / parts - v * i / parts;

    /// <summary>How far up, a cell at a time to <c>Fauna.StepUp</c>, a body walking into what is solid at (<paramref name="x"/>, <paramref name="y"/>) fits; null when it does not.</summary>
    private int? StepOnto(Spec.Species spec, int x, int y)
    {
        for (int k = 1; k <= Spec.StepUp; k++)
            if (!Overlaps(spec, x, y - k * Unit)) return k * Unit;
        return null;
    }

    /// <summary>
    /// How far down, a cell at a time to <c>Fauna.StepUp</c>, the ground lies under a body at (<paramref name="x"/>, <paramref name="y"/>)
    /// with none right under it; 0 for none so near, or ground right under it.
    /// </summary>
    private int StepOff(Spec.Species spec, int x, int y)
    {
        if (Overlaps(spec, x, y + 1)) return 0;
        for (int k = 1; k <= Spec.StepUp; k++)
        {
            if (Overlaps(spec, x, y + k * Unit)) return 0;
            if (Overlaps(spec, x, y + k * Unit + 1)) return k * Unit;
        }
        return 0;
    }

    /// <summary>The middle of a body <paramref name="half"/> wide either way flush against the column it would enter at <paramref name="to"/> going <paramref name="vx"/>.</summary>
    private static int Flush(int to, int vx, int half) =>
        vx > 0 ? Down(to + half - 1, Unit) * Unit - half : (Down(to - half, Unit) + 1) * Unit + half;

    /// <summary>Whether a body of <paramref name="spec"/> with its bottom middle at (<paramref name="x"/>, <paramref name="y"/>) is in anything solid or past the grid's edge.</summary>
    internal bool Overlaps(Spec.Species spec, int x, int y)
    {
        int half = spec.Width * Unit / 2;
        int left = Down(x - half, Unit), right = Down(x + half - 1, Unit);
        int top = Down(y - spec.Height * Unit, Unit), bottom = Down(y - 1, Unit);
        for (int row = top; row <= bottom; row++)
            for (int column = left; column <= right; column++)
                if (Solid(column, row)) return true;
        return false;
    }

    /// <summary>
    /// What stops a creature: a wall, a floor or a closed door, as for a stack, or
    /// past the grid's edge. A machine is not, as for a stack.
    /// </summary>
    private bool Solid(int x, int y) =>
        x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height
        || _grid.StructureAt(y * _grid.Width + x) is Structure.Wall or Structure.Floor or Structure.DoorClosed or Structure.HatchClosed;
}
