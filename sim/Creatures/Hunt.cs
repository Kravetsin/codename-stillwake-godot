using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The hunt (the catcher, tracker-catchers): still its warning where it came, then on its way to its quarry's reported cell by a path
/// (catcher-paths), running on ground, flying where that is better, breaking what is in the way where going round costs more; its
/// first move begun at it within its strike in line. Its distance not shrunk for a while, it leaps to land ahead of its quarry. Out
/// of time it calls the hunt off and leaves straight up through anything. Its quarry out, gone or off the surface, crew has reset
/// the meter: it leaves. A blow from another hero it answers with its second move, at most every <c>AnswerEvery</c> ticks, then
/// turns back. What it keeps, in its <c>Mem</c>: its quarry (its peer plus one), the clocks its warning and its hunt end at, and of
/// its next answer; when its path is due again and the goal it last sought (a cell plus one); the leap's top and landing (each a cell plus one,
/// none while not leaping), its clock and the closest it came (plus one); its quarry's cell when last sampled (each plus one) and
/// how far it went in the sample before.
/// </summary>
internal sealed class HuntDrive : DriveRule
{
    internal const string Quarry = "quarry", WarnTill = "warnTill", HuntTill = "huntTill", AnswerAt = "answerAt",
        PlanAt = "planAt", Sought = "sought", LeapTop = "leapTop", LeapTo = "leapTo", LeapClock = "leapClock", Closest = "closest",
        SampleX = "sampleX", SampleY = "sampleY", DriftX = "driftX", DriftY = "driftY";

    internal override Type Serves => typeof(Spec.Hunt);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override bool Menaces => true;

    /// <summary><paramref name="c"/>, just come, starts hunting <paramref name="peer"/>'s hero: its warning from now.</summary>
    internal static void Start(CreaturesModule m, Creature c, Spec.Species spec, int peer)
    {
        var hunt = spec.Brain.OfType<Spec.Hunt>().Single();
        c.Keep(Quarry, peer + 1);
        c.Keep(WarnTill, m.Clock + hunt.Warn);
        c.Keep(HuntTill, m.Clock + hunt.Warn + hunt.Ticks);
        c.Target = Ref.Hero(peer);
    }

    /// <summary>The peer whose hero <paramref name="c"/> hunts; null for one that hunts none.</summary>
    internal static int? QuarryOf(Creature c) => c.Recall(Quarry) is int q and > 0 ? q - 1 : null;

    /// <summary>Whether <paramref name="c"/> has left the hunt, flying off through anything.</summary>
    internal static bool Leaving(Creature c) => c.Recall(FlyerBody.Through) == 1;

    /// <summary>
    /// When <paramref name="c"/> wants a path searched: the clock it is due at, past its warning, free of any move and not leaping;
    /// null for one that wants none now.
    /// </summary>
    internal static int? DueAt(CreaturesModule m, Creature c) =>
        QuarryOf(c) == null || Leaving(c) || c.Act != null || c.Recall(LeapTo) != 0 || m.Clock < c.Recall(WarnTill) || c.Recall(PlanAt) > m.Clock
            ? null : c.Recall(PlanAt);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var hunt = (Spec.Hunt)d;
        c.Keep(FlyerBody.Cuts, 0);
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
        if (m.Clock < c.Recall(WarnTill))
        {
            (c.VX, c.VY) = (0, 0);
            return true;
        }
        int middle = c.Y - spec.Body.Height * Unit / 2;
        int far = Math.Max(Math.Abs(at.X - Down(c.X, Unit)), Math.Abs(at.Y - Down(middle, Unit)));
        if (far <= hunt.Strike && m.InLine(c, spec, at) && RuleBook.Move(spec.Moves[0]).Ready(m, c, spec.Moves[0]))
        {
            Stop(c);
            RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
            return true;
        }
        var f = (Spec.Flyer)spec.Body;
        Sample(m, c, hunt, at);
        if (c.Recall(LeapTo) == 0 && Lags(m, c, hunt, f, far)) Leap(m, c, spec, hunt, f, at, far);
        if (c.Recall(LeapTo) > 0) Fly(m, c, spec, f.Leap);
        else
        {
            if (m.Searcher == c.Id) Plan(m, c, spec, hunt, f, at);
            Follow(m, c, spec, hunt, f);
        }
        return true;
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
    /// Whether <paramref name="c"/> has not come closer than its closest for <c>LeapAfter</c> ticks: then it is time to leap, and its
    /// clock starts again whether it leaps or not.
    /// </summary>
    private static bool Lags(CreaturesModule m, Creature c, Spec.Hunt hunt, Spec.Flyer f, int far)
    {
        if (hunt.LeapAfter == 0 || f.Leap == 0) return false;
        if (c.Recall(Closest) == 0 || far < c.Recall(Closest) - 1 || c.Recall(LeapClock) == 0)
        {
            c.Keep(Closest, far + 1);
            c.Keep(LeapClock, m.Clock);
            return false;
        }
        if (m.Clock - c.Recall(LeapClock) < hunt.LeapAfter) return false;
        c.Keep(Closest, far + 1);
        c.Keep(LeapClock, m.Clock);
        return true;
    }

