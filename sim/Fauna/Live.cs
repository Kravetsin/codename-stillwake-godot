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
        if (c.ModeLeft > 0) c.ModeLeft -= 1;
        if (c.ModeLeft == 0) Next(c, spec);
        bool grounded = Grounded(spec, c);
        if (grounded && c.Mode is Mode.Wander or Mode.Flee)
        {
            int way = c.VX >= 0 ? 1 : -1, speed = c.Mode == Mode.Flee ? spec.Flee : spec.Wander;
            if (Blocked(spec, c, way, speed))
            {
                // Over the wall or across the gap where a jump lands it on the far side; else back.
                if (JumpFor(spec, c, way, speed) is int jump)
                {
                    (c.VX, c.VY) = (way * Math.Min(speed, spec.JumpAcross), -jump);
                    Move(c, spec, grounded: false);
                    return;
                }
                way = -way;
            }
            c.VX = way * speed;
        }
        else if (grounded && c.Mode == Mode.Idle) c.VX = 0;
        Move(c, spec, grounded);
    }

    /// <summary>
    /// The mode after <paramref name="c"/>'s ends: a flinch turns to flight away from the nearest hero; flight and a wander to
    /// idling; idling to a wander either way. Only an idle or a wander begun draws: its length, and a wander its way.
    /// </summary>
    private void Next(Creature c, Spec.Species spec)
    {
        switch (c.Mode)
        {
            case Mode.Hurt:
                c.Mode = Mode.Flee;
                c.ModeLeft = spec.FleeTicks;
                c.VX = FleeWay(c) * spec.Flee;
                break;
            case Mode.Idle:
                c.Mode = Mode.Wander;
                int way = Random.Below(2) == 0 ? 1 : -1;
                c.ModeLeft = Spec.WanderLeast + (int)Random.Below(Spec.WanderMost - Spec.WanderLeast + 1);
                c.VX = way * spec.Wander;
                break;
            default:
                c.Mode = Mode.Idle;
                c.ModeLeft = Spec.IdleLeast + (int)Random.Below(Spec.IdleMost - Spec.IdleLeast + 1);
                break;
        }
    }

    /// <summary>
    /// Away from the hero nearest <paramref name="c"/> by box distance in cells, the first in id order at a tie; level with it, or
    /// with none, the way it was knocked.
    /// </summary>
    private int FleeWay(Creature c)
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
        if (nearest is { } hero && c.X != hero.X * Unit + Unit / 2) return Math.Sign(c.X - (hero.X * Unit + Unit / 2));
        return c.VX >= 0 ? 1 : -1;
    }

    /// <summary>Whether something solid is right under <paramref name="c"/>'s feet: it stands.</summary>
    private bool Grounded(Spec.Species spec, Creature c) => c.VY >= 0 && Overlaps(spec, c.X, c.Y + 1);

    /// <summary>
    /// Whether a walker standing as <paramref name="c"/> stands must turn from <paramref name="way"/>: the grid's edge, a wall in the
    /// column ahead it cannot step up (more than a cell high, or no room over its body to rise into), or no floor there within
    /// <c>Fauna.Drop</c> rows under its feet; or such a drop in the column its leading edge reaches going <paramref name="speed"/>
    /// this tick, so a fleeing one turns at the brink rather than past it. A wall is met flush as it moves.
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
        return reached != column && reached >= 0 && reached < _grid.Width && !Solid(column, feet - 1) && !Solid(reached, feet - 1)
            && Falls(reached, feet);
    }

    /// <summary>Whether no floor lies in <paramref name="column"/> within <c>Fauna.Drop</c> rows from <paramref name="feet"/> down.</summary>
    private bool Falls(int column, int feet)
    {
        for (int row = feet; row < feet + Spec.Drop; row++)
            if (Solid(column, row)) return false;
        return true;
    }

    /// <summary>Whether the column just ahead of <paramref name="c"/>'s leading edge turns it back: a wall it cannot step up, or a drop.</summary>
    private bool Ahead(Spec.Species spec, Creature c, int column)
    {
        int half = spec.Width * Unit / 2;
        int feet = c.Y / Unit, top = feet - spec.Height;
        if (Solid(column, feet - 1))
        {
            // Up one cell: the column ahead free of the body raised, and the row over the body free where it stands.
            for (int row = top - 1; row < feet - 1; row++)
                if (Solid(column, row)) return true;
            for (int x = Down(c.X - half, Unit); x <= Down(c.X + half - 1, Unit); x++)
                if (Solid(x, top - 1)) return true;
            return false;
        }
        for (int row = top; row < feet - 1; row++)
            if (Solid(column, row)) return true;
        return Falls(column, feet);
    }

    /// <summary>
    /// <paramref name="c"/> moved a tick: falling by the surface's gravity, across by its speed, stepping up a cell onto a ledge it
    /// walks into on the ground and, walking, down a cell off one; stopped flush against what is solid, turned back by a wall while
    /// it walks, landed or hitting its head.
    /// </summary>
    private void Move(Creature c, Spec.Species spec, bool grounded)
    {
        c.VY = Math.Min(c.VY + Rules.Fall(Spec.Gravity), Spec.MaxFall);
        if (c.VX != 0)
        {
            int to = c.X + c.VX, half = spec.Width * Unit / 2;
            if (!Overlaps(spec, to, c.Y)) c.X = to;
            else if (grounded && !Overlaps(spec, to, c.Y - Unit)) (c.X, c.Y) = (to, c.Y - Unit);
            else
            {
                // Flush against the column it would enter: a tick moves less than a cell, so it is the next one. Walking it keeps
                // its way, its steering next tick jumping or turning; in the air it presses on, so a leap carries it over once clear.
                c.X = Flush(to, c.VX, half);
                if (c.Mode is not (Mode.Wander or Mode.Flee)) c.VX = 0;
            }
            // Down a step one cell deep as it walks, as up one: a slope's run of steps is walked, not fallen down.
            if (grounded && c.Mode is Mode.Wander or Mode.Flee && !Overlaps(spec, c.X, c.Y + 1)
                && !Overlaps(spec, c.X, c.Y + Unit) && Overlaps(spec, c.X, c.Y + Unit + 1))
                (c.Y, c.VY) = (c.Y + Unit, 0);
        }
        int down = c.Y + c.VY;
        if (!Overlaps(spec, c.X, down)) c.Y = down;
        else if (c.VY > 0)
        {
            c.Y = Down(down - 1, Unit) * Unit;
            c.VY = 0;
        }
        else
        {
            c.Y = (Down(down - spec.Height * Unit, Unit) + 1 + spec.Height) * Unit;
            c.VY = 0;
        }
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
    /// What stops a creature: a wall (its shape whole: a slope is stood on at its top), a floor or a closed door, as for a stack, or
    /// past the grid's edge. A machine is not, as for a stack.
    /// </summary>
    private bool Solid(int x, int y) =>
        x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height
        || _grid.StructureAt(y * _grid.Width + x) is Structure.Wall or Structure.Floor or Structure.DoorClosed;
}
