using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The walker (0102, 0104, 0106): falling by the surface's gravity, walking and stepping up and down ledges, turning at walls and
/// drops, leaping where a leap lands it; steered by the drive or act in charge. Its leaps are flown through the grid as the move
/// would, tick by tick: integers only, no draw.
/// </summary>
internal sealed class WalkerBody : BodyRule
{
    /// <summary>How far over what it must clear a jump is sized to carry the body's feet, in thousandths: room to land on it.</summary>
    private const int JumpMargin = 400;

    /// <summary>The most ticks a leap is flown looking for its landing: past any jump's whole flight.</summary>
    private const int LeapTicks = 120;

    internal override Type Serves => typeof(Spec.Walker);

    // Pushed up a cell, still, while something built since stands in its body.
    internal override bool Unstick(CreaturesModule m, Creature c, Spec.Shape s)
    {
        if (!m.Overlaps(s, c.X, c.Y)) return false;
        if (c.Y - (s.Height + 1) * Unit >= 0) c.Y -= Unit;
        (c.VX, c.VY) = (0, 0);
        return true;
    }

    /// <summary>Whether something solid is right under <paramref name="c"/>'s feet: it stands.</summary>
    internal override bool Grounded(CreaturesModule m, Creature c, Spec.Shape s) => c.VY >= 0 && m.Overlaps(s, c.X, c.Y + 1);

    /// <summary>
    /// On the ground, an act steers itself; a drive that walks or runs keeps its way, turning at a wall it cannot step up or a
    /// drop too deep unless it leaps, or its drive acts instead; one standing stops. Then its move.
    /// </summary>
    internal override void Step(CreaturesModule m, Creature c, Spec.Species spec, bool grounded)
    {
        var w = (Spec.Walker)spec.Body;
        if (c.Act is { } act)
        {
            var mv = spec.Moves[act.Move];
            if (grounded) RuleBook.Move(mv).Steer(m, c, spec, mv);
            Move(m, c, w, grounded, Walks(c, spec));
            return;
        }
        var d = spec.Brain[c.Drive];
        var rule = RuleBook.Drive(d);
        var gait = rule.GaitOf(c);
        if (grounded && gait != Gait.Still)
        {
            int way = c.VX >= 0 ? 1 : -1, speed = gait == Gait.Run ? w.Run : w.Walk;
            if (Blocked(m, c, w, way, speed))
            {
                // Over the wall or across the gap where a jump lands it on the far side; else its drive acts, or it turns.
                if (JumpFor(m, c, w, way, speed, fleeing: gait == Gait.Run) is int jump)
                {
                    (c.VX, c.VY) = (way * Math.Min(speed, w.JumpAcross), -jump);
                    Move(m, c, w, grounded: false, Walks(c, spec));
                    return;
                }
                if (rule.Blocked(m, c, spec, d, way))
                {
                    Move(m, c, w, grounded, Walks(c, spec));
                    return;
                }
                way = -way;
            }
            c.VX = way * speed;
        }
        else if (grounded) c.VX = 0;
        Move(m, c, w, grounded, Walks(c, spec));
    }

    /// <summary>Whether <paramref name="c"/> walks its way now: wandering, fleeing or charging, kept against a wall till its steering turns it.</summary>
    private static bool Walks(Creature c, Spec.Species spec) =>
        c.Act is { } act ? RuleBook.Move(spec.Moves[act.Move]).Walks(act) : RuleBook.Drive(spec.Brain[c.Drive]).GaitOf(c) != Gait.Still;