    /// <summary>
    /// Its leap, if there is one: to where its quarry will be when it lands, and <c>LeapAhead</c> cells past, by how far it went in the
    /// last sample, the meeting's ticks its distance over the leap's speed less its quarry's, rounded down, the lead capped at
    /// <c>LeadMost</c> cells each way; dropped to the ground below or risen out of it by a step. It goes up first, to a top over the
    /// middle of the way a third as high as the way is long, at most its body's height. None with its quarry too fast to close on
    /// (closing under a quarter of its speed), no ground below, deeper in, or something solid on the way.
    /// </summary>
    private static void Leap(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, Spec.Flyer f, (int X, int Y) at, int far)
    {
        int driftX = c.Recall(DriftX), driftY = c.Recall(DriftY);
        int closing = f.Leap - Math.Max(Math.Abs(driftX), Math.Abs(driftY)) * Unit / hunt.Sample;
        if (closing < f.Leap / 4) return;
        int ticks = far * Unit / closing;
        int x = at.X + Math.Clamp(driftX * ticks / hunt.Sample + Math.Sign(driftX) * hunt.LeapAhead, -hunt.LeadMost, hunt.LeadMost);
        int y = at.Y + Math.Clamp(driftY * ticks / hunt.Sample, -hunt.LeadMost, hunt.LeadMost) + f.Height / 2;
        if (x < 0 || x >= m.GridWidth) return;
        // A point a row or two into the ground, its quarry's middle reported low, rises out of it as a step would.
        for (int rise = 0; y >= 0 && y < m.GridHeight && m.Overlaps(f, m.Middle(x), (y + 1) * Unit); rise++)
        {
            if (rise >= Spec.StepUp) return;
            y -= 1;
        }
        if (y < 0 || y >= m.GridHeight) return;
        for (int fall = 0; !m.Grounded(f, x, y); fall++)
        {
            if (fall >= Spec.Paths.Margin || y + 1 >= m.GridHeight) return;
            y += 1;
        }
        int sx = Down(c.X, Unit), sy = Down(c.Y - 1, Unit);
        int ax = (sx + x) / 2, ay = Math.Max(f.Height - 1, Math.Min(sy, y) - Math.Min(f.Height, Math.Abs(x - sx) / 3));
        if (!m.Clear(spec.Body, c.X, c.Y, m.Middle(ax), (ay + 1) * Unit) || !m.Clear(spec.Body, m.Middle(ax), (ay + 1) * Unit, m.Middle(x), (y + 1) * Unit)) return;
        c.Keep(LeapTop, ay * m.GridWidth + ax + 1);
        c.Keep(LeapTo, y * m.GridWidth + x + 1);
        c.Path = null;
    }

    /// <summary>
    /// A search from where it stands to its quarry, body middle on its quarry's: its path, due again in <c>Paths.Replan</c> ticks, or
    /// twice that when it fell short of a goal that has not moved.
    /// </summary>
    private static void Plan(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, Spec.Flyer f, (int X, int Y) at)
    {
        int sx = Down(c.X, Unit), sy = Down(c.Y - 1, Unit);
        int gx = at.X, gy = Math.Min(m.GridHeight - 1, at.Y + f.Height / 2), goal = gy * m.GridWidth + gx + 1;
        c.Path ??= [];
        bool reached = m.Paths.Find(m, f, hunt.Speed, sx, sy, gx, gy, c.Path);
        // Off its node, it goes to it first, as a cut: a move ended between cells or a knock left it there.
        if (c.X != m.Middle(sx) || c.Y != (sy + 1) * Unit) c.Path.Insert(0, (sy * m.GridWidth + sx) * 4 + (int)StepKind.Cut);
        c.Keep(PlanAt, m.Clock + (!reached && c.Recall(Sought) == goal ? 2 : 1) * Spec.Paths.Replan);
        c.Keep(Sought, goal);
    }

