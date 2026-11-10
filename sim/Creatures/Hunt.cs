using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The hunt (the catcher, tracker-catchers, catcher-paths, catcher-combat): from the tick it comes, on its way to its quarry's reported
/// cell by a path run on the ground, falling where it stands on nothing; its first move begun at it within its strike in line. On the
/// run it strikes its second move's arc at its quarry in sight, its aim held a tell before. Its distance not shrunk for a while, or no
/// run reaching its quarry, it gathers where it stands and is there, ahead of its quarry or near it in its sight, then stands spent; now
/// and then it rises and lobs a salvo of shells round its quarry, then falls; shut out with no spot in sight, it lobs a charge at what
/// stands between them. Out of time it calls the hunt off and leaves straight up through anything. Its quarry out, gone or off the
/// surface, crew has reset the meter: it leaves. A blow from another hero it answers with its second move, at most every
/// <c>AnswerEvery</c> ticks, then turns back. What it keeps, in its <c>Mem</c>, a clock or a cell plus one of none kept as 0: its quarry
/// (its peer plus one), when its hunt ends and its next answer; when its path is due again, the goal it last sought and whether that
/// search fell short; its quarry's cell when last sampled, how far it went in the sample before, the closest it came and since when;
/// whether it aims, the cell its aim holds and its next shot; its blink's spot, when it is there, when it last was, when its spent ends
/// and the next blink may start; its salvo's phase, top and next clock, its shells' marks and when they land; its next lob.
/// </summary>
internal sealed class HuntDrive : DriveRule
{
    internal const string Quarry = "quarry", HuntTill = "huntTill", AnswerAt = "answerAt",
        PlanAt = "planAt", Sought = "sought", Short = "short", Closest = "closest", LagClock = "lagClock",
        SampleX = "sampleX", SampleY = "sampleY", DriftX = "driftX", DriftY = "driftY",
        Aiming = "aiming", Held = "held", FireAt = "fireAt",
        BlinkTo = "blinkTo", BlinkAt = "blinkAt", BlinkedAt = "blinkedAt", SpentTill = "spentTill", BlinkNext = "blinkNext",
        Salvo = "salvo", SalvoTop = "salvoTop", SalvoAt = "salvoAt", Thrown = "thrown", LobAt = "lobAt";

    /// <summary>Its shells' marks, by shell: a cell plus one.</summary>
    internal static readonly string[] Marks = [.. Enumerable.Range(0, 16).Select(i => "mark" + i)];

    /// <summary>A salvo's phases: rising to its top, then falling to the ground.</summary>
    internal const int Rising = 1, Falling = 2;

    /// <summary>How near its quarry, in cells, the nearest spot a blink takes may be: never on it.</summary>
    private const int Gap = 3;

    internal override Type Serves => typeof(Spec.Hunt);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override bool Menaces => true;

    /// <summary><paramref name="c"/>, just come, starts hunting <paramref name="peer"/>'s hero, at once; its first shot and salvo a whole wait away.</summary>
    internal static void Start(CreaturesModule m, Creature c, Spec.Species spec, int peer)
    {
        var hunt = spec.Brain.OfType<Spec.Hunt>().Single();
        c.Keep(Quarry, peer + 1);
        c.Keep(HuntTill, m.Clock + hunt.Ticks);
        c.Keep(FireAt, m.Clock + hunt.Gunfire.Every);
        c.Keep(SalvoAt, m.Clock + hunt.Salvo.Every);
        c.Target = Ref.Hero(peer);
    }

    /// <summary>The peer whose hero <paramref name="c"/> hunts; null for one that hunts none.</summary>
    internal static int? QuarryOf(Creature c) => c.Recall(Quarry) is int q and > 0 ? q - 1 : null;

    /// <summary>Whether <paramref name="c"/> has left the hunt, flying off through anything.</summary>
    internal static bool Leaving(Creature c) => c.Recall(FlyerBody.Through) == 1;