    /// <summary>
    /// Whether a walker standing as <paramref name="c"/> stands must turn from <paramref name="way"/>: the grid's edge, a wall in the
    /// column ahead it cannot step up (more than <c>Creatures.StepUp</c> cells high, or no room over its body to rise into), or no floor
    /// there within <c>Creatures.Drop</c> rows under its feet; or such a drop in a column its leading edge reaches going
    /// <paramref name="speed"/> this tick, so a fleeing one turns at the brink rather than past it. A wall is met flush as it moves.
    /// </summary>
    internal static bool Blocked(CreaturesModule m, Creature c, Spec.Walker w, int way, int speed)
    {
        int half = w.Width * Unit / 2;
        int column = Down(way > 0 ? c.X + half : c.X - half - 1, Unit);
        if (column < 0 || column >= m.GridWidth) return true;
        if (Ahead(m, c, w, column)) return true;
        int reached = Down(way > 0 ? c.X + half + speed - 1 : c.X - half - speed, Unit);
        int feet = c.Y / Unit;
        // Only over flat ground: a step up met first is climbed this tick, and a column off the grid is the edge's, met next tick.
        if (Rise(m, column, feet) > 0) return false;
        for (int at = column + way; at != reached + way; at += way)
        {
            if (at < 0 || at >= m.GridWidth || Rise(m, at, feet) > 0) return false;
            if (Falls(m, at, feet)) return true;
        }
        return false;
    }

    /// <summary>Whether no floor lies in <paramref name="column"/> within <c>Creatures.Drop</c> rows from <paramref name="feet"/> down.</summary>
    private static bool Falls(CreaturesModule m, int column, int feet)
    {
        for (int row = feet; row < feet + Spec.Drop; row++)
            if (m.Solid(column, row)) return false;
        return true;
    }

    /// <summary>How many rows of <paramref name="column"/> over <paramref name="feet"/>, at most <c>Creatures.StepUp</c>, its highest solid one stands: a step's height, 0 for none.</summary>
    private static int Rise(CreaturesModule m, int column, int feet)
    {
        for (int k = Spec.StepUp; k >= 1; k--)
            if (m.Solid(column, feet - k)) return k;
        return 0;
    }

    /// <summary>Whether the column just ahead of <paramref name="c"/>'s leading edge turns it back: a wall it cannot step up, or a drop.</summary>
    private static bool Ahead(CreaturesModule m, Creature c, Spec.Walker w, int column)
    {
        int half = w.Width * Unit / 2;
        int feet = c.Y / Unit, top = feet - w.Height, rise = Rise(m, column, feet);
        if (rise > 0)
        {
            // Up the step: the column ahead free of the body raised, and the rows over the body free where it stands.
            for (int row = top - rise; row < feet - rise; row++)
                if (m.Solid(column, row)) return true;
            for (int x = Down(c.X - half, Unit); x <= Down(c.X + half - 1, Unit); x++)
                for (int row = top - rise; row < top; row++)
                    if (m.Solid(x, row)) return true;
            return false;
        }
        for (int row = top; row < feet - 1; row++)
            if (m.Solid(column, row)) return true;
        return Falls(m, column, feet);
    }

    /// <summary>
    /// <paramref name="c"/> moved a tick: falling by the surface's gravity, across by its speed, stepping up onto a ledge it walks
    /// into on the ground and, walking, down off one, each up to <c>Creatures.StepUp</c> cells; stopped flush against what is solid,
    /// turned back by a wall while it walks, landed or hitting its head. It moves in parts of under a cell, so no wall a cell
    /// thick is passed at speed (0104).
    /// </summary>
    private static void Move(CreaturesModule m, Creature c, Spec.Walker w, bool grounded, bool walks)
    {
        c.VY = Math.Min(c.VY + m.Rules.Fall(Spec.Gravity), Spec.MaxFall);
        if (c.VX != 0)
        {
            int x = c.X, y = c.Y;
            // Flush against what it met, walking it keeps its way, its steering next tick jumping or turning; in the air it presses
            // on, so a leap carries it over once clear.
            if (Across(m, w, ref x, ref y, c.VX, grounded, walks, out bool down) && !walks) c.VX = 0;
            // Down a step as it walks, as up one: a stair is walked, not fallen down.
            if (down) c.VY = 0;
            (c.X, c.Y) = (x, y);
        }
        int to = c.Y;
        if (Upright(m, w, c.X, ref to, c.VY)) c.VY = 0;
        c.Y = to;
    }