    /// <summary>
    /// Along its path: the steps it stands on or passed dropped, then at the next, running at <c>Run</c> on a run step (up a step
    /// rising first, down one moving across first), else at its speed, breaking on a cut step; straight steps alike in a row are
    /// one, so it keeps its speed through them. A step no longer passable as it was planned: due a search, holding still.
    /// </summary>
    private static void Follow(CreaturesModule m, Creature c, Spec.Species spec, Spec.Hunt hunt, Spec.Flyer f)
    {
        var path = c.Path;
        int width = m.GridWidth;
        while (path is { Count: > 0 } && Passed(m, c, path, width)) path.RemoveAt(0);
        if (path is not { Count: > 0 })
        {
            Due(m, c);
            return;
        }
        var (x, y, kind) = Node(path[0], width);
        if (!Passable(m, spec, f, x, y, kind))
        {
            Due(m, c);
            return;
        }
        // The last of the straight steps alike in a row from here.
        int last = 0;
        int sx = Math.Sign(m.Middle(x) - c.X), sy = Math.Sign((y + 1) * Unit - c.Y);
        while (last + 1 < path.Count && (kind != StepKind.Run || sy == 0))
        {
            var (nx, ny, nk) = Node(path[last + 1], width);
            var (px, py, _) = Node(path[last], width);
            if (nk != kind || nx - px != sx || ny - py != sy) break;
            last += 1;
        }
        var (tx, ty, _) = Node(path[last], width);
        int dx = m.Middle(tx) - c.X, dy = (ty + 1) * Unit - c.Y;
        if (kind == StepKind.Run)
        {
            if (dy < 0) dx = 0;
            else if (dy > 0 && dx != 0) dy = 0;
            (c.VX, c.VY) = (Math.Clamp(dx, -f.Run, f.Run), Math.Clamp(dy, -f.Run, f.Run));
        }
        else (c.VX, c.VY) = Toward(dx, dy, hunt.Speed);
        c.Keep(FlyerBody.Cuts, kind == StepKind.Cut ? 1 : 0);
    }

    /// <summary>Its leap, at <paramref name="speed"/> straight at its top, then its landing; landed, or stopped short, a search is due.</summary>
    private static void Fly(CreaturesModule m, Creature c, Spec.Species spec, int speed)
    {
        int to = (c.Recall(LeapTop) is > 0 and var top ? top : c.Recall(LeapTo)) - 1;
        int dx = m.Middle(to % m.GridWidth) - c.X, dy = (to / m.GridWidth + 1) * Unit - c.Y;
        if (dx == 0 && dy == 0 && c.Recall(LeapTop) > 0)
        {
            c.Keep(LeapTop, 0);
            Fly(m, c, spec, speed);
            return;
        }
        var (vx, vy) = Toward(dx, dy, speed);
        if (dx == 0 && dy == 0 || m.Overlaps(spec.Body, c.X + vx, c.Y) || m.Overlaps(spec.Body, c.X + vx, c.Y + vy))
        {
            Stop(c);
            Due(m, c);
            return;
        }
        (c.VX, c.VY) = (vx, vy);
    }

    /// <summary>A move of (<paramref name="dx"/>, <paramref name="dy"/>) at most <paramref name="speed"/> on its longer axis, the other its share, rounded toward none.</summary>
    private static (int, int) Toward(int dx, int dy, int speed)
    {
        int most = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return most <= speed ? (dx, dy) : ((int)((long)speed * dx / most), (int)((long)speed * dy / most));
    }

    /// <summary>
    /// Whether <paramref name="c"/> stands on the path's first node, or has passed it on the straight way to the second, the steps alike.
    /// </summary>
    private static bool Passed(CreaturesModule m, Creature c, List<int> path, int width)
    {
        var (x, y, kind) = Node(path[0], width);
        long ax = c.X - m.Middle(x), ay = c.Y - (y + 1) * Unit;
        if (ax == 0 && ay == 0) return true;
        if (path.Count < 2) return false;
        var (nx, ny, nk) = Node(path[1], width);
        if (nk != kind) return false;
        long bx = (nx - x) * (long)Unit, by = (ny - y) * (long)Unit;
        return ax * by == ay * bx && ax * bx + ay * by > 0 && ax * bx + ay * by < bx * bx + by * by;
    }

    /// <summary>Whether the step to (<paramref name="x"/>, <paramref name="y"/>) is still passable as <paramref name="kind"/>: free for a run or a flight, breakable for a cut.</summary>
    private static bool Passable(CreaturesModule m, Spec.Species spec, Spec.Flyer f, int x, int y, StepKind kind)
    {
        if (!m.Overlaps(spec.Body, m.Middle(x), (y + 1) * Unit)) return true;
        return kind == StepKind.Cut && m.Breaks(f, x, y);
    }

    private static (int X, int Y, StepKind Kind) Node(int step, int width) => (step / 4 % width, step / 4 / width, (StepKind)(step % 4));

    /// <summary>Holding still, a search due now.</summary>
    private static void Due(CreaturesModule m, Creature c)
    {
        (c.VX, c.VY) = (0, 0);
        c.Path = null;
        c.Keep(PlanAt, Math.Min(c.Recall(PlanAt), m.Clock));
    }

    /// <summary>Its way given up: no leap, no path, a search due when it is free again.</summary>
    private static void Stop(Creature c)
    {
        c.Keep(LeapTop, 0);
        c.Keep(LeapTo, 0);
        c.Path = null;
        c.Keep(PlanAt, 0);
    }

    /// <summary>Its hunt over: it flies off through anything, its quarry let be.</summary>
    private static void Leave(Creature c)
    {
        Stop(c);
        c.Keep(FlyerBody.Through, 1);
        c.Target = Ref.None;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    /// <summary>
    /// Struck by a hero not its quarry, free of any move, hunting and with its answer ready: its second move at the striker. Every
    /// blow is claimed, so nothing else answers.
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