    /// <summary>Whether <paramref name="c"/> is gathering for a blink, spent after one, or in a salvo: off its path.</summary>
    internal static bool Busy(CreaturesModule m, Creature c) => c.Recall(BlinkTo) != 0 || c.Recall(Salvo) != 0 || m.Clock < c.Recall(SpentTill);

    /// <summary>
    /// When <paramref name="c"/> wants a path searched: the clock it is due at, free of any move, busy with nothing else and on the
    /// ground or on its way; null for one that wants none now.
    /// </summary>
    internal static int? DueAt(CreaturesModule m, Creature c) =>
        m.Rules.Floats || QuarryOf(c) == null || Leaving(c) || c.Act != null || Busy(m, c) || c.Recall(PlanAt) > m.Clock
        || c.Path is not { Count: > 0 } && !Landed(m, c, Spec.Get(c.Species).Body) ? null : c.Recall(PlanAt);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var hunt = (Spec.Hunt)d;
        if (Leaving(c))
        {
            (c.VX, c.VY) = (0, -hunt.Speed);
            return true;
        }
        if (QuarryOf(c) is not int peer || m.HeroAt(peer, awake: true) is not { } at)
        {
            Leave(c);
            return true;
        }
        if (m.Clock >= c.Recall(HuntTill))
        {
            m.CallOff(peer);
            Leave(c);
            return true;
        }
        int middle = c.Y - spec.Body.Height * Unit / 2;
        int far = Math.Max(Math.Abs(at.X - Down(c.X, Unit)), Math.Abs(at.Y - Down(middle, Unit)));
        if (c.Recall(Salvo) != 0)
        {
            Salvoing(m, c, spec, hunt, at);
            return true;
        }
        if (m.Clock < c.Recall(SpentTill))
        {
            Hold(c);
            return true;
        }
        Gunfire(m, c, spec, hunt, at, far);
        if (c.Recall(BlinkTo) != 0)
        {
            Blinking(m, c, spec, hunt);
            return true;
        }
        if (far <= hunt.Strike && m.InLine(c, spec, at) && RuleBook.Move(spec.Moves[0]).Ready(m, c, spec.Moves[0]))
        {
            Stop(c);
            RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
            return true;
        }
        var f = (Spec.Flyer)spec.Body;
        Sample(m, c, hunt, at);
        bool lags = Lags(m, c, hunt, far);
        if (m.Rules.Floats)
        {
            Afloat(m, c, spec, hunt, f, at, lags);
            return true;
        }
        if (SalvoDue(m, c, spec, hunt, far))
        {
            StartSalvo(m, c, hunt);
            return true;
        }
        if ((lags || c.Recall(Short) == 1) && m.Clock >= c.Recall(BlinkNext))
        {
            if (StartBlink(m, c, hunt, f, at)) return true;
            if (c.Recall(Short) == 1) LobAtShelter(m, c, spec, hunt, at);
        }
        if (c.Path is not { Count: > 0 } && !Landed(m, c, spec.Body))
        {
            Fall(m, c, spec.Body, hunt.Speed);
            return true;
        }
        if (m.Searcher == c.Id) Plan(m, c, spec, f, at);
        Follow(m, c, spec, f);
        return true;
    }

    /// <summary>
    /// Its hunt where things float (catchers-space): no path, no salvo. Its quarry shut in a room, it is short and lobs its charge at
    /// what stands between them; lagging, it blinks ahead or near in open space; else it flies straight at its quarry, sliding along
    /// what it meets.
    /// </summary>
    private static void Afloat(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, Spec.Flyer f, (int X, int Y) at, bool lags)
    {
        bool shut = m.InRoom(m.CellOf(at));
        c.Keep(Short, shut ? 1 : 0);
        if (shut) LobAtShelter(m, c, spec, hunt, at);
        else if (lags && m.Clock >= c.Recall(BlinkNext) && StartBlink(m, c, hunt, f, at)) return;
        FlyAt(c, spec, hunt, at, m);
    }

    /// <summary>
    /// Its way straight at its quarry, its middle on the middle of its quarry's cell: at <c>Speed</c> on the longer axis, the other
    /// its share, rounded toward none; never past it. The body stops each axis at what it meets, so it slides along a hull.
    /// </summary>
    private static void FlyAt(Creature c, Spec.Species spec, Spec.Hunt hunt, (int X, int Y) at, CreaturesModule m)
    {
        int dx = m.Middle(at.X) - c.X, dy = m.Middle(at.Y) + spec.Body.Height * Unit / 2 - c.Y;
        int longer = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (longer <= hunt.Speed)
        {
            (c.VX, c.VY) = (dx, dy);
            return;
        }
        (c.VX, c.VY) = ((int)((long)dx * hunt.Speed / longer), (int)((long)dy * hunt.Speed / longer));
    }

    /// <summary>Every <c>Sample</c> ticks, how far its quarry went since the last sample, and where it is now.</summary>
    private static void Sample(CreaturesModule m, Creature c, Spec.Hunt hunt, (int X, int Y) at)
    {
        if (m.Clock % hunt.Sample != 0) return;
        if (c.Recall(SampleX) > 0)
        {
            c.Keep(DriftX, at.X - (c.Recall(SampleX) - 1));
            c.Keep(DriftY, at.Y - (c.Recall(SampleY) - 1));
        }
        c.Keep(SampleX, at.X + 1);
        c.Keep(SampleY, at.Y + 1);
    }

    /// <summary>
    /// Whether <paramref name="c"/> has not come closer than its closest for <c>LagAfter</c> ticks: then it lags, and its clock starts
    /// again whether it blinks or not.
    /// </summary>
    private static bool Lags(CreaturesModule m, Creature c, Spec.Hunt hunt, int far)
    {
        if (c.Recall(Closest) == 0 || far < c.Recall(Closest) - 1 || c.Recall(LagClock) == 0)
        {
            c.Keep(Closest, far + 1);
            c.Keep(LagClock, m.Clock);
            return false;
        }
        if (m.Clock - c.Recall(LagClock) < hunt.LagAfter) return false;
        c.Keep(Closest, far + 1);
        c.Keep(LagClock, m.Clock);
        return true;
    }

    /// <summary>
    /// Its laser on the run: its quarry in sight past its pounce (<c>Strike</c>) and within the arc's reach, it aims (drawn as its aim
    /// line); come within its pounce, its held shot is dropped. <c>Tell</c> ticks before its shot
    /// it holds its quarry's cell, or as soon after as its quarry is in sight, and strikes it as the shot comes due, the next
    /// <c>Every</c> ticks on.
    /// </summary>
    private static void Gunfire(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, (int X, int Y) at, int far)
    {
        var arc = (Spec.Shoot)spec.Moves[1];
        // Within its pounce it pounces, never shoots: played, a laser at a quarry beside it ran off anywhere.
        bool sees = far > hunt.Strike && far <= arc.Reach && m.InLine(c, spec, at);
        c.Keep(Aiming, sees ? 1 : 0);
        int due = c.Recall(FireAt);
        if (c.Recall(Held) == 0)
        {
            if (m.Clock < due - hunt.Gunfire.Tell || !sees) return;
            c.Keep(Held, m.CellOf(at) + 1);
            c.Keep(FireAt, Math.Max(due, m.Clock + hunt.Gunfire.Tell));
            return;
        }
        if (far <= hunt.Strike)
        {
            c.Keep(Held, 0);
            return;
        }
        if (m.Clock < due) return;
        m.StrikeArc(c, spec, arc, c.Recall(Held) - 1);
        c.Keep(Held, 0);
        c.Keep(FireAt, m.Clock + hunt.Gunfire.Every);
    }

    /// <summary>
    /// Its blink, if there is a spot for it: ahead of its quarry, unless its last search fell short of it, else the nearest in its sight.
    /// Gathering, it holds still till it is there. None: it looks again a sample later.
    /// </summary>
    private static bool StartBlink(CreaturesModule m, Creature c, Spec.Hunt hunt, Spec.Flyer f, (int X, int Y) at)
    {
        var b = hunt.Blink;
        if (((c.Recall(Short) == 1 ? null : LeadSpot(m, c, hunt, f, at)) ?? NearSpot(m, f, b, at)) is not int spot)
        {
            c.Keep(BlinkNext, m.Clock + hunt.Sample);
            return false;
        }
        Stop(c);
        Hold(c);
        c.Keep(BlinkTo, spot + 1);
        c.Keep(BlinkAt, m.Clock + b.Gather);
        c.Keep(BlinkNext, m.Clock + b.Every);
        return true;
    }

    /// <summary>
    /// The spot ahead of its quarry: where it will be as the gather ends by how far it went in the last sample, and <c>Ahead</c> cells
    /// past, the lead capped at <c>LeadMost</c> each way; a quarry standing, half a strike off it on its own side; its footing there.
    /// </summary>
    private static int? LeadSpot(CreaturesModule m, Creature c, Spec.Hunt hunt, Spec.Flyer f, (int X, int Y) at)
    {
        var b = hunt.Blink;
        int driftX = c.Recall(DriftX), driftY = c.Recall(DriftY);
        int lead = driftX != 0 ? driftX * b.Gather / hunt.Sample + Math.Sign(driftX) * b.Ahead : Math.Sign(Down(c.X, Unit) - at.X) * hunt.Strike / 2;
        int x = at.X + Math.Clamp(lead, -b.LeadMost, b.LeadMost), y = at.Y + Math.Clamp(driftY * b.Gather / hunt.Sample, -b.LeadMost, b.LeadMost) + f.Height / 2;
        return m.Rules.Floats ? Open(m, f, x, y) : Footing(m, f, x, y);
    }

    /// <summary>Where things float, (<paramref name="x"/>, <paramref name="y"/>) as a cell if a body of <paramref name="f"/> standing there lies wholly in open space; else none.</summary>
    private static int? Open(CreaturesModule m, Spec.Flyer f, int x, int y) =>
        x >= 0 && x < m.GridWidth && y >= 0 && y < m.GridHeight && m.InSpace(f, m.Middle(x), (y + 1) * Unit) ? y * m.GridWidth + x : null;

    /// <summary>
    /// Where a body of <paramref name="f"/> stands nearest (<paramref name="x"/>, <paramref name="y"/>), as a cell: risen out of the
    /// ground by up to a step (a quarry's middle reported low), dropped to it within <c>Paths.Margin</c> (a quarry in the air); none off
    /// the grid, deeper in, or with no ground below.
    /// </summary>
    private static int? Footing(CreaturesModule m, Spec.Flyer f, int x, int y)
    {
        if (x < 0 || x >= m.GridWidth) return null;
        for (int rise = 0; y >= 0 && y < m.GridHeight && m.Overlaps(f, m.Middle(x), (y + 1) * Unit); rise++)
        {
            if (rise >= Spec.StepUp) return null;
            y -= 1;
        }
        if (y < 0 || y >= m.GridHeight) return null;
        for (int fall = 0; !m.Grounded(f, x, y); fall++)
        {
            if (fall >= Spec.Paths.Margin || y + 1 >= m.GridHeight) return null;
            y += 1;
        }
        return y * m.GridWidth + x;
    }

    /// <summary>
    /// The spot nearest its quarry, ring by ring from <see cref="Gap"/> cells to <c>Near</c>, rows from the top, columns from the left:
    /// the body free standing there on ground, its middle in its quarry's sight. None for a quarry shut in where it does not fit.
    /// </summary>
    private static int? NearSpot(CreaturesModule m, Spec.Flyer f, Spec.Blink b, (int X, int Y) at)
    {
        int feet = at.Y + f.Height / 2;
        for (int d = Gap; d <= b.Near; d++)
            for (int dy = -d; dy <= d; dy++)
                for (int dx = -d; dx <= d; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != d) continue;
                    int x = at.X + dx, y = feet + dy, bottom = (y + 1) * Unit;
                    if (x < 0 || y < 0 || x >= m.GridWidth || y >= m.GridHeight) continue;
                    bool stands = m.Rules.Floats ? m.InSpace(f, m.Middle(x), bottom) : !m.Overlaps(f, m.Middle(x), bottom) && m.Grounded(f, x, y);
                    if (!stands || !m.Sees(m.Middle(x), bottom - f.Height * Unit / 2, at)) continue;
                    return y * m.GridWidth + x;
                }
        return null;
    }

    /// <summary>Gathering, still; its time come, it is on its spot if the body still fits there, else it stays; spent either way, a search due.</summary>
    private static void Blinking(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt)
    {
        Hold(c);
        if (m.Clock < c.Recall(BlinkAt)) return;
        int to = c.Recall(BlinkTo) - 1, x = m.Middle(to % m.GridWidth), y = (to / m.GridWidth + 1) * Unit;
        c.Keep(BlinkTo, 0);
        c.Keep(BlinkAt, 0);
        Stop(c);
        if (m.Overlaps(spec.Body, x, y)) return;
        (c.X, c.Y) = (x, y);
        c.Keep(BlinkedAt, m.Clock);
        c.Keep(SpentTill, m.Clock + hunt.Blink.Spent);
    }

    /// <summary>Whether its salvo is due: its clock come, standing on ground with room to rise to its top, its quarry within its arc's reach.</summary>
    private static bool SalvoDue(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, int far) =>
        m.Clock >= c.Recall(SalvoAt) && far <= ((Spec.Shoot)spec.Moves[1]).Reach && Landed(m, c, spec.Body)
        && m.Clear(spec.Body, c.X, c.Y, c.X, c.Y - hunt.Salvo.Headroom * Unit);

    private static void StartSalvo(CreaturesModule m, Creature c, Spec.Hunt hunt)
    {
        Stop(c);
        Hold(c);
        c.Keep(Salvo, Rising);
        c.Keep(SalvoTop, c.Y - hunt.Salvo.Hover * Unit);
        c.Keep(SalvoAt, m.Clock + hunt.Salvo.Every);
    }

    /// <summary>Its salvo: rising at its speed to its top, or as high as it gets; there it throws, then falls; landed, a search is due.</summary>
    private static void Salvoing(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, (int X, int Y) at)
    {
        if (c.Recall(Salvo) == Rising)
        {
            int rise = Math.Min(hunt.Speed, c.Y - c.Recall(SalvoTop));
            if (rise > 0 && !m.Overlaps(spec.Body, c.X, c.Y - rise))
            {
                (c.VX, c.VY) = (0, -rise);
                return;
            }
            Throw(m, c, spec, hunt.Salvo, at);
            c.Keep(Salvo, Falling);
            Hold(c);
            return;
        }
        if (!Landed(m, c, spec.Body))
        {
            Fall(m, c, spec.Body, hunt.Speed);
            return;
        }
        c.Keep(Salvo, 0);
        c.Keep(SalvoTop, 0);
        Due(m, c);
    }

    /// <summary>
    /// Its shells, from its middle, one on each of <c>Shells</c> columns spread evenly over <c>Spread</c> round its quarry's (rounded
    /// down, clamped to the grid): each on the floor under its quarry's row, risen out of the ground first within <c>Paths.Margin</c>,
    /// marked there till they land, each <c>Stagger</c> ticks after the one before it.
    /// </summary>
    private static void Throw(CreaturesModule m, Creature c, Spec.Species spec, Spec.Salvo salvo, (int X, int Y) at)
    {
        int x0 = c.X, y0 = c.Y - spec.Body.Height * Unit / 2, width = m.GridWidth;
        for (int i = 0; i < Math.Min(salvo.Shells, Marks.Length); i++)
        {
            int x = Math.Clamp(at.X - salvo.Spread / 2 + i * salvo.Spread / Math.Max(1, salvo.Shells - 1), 0, width - 1), y = at.Y;
            for (int rise = 0; rise < Spec.Paths.Margin && y > 0 && m.Solid(x, y); rise++) y--;
            y = m.FloorUnder(x, y);
            c.Keep(Marks[i], y * width + x + 1);
            m.Lob(c, salvo.Projectile, x0, y0, m.Middle(x), (y + 1) * Unit + Unit / 2, salvo.Flight + i * salvo.Stagger);
        }
        c.Keep(Thrown, m.Clock + 1);
    }

    /// <summary>Its lob at a shelter, every <c>Every</c> ticks: from its middle at the solid cell nearest its quarry on the straight line between them.</summary>
    private static void LobAtShelter(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, (int X, int Y) at)
    {
        if (m.Clock < c.Recall(LobAt)) return;
        int x = c.X, y = c.Y - spec.Body.Height * Unit / 2;
        if (m.FirstSolid(m.Middle(at.X), m.Middle(at.Y), x, y) is not int wall) return;
        c.Keep(LobAt, m.Clock + hunt.Lob.Every);
        m.Lob(c, hunt.Lob.Projectile, x, y, m.Middle(wall % m.GridWidth), m.Middle(wall / m.GridWidth), hunt.Lob.Flight);
    }

    /// <summary>
    /// A search from where it stands to its quarry: to its footing under it, body middle on its quarry's, else to that point as it is.
    /// Its path, due again in <c>Paths.Replan</c> ticks, or twice that when it fell short of a goal that has not moved; whether it fell
    /// short kept, so a quarry it has no run to is blinked to.
    /// </summary>
    private static void Plan(CreaturesModule m, Creature c, Spec.Species spec, Spec.Flyer f, (int X, int Y) at)
    {
        int sx = Down(c.X, Unit), sy = Down(c.Y - 1, Unit), gy0 = Math.Min(m.GridHeight - 1, at.Y + f.Height / 2);
        int goal = (Footing(m, f, at.X, gy0) ?? gy0 * m.GridWidth + at.X) + 1, gx = (goal - 1) % m.GridWidth, gy = (goal - 1) / m.GridWidth;
        c.Path ??= [];
        bool reached = m.Paths.Find(m, f, sx, sy, gx, gy, c.Path);
        // Off its node, it goes on to the first step if the way there is clear, never back to the node (a playtest saw it turn
        // back at each search); else to the node first.
        if ((c.X != m.Middle(sx) || c.Y != (sy + 1) * Unit) && !(c.Path is [var first, ..] && Clear(m, spec, c, first, m.GridWidth)))
            c.Path.Insert(0, (sy * m.GridWidth + sx) * 4 + (int)StepKind.Run);
        c.Keep(PlanAt, m.Clock + (!reached && c.Recall(Sought) == goal ? 2 : 1) * Spec.Paths.Replan);
        c.Keep(Sought, goal);
        c.Keep(Short, reached ? 0 : 1);
    }

    /// <summary>
    /// Along its path: the steps it stands on or passed dropped, then on through the run steps at <c>Run</c>. A step no longer free, or
    /// of a kind it no longer takes (a flight or a cut from an older save): due a search, holding still.
    /// </summary>
    private static void Follow(CreaturesModule m, Creature c, Spec.Species spec, Spec.Flyer f)
    {
        var path = c.Path;
        int width = m.GridWidth;
        if (path is { Count: > 0 }) path.RemoveRange(0, Passed(m, c, path, width));
        if (path is not { Count: > 0 })
        {
            Due(m, c);
            return;
        }
        var (x, y, kind) = Node(path[0], width);
        if (kind != StepKind.Run || m.Overlaps(spec.Body, m.Middle(x), (y + 1) * Unit))
        {
            Due(m, c);
            return;
        }
        (c.VX, c.VY) = RunAlong(m, spec.Body, path, c.X, c.Y, width, f.Run);
    }

    /// <summary>
    /// Its move this tick along the run steps from the first, <paramref name="budget"/> across through their corners, so it keeps its
    /// speed (a playtest saw it halt at each): up a step it rises then goes across, down one across then down, the step itself free
    /// as a hero steps, up to <c>StepUp</c> cells a tick; one way across and one way up or down a tick. The body takes the sum rising first, else across first: legs out of that order (a rise after
    /// going across, across after dropping) only where the body is free taking them so, else it stops before them.
    /// </summary>
    private static (int, int) RunAlong(CreaturesModule m, Spec.Shape body, List<int> path, int x0, int y0, int width, int budget)
    {
        int x = x0, y = y0, vx = 0, vy = 0, way = 0, rise = 0, lift = Spec.StepUp * Unit;
        bool across = false, early = false;
        (int, int) before = (0, 0);
        for (int i = 0; i < path.Count && budget > 0; i++)
        {
            var (nx, ny, kind) = Node(path[i], width);
            if (kind != StepKind.Run) break;
            int tx = m.Middle(nx), ty = (ny + 1) * Unit;
            bool whole = ty < y ? Leg(0, ty - y) && Leg(tx - x, 0) : Leg(tx - x, 0) && Leg(0, ty - y);
            if (!whole) break;
        }
        if (early && (vy < 0 ? m.Overlaps(body, x0, y0 + vy) : m.Overlaps(body, x0 + vx, y0))) return before;
        return (vx, vy);

        // A leg, straight across or up or down, taken as far as the budget goes (up or down, the lift); false where it is not taken whole.
        bool Leg(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return true;
            if (dx != 0 && way != 0 && Math.Sign(dx) != way || dy != 0 && rise != 0 && Math.Sign(dy) != rise) return false;
            if (!early && (dy < 0 && across || dx != 0 && rise > 0)) (early, before) = (true, (vx, vy));
            int length = Math.Abs(dx) + Math.Abs(dy), take = Math.Min(length, dx != 0 ? budget : lift);
            (int sx, int sy) = (Math.Sign(dx) * take, Math.Sign(dy) * take);
            (vx, vy, x, y) = (vx + sx, vy + sy, x + sx, y + sy);
            if (dx != 0) budget -= take;
            else lift -= take;
            if (dx != 0) (across, way) = (true, Math.Sign(dx));
            else rise = Math.Sign(dy);
            return take == length;
        }
    }

    /// <summary>Whether <paramref name="c"/> stands on something: solid right under its feet.</summary>
    private static bool Landed(CreaturesModule m, Creature c, Spec.Shape s) => m.Overlaps(s, c.X, c.Y + 1);

    /// <summary>A fall of at most <paramref name="speed"/>, short of what it would land in: on top of it.</summary>
    private static void Fall(CreaturesModule m, Creature c, Spec.Shape s, int speed)
    {
        int y = c.Y + speed;
        while (y > c.Y && m.Overlaps(s, c.X, y)) y = Math.Max(c.Y, (y - 1) / Unit * Unit);
        (c.VX, c.VY) = (0, y - c.Y);
    }

    /// <summary>
    /// How many of the path's first steps <paramref name="c"/> has passed: up to the last of the next <see cref="Ahead"/> nodes it
    /// stands on or has passed on its way on. A tick's run may pass more than one (a playtest saw it turn back to one it ran past).
    /// </summary>
    private static int Passed(CreaturesModule m, Creature c, List<int> path, int width)
    {
        int passed = 0;
        for (int i = 0; i < Math.Min(Ahead, path.Count); i++)
            if (PassedAt(m, c, path, i, width)) passed = i + 1;
        return passed;
    }

    private const int Ahead = 4;

    /// <summary>
    /// Whether <paramref name="c"/> stands on node <paramref name="i"/> of the path, or has passed it on its way to the next: a run's
    /// legs (up a step rising first, down one going across first), straight on level ground.
    /// </summary>
    private static bool PassedAt(CreaturesModule m, Creature c, List<int> path, int i, int width)
    {
        var (x, y, _) = Node(path[i], width);
        int ax = m.Middle(x), ay = (y + 1) * Unit;
        if (c.X == ax && c.Y == ay) return true;
        if (path.Count < i + 2) return false;
        var (nx, ny, _) = Node(path[i + 1], width);
        int bx = m.Middle(nx), by = (ny + 1) * Unit;
        if (by == ay) return On(c.X, c.Y, ax, ay, bx, by);
        return by < ay ? On(c.X, c.Y, ax, ay, ax, by) || On(c.X, c.Y, ax, by, bx, by) : On(c.X, c.Y, ax, ay, bx, ay) || On(c.X, c.Y, bx, ay, bx, by);
    }

    /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) lies on the segment from a to b, a itself excepted.</summary>
    private static bool On(int x, int y, int ax, int ay, int bx, int by)
    {
        long px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay;
        long along = px * dx + py * dy;
        return (px != 0 || py != 0) && px * dy == py * dx && along >= 0 && along <= dx * dx + dy * dy;
    }

    /// <summary>Whether the body goes from where <paramref name="c"/> is to the node of <paramref name="step"/> as a run takes it, through nothing solid.</summary>
    private static bool Clear(CreaturesModule m, Spec.Species spec, Creature c, int step, int width)
    {
        var (x, y, _) = Node(step, width);
        int tx = m.Middle(x), ty = (y + 1) * Unit;
        if (ty == c.Y) return m.Clear(spec.Body, c.X, c.Y, tx, ty);
        // A run rises first up a step and moves across first down one.
        return ty < c.Y
            ? m.Clear(spec.Body, c.X, c.Y, c.X, ty) && m.Clear(spec.Body, c.X, ty, tx, ty)
            : m.Clear(spec.Body, c.X, c.Y, tx, c.Y) && m.Clear(spec.Body, tx, c.Y, tx, ty);
    }

    private static (int X, int Y, StepKind Kind) Node(int step, int width) => (step / 4 % width, step / 4 / width, (StepKind)(step % 4));

    /// <summary>Holding still, a search due now.</summary>
    private static void Due(CreaturesModule m, Creature c)
    {
        Hold(c);
        c.Path = null;
        c.Keep(PlanAt, Math.Min(c.Recall(PlanAt), m.Clock));
    }

    private static void Hold(Creature c) => (c.VX, c.VY) = (0, 0);

    /// <summary>Its way given up: no path, a search due when it is free again.</summary>
    private static void Stop(Creature c)
    {
        c.Path = null;
        c.Keep(PlanAt, 0);
    }

    /// <summary>Its hunt over: it flies off through anything, its quarry let be, its blink and salvo dropped.</summary>
    private static void Leave(Creature c)
    {
        Stop(c);
        c.Keep(BlinkTo, 0);
        c.Keep(Salvo, 0);
        c.Keep(Aiming, 0);
        c.Keep(FlyerBody.Through, 1);
        c.Target = Ref.None;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    /// <summary>
    /// Struck by a hero not its quarry, free of any move, hunting and with its answer ready: its second move at
    /// the striker. Every blow is claimed, so nothing else answers.
    /// </summary>
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        if (hit.Peer == QuarryOf(c) || c.Act != null || Leaving(c) || m.Clock < c.Recall(AnswerAt) || m.HeroAt(hit.Peer, awake: true) == null) return true;
        c.Keep(AnswerAt, m.Clock + ((Spec.Hunt)d).AnswerEvery);
        c.Target = Ref.Hero(hit.Peer);
        Stop(c);
        RuleBook.Move(spec.Moves[1]).Begin(m, c, spec, 1);
        return true;
    }

    /// <summary>A move over: it hangs where it stopped, turned back to its quarry.</summary>
    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop)
    {
        (c.VX, c.VY) = (0, 0);
        c.Target = QuarryOf(c) is int peer && !Leaving(c) ? Ref.Hero(peer) : Ref.None;
    }

    internal override Gait GaitOf(Creature c) => Gait.Walk;
}