    /// <summary>
    /// A body of <paramref name="w"/> at (<paramref name="x"/>, <paramref name="y"/>) moved <paramref name="vx"/> across in parts
    /// of under a cell, on the ground stepping up onto what it walks into and, <paramref name="walks"/>, down off a ledge, each up
    /// to <c>Creatures.StepUp</c> cells. Whether it met what is solid, then flush against the column it would enter; <paramref name="down"/>
    /// whether it stepped down.
    /// </summary>
    private static bool Across(CreaturesModule m, Spec.Walker w, ref int x, ref int y, int vx, bool grounded, bool walks, out bool down)
    {
        int half = w.Width * Unit / 2, parts = Math.Abs(vx) / Unit + 1;
        bool met = false;
        down = false;
        for (int i = 0; i < parts && !met; i++)
        {
            int to = x + Part(vx, i, parts);
            if (!m.Overlaps(w, to, y)) x = to;
            else if (grounded && StepOnto(m, w, to, y) is int up) (x, y) = (to, y - up);
            else (x, met) = (Flush(to, vx, half), true);
            if (grounded && walks && StepOff(m, w, x, y) is var off and > 0) (y, down) = (y + off, true);
        }
        return met;
    }

    /// <summary>
    /// A body of <paramref name="w"/> at <paramref name="y"/> moved <paramref name="vy"/> down (up, when less than none) in parts
    /// of under a cell: whether it landed or hit its head, then flush against what stopped it.
    /// </summary>
    private static bool Upright(CreaturesModule m, Spec.Walker w, int x, ref int y, int vy)
    {
        int parts = Math.Abs(vy) / Unit + 1;
        for (int i = 0; i < parts; i++)
        {
            int to = y + Part(vy, i, parts);
            if (!m.Overlaps(w, x, to)) y = to;
            else
            {
                y = vy > 0 ? Down(to - 1, Unit) * Unit : (Down(to - w.Height * Unit, Unit) + 1 + w.Height) * Unit;
                return true;
            }
        }
        return false;
    }

    /// <summary>The <paramref name="i"/>th of <paramref name="parts"/> parts of <paramref name="v"/>, whole, the parts summing to it.</summary>
    private static int Part(int v, int i, int parts) => v * (i + 1) / parts - v * i / parts;

    /// <summary>How far up, a cell at a time to <c>Creatures.StepUp</c>, a body walking into what is solid at (<paramref name="x"/>, <paramref name="y"/>) fits; null when it does not.</summary>
    private static int? StepOnto(CreaturesModule m, Spec.Walker w, int x, int y)
    {
        for (int k = 1; k <= Spec.StepUp; k++)
            if (!m.Overlaps(w, x, y - k * Unit)) return k * Unit;
        return null;
    }

    /// <summary>
    /// How far down, a cell at a time to <c>Creatures.StepUp</c>, the ground lies under a body at (<paramref name="x"/>, <paramref name="y"/>)
    /// with none right under it; 0 for none so near, or ground right under it.
    /// </summary>
    private static int StepOff(CreaturesModule m, Spec.Walker w, int x, int y)
    {
        if (m.Overlaps(w, x, y + 1)) return 0;
        for (int k = 1; k <= Spec.StepUp; k++)
        {
            if (m.Overlaps(w, x, y + k * Unit)) return 0;
            if (m.Overlaps(w, x, y + k * Unit + 1)) return k * Unit;
        }
        return 0;
    }

    /// <summary>The middle of a body <paramref name="half"/> wide either way flush against the column it would enter at <paramref name="to"/> going <paramref name="vx"/>.</summary>
    private static int Flush(int to, int vx, int half) =>
        vx > 0 ? Down(to + half - 1, Unit) * Unit - half : (Down(to - half, Unit) + 1) * Unit + half;

    /// <summary>
    /// The speed up of a leap <paramref name="c"/>, standing and blocked going <paramref name="way"/> at <paramref name="speed"/>,
    /// makes over what blocks it, or null to turn: none for one that never jumps; walking, only up a wall at its feet as high as
    /// <c>Creatures.WanderLedge</c>, sized for it; <paramref name="fleeing"/>, the lowest of a jump sized for that ledge, one a step
    /// higher and its whole jump that lands it on the far side.
    /// </summary>
    private static int? JumpFor(CreaturesModule m, Creature c, Spec.Walker w, int way, int speed, bool fleeing)
    {
        if (w.Jump <= 0) return null;
        int g = m.Rules.Fall(Spec.Gravity), across = Math.Min(speed, w.JumpAcross);
        int low = Leaps.SpeedFor(Spec.WanderLedge * Unit + JumpMargin, g, w.Jump);
        if (!fleeing)
        {
            int half = w.Width * Unit / 2, column = Down(way > 0 ? c.X + half : c.X - half - 1, Unit);
            return m.Solid(column, c.Y / Unit - 1) && Lands(m, c, w, way, low, across) ? low : null;
        }
        int higher = Leaps.SpeedFor((Spec.WanderLedge + Spec.StepUp) * Unit + JumpMargin, g, w.Jump);
        foreach (int v in new[] { low, higher, w.Jump })
            if (Lands(m, c, w, way, v, across)) return v;
        return null;
    }

    /// <summary>
    /// Whether a leap of <paramref name="c"/> at <paramref name="vy"/> up and <paramref name="across"/> going <paramref name="way"/>,
    /// flown through the grid as <see cref="Move"/> moves it in the air — falling, pressing flush on what it meets — lands it at
    /// least two cells on and no deeper than <c>Creatures.Drop</c> rows down, a drop it turns from walking but leaps down fleeing (off a
    /// pillar it jumped onto): a reachable landing, not the void nor back where it stood.
    /// </summary>
    private static bool Lands(CreaturesModule m, Creature c, Spec.Walker w, int way, int vy, int across)
    {
        int g = m.Rules.Fall(Spec.Gravity);
        int x = c.X, y = c.Y, vx = way * across, up = -vy;
        for (int t = 0; t < LeapTicks; t++)
        {
            up = Math.Min(up + g, Spec.MaxFall);
            Across(m, w, ref x, ref y, vx, grounded: false, walks: false, out _);
            if (Upright(m, w, x, ref y, up))
            {
                // Two cells on, the 16 px cell it was (0104): not back where it stood.
                if (up > 0) return (x - c.X) * way >= 2 * Unit && y <= c.Y + Spec.Drop * Unit;
                up = 0;
            }
            if (y > c.Y + Spec.Drop * Unit) return false;
        }
        return false;
    }
}

/// <summary>A jump's shape under a planet's gravity (0102): integer, as a creature moves, a tick at a time.</summary>
internal static class Leaps
{
    /// <summary>How high a jump at <paramref name="vy"/> a tick up carries the feet under gravity <paramref name="g"/> a tick, in thousandths.</summary>
    public static int Height(int vy, int g)
    {
        int up = -vy, rise = 0;
        while ((up += g) < 0) rise -= up;
        return rise;
    }

    /// <summary>How far across, in thousandths, a jump at <paramref name="vy"/> up and <paramref name="across"/> a tick lands on ground level with where it left.</summary>
    public static int Length(int vy, int across, int g)
    {
        int up = -vy, y = 0, ticks = 0;
        do
        {
            up += g;
            y += up;
            ticks++;
        }
        while (y < 0);
        return across * ticks;
    }

    /// <summary>The least speed up, in twenties (tens of the 16 px cell, 0104), whose jump carries the feet <paramref name="height"/> up; at most <paramref name="most"/>.</summary>
    public static int SpeedFor(int height, int g, int most)
    {
        int v = 0;
        while (v < most && Height(v, g) < height) v += 20;
        return Math.Min(v, most);
    }
}
