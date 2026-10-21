using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A script that plays the hero for recordings (<c>-- --autopilot</c>): in the wreck it looks about,
/// opens the doorway to torn room B and floats out on its tether, comes back, jumps, and leaves by the
/// airlock; in the <see cref="Gym"/> (<c>-- --gym</c>) it climbs a ladder, crosses a floor, drops off it,
/// jumps up through it and climbs down again.
/// </summary>
public sealed class HeroAutopilot
{
    /// <summary>A room with a ladder up to a floor, for recording climbing.</summary>
    public static readonly Layout Gym = Layout.Parse("""
        ################
        #..............#
        #..............#
        #..............#
        #..............#
        #..............#
        #....H=====....#
        #....H.........#
        #....H.........#
        #.S..H.........#
        ################
        """);

    /// <summary>
    /// A hall for recording the figure's motion (0090), <c>-- --poses</c>: a ladder up to a floor eight cells over the deck and
    /// one up to a floor twenty-one over it, each walked off; lamps under its ceiling and halfway down, lit with <c>--lit</c>.
    /// </summary>
    public static readonly Layout Hall = Layout.Parse(
        [.. Enumerable.Range(0, 32).Select(r => Lamps(r, r switch
        {
            0 or 31 => new string('#', 46),
            10 => "#" + new string('.', 31) + "H========" + new string('.', 4) + "#",
            23 => "#" + new string('.', 7) + "H======" + new string('.', 17) + "H" + new string('.', 12) + "#",
            30 => "#" + new string('.', 7) + "H" + new string('.', 9) + "S" + new string('.', 13) + "H" + new string('.', 12) + "#",
            > 23 => "#" + new string('.', 7) + "H" + new string('.', 23) + "H" + new string('.', 12) + "#",
            > 10 => "#" + new string('.', 31) + "H" + new string('.', 12) + "#",
            _ => "#" + new string('.', 44) + "#",
        }))]);

    /// <summary>
    /// A shaft for recording the ladder (0092), <c>-- --ladders --autopilot --lit</c>: a ladder fourteen cells tall in column 8
    /// up through an open hatch into the room above; right of it a pit four cells deep with a ladder up out of it in column 16,
    /// and the floor on beyond.
    /// </summary>
    public static readonly Layout Ladders = Layout.Parse(
        [.. Enumerable.Range(0, 25).Select(r => ShaftLamps(r, r switch
        {
            0 or 24 => new string('#', 28),
            6 => "#######ddd##################",
            20 => new string('#', 14) + ".." + "H" + "....." + new string('#', 6),
            > 20 => new string('#', 14) + ".." + "H" + "....." + new string('#', 6),
            19 => "#.S.....H.......H..........#",
            > 9 and < 20 => "#.......H.......H..........#",
            > 6 => "#.......H...................#"[..27] + "#",
            _ => "#" + new string('.', 26) + "#",
        }))]);

    /// <summary>
    /// Two ledges for recording the ledge assist (0092), <c>-- --ledges --autopilot --lit</c>: one four cells high, which a held
    /// jump falls 46 short of and steps up onto, and one five high, which it falls clearly short of.
    /// </summary>
    public static readonly Layout Ledges = Layout.Parse(
        [.. Enumerable.Range(0, 13).Select(r => ShaftLamps(r, r switch
        {
            0 or 12 => new string('#', 30),
            11 => "#.S....." + new string('#', 6) + "......" + new string('#', 10),
            >= 8 => "#......." + new string('#', 6) + "......" + new string('#', 10),
            >= 7 => "#......." + new string('.', 6) + "......" + new string('#', 10),
            _ => "#" + new string('.', 28) + "#",
        }))]);

    private static string ShaftLamps(int row, string cells) =>
        row is 2 or 9 or 14 or 19 ? string.Concat(cells.Select((c, x) => c == '.' && x % 5 == 1 ? 'L' : c)) : cells;

    private static string Lamps(int row, string cells) =>
        row is 1 or 7 or 19 or 26 or 29 ? string.Concat(cells.Select((c, x) => c == '.' && x % 6 == 3 ? 'L' : c)) : cells;

    /// <summary>
    /// One act: the keys held until <see cref="Done"/> says so, where the pointer is meanwhile, whether it starts with E, and
    /// whether Space is let go through it, cutting a jump short (0089); the hotbar key, 1 to 9, it starts by pressing
    /// (<see cref="TakeSlot"/>).
    /// </summary>
    private sealed record Act(
        HeroInput Keys, Func<Hero, Progress, bool> Done, Func<Hero, (double X, double Y)?>? Point = null, bool Press = false, bool LetsGo = false,
        int? Slot = null);

    /// <summary>How far into an act the script is: its steps so far, and the hero a step ago.</summary>
    private readonly record struct Progress(int Steps, Hero? Last)
    {
        /// <summary>Whether the hero stood still the last step, a few steps in: stopped by a wall or a door.</summary>
        public bool Stopped(Hero hero) => Steps > 2 && Last is Hero last && last.X == hero.X && last.Y == hero.Y;
    }

    private static readonly HeroInput Still = new(0, 0, false);
    private readonly IReadOnlyList<Act> _acts;
    private int _at, _steps, _held, _step;
    private bool _trace;
    private bool _press;
    private Hero? _last;

    private HeroAutopilot(IReadOnlyList<Act> acts) => _acts = acts;

    /// <summary>Where the pointer is, in cells, for the hero as it stands; none once the script is done.</summary>
    public (double X, double Y)? PointerFor(Hero hero) => _at < _acts.Count ? _acts[_at].Point?.Invoke(hero) : null;

    /// <summary>Whether E was pressed since the last call.</summary>
    public bool TakePress()
    {
        bool pressed = _press;
        _press = false;
        return pressed;
    }

    private int? _slot;

    /// <summary>The hotbar key the script pressed since the last call, if any: its recorder presses it.</summary>
    public int? TakeSlot()
    {
        var slot = _slot;
        _slot = null;
        return slot;
    }

    /// <summary>The keys for the next controller step of <paramref name="hero"/>.</summary>
    public HeroInput Next(Hero hero)
    {
        while (_at < _acts.Count)
        {
            var act = _acts[_at];
            if (_steps == 0 && act.Press) _press = true;
            if (_steps == 0 && act.Slot != null) _slot = act.Slot;
            if (!act.Done(hero, new Progress(_steps, _last))) break;
            _at++;
            _steps = 0;
        }
        _last = hero;
        if (_at >= _acts.Count) return Still;
        _steps++;
        var keys = _acts[_at].Keys;
        // Space stays held a while after each press, so the script's jumps go their full height, but where an act lets go (0089).
        _held = keys.Jump ? HeldFor : _acts[_at].LetsGo ? 0 : Math.Max(0, _held - 1);
        if (_trace) Godot.GD.Print($"feel: step {++_step} act {_at} x {hero.X} y {hero.Y} vx {hero.Vx} vy {hero.Vy} frame {Godot.Engine.GetFramesDrawn()}");
        return keys with { JumpHeld = keys.JumpHeld || _held > 0 };
    }

    /// <summary>How many steps the script holds Space after pressing it: past a held jump's top.</summary>
    private const int HeldFor = 40;

    private static Act Hold(HeroInput keys, int steps, Func<Hero, (double, double)?>? point = null) => new(keys, (_, p) => p.Steps >= steps, point);
    private static Act Until(HeroInput keys, Func<Hero, Progress, bool> done) => new(keys, done);
    private static Act Wait(int steps) => Hold(Still, steps);
    private static Act Jump(int dir = 0) => Hold(new HeroInput(dir, 0, true), 1);
    private static Act Walk(int dir, Func<Hero, bool> until) => new(new HeroInput(dir, 0, false), (h, _) => until(h));
    private static int Column(Hero h) => h.X / Hero.Unit;

    /// <summary>A point <paramref name="right"/> cells right of the hero's middle and <paramref name="below"/> under it.</summary>
    private static Func<Hero, (double, double)?> Beside(double right, double below) =>
        h => ((double)h.X / Hero.Unit + right, (double)h.Middle.Y / Hero.Unit + below);

    /// <summary>E on the cell at (<paramref name="x"/>, <paramref name="y"/>), then a wait while the host runs it.</summary>
    private static Act Press(int x, int y) => new(Still, (_, p) => p.Steps >= 20, _ => (x + 0.5, y + 0.5), Press: true);

    /// <summary>The wreck's script: its cells are the wreck's, as content draws it.</summary>
    public static HeroAutopilot Wreck()
    {
        return new HeroAutopilot(
        [
            Hold(Still, 25, Beside(-3, -2.5)),
            Hold(Still, 25, Beside(-3, 2)),
            Until(new HeroInput(-1, 0, false), (h, p) => p.Stopped(h)),
            Press(45, 21),
            Until(new HeroInput(-1, 0, false), (h, p) => h.Anchor != null && p.Stopped(h)),
            Hold(new HeroInput(-1, -1, false), 40),
            Until(new HeroInput(0, 1, false), (h, _) => h.Y >= 23 * Hero.Unit),
            Walk(1, h => Column(h) >= 49),
            Wait(20),
            Jump(),
            Wait(45),
            Jump(1),
            Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
            Press(54, 21),
            Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
            Press(57, 21),
            Walk(1, h => h.Anchor != null && Column(h) >= 62),
            Hold(new HeroInput(1, -1, false), 60, Beside(4, -3)),
            Wait(40),
        ]);
    }

    /// <summary>
    /// <c>-- --jetpack</c> on the testbed (0047): still while the jetpack goes onto the back, then through the
    /// doorway to room two and out by the airlock, the inner door shut behind; out in space it thrusts right,
    /// up, coasts, brakes, thrusts toward the pointer, and drifts.
    /// </summary>
    public static HeroAutopilot Jetpack()
    {
        var thrust = new HeroInput(0, 0, false, Sprint: true);
        return new HeroAutopilot(
        [
            Wait(170),
            Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
            Press(17, 6),
            Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
            Press(44, 6),
            Walk(1, h => Column(h) >= 46),
            Press(44, 6),
            Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
            Press(48, 6),
            Walk(1, h => Column(h) >= 50),
            Hold(thrust with { X = 1 }, 40),
            Hold(thrust with { Y = -1 }, 18),
            Wait(50),
            Hold(thrust with { X = -1 }, 30),
            Hold(thrust, 45, Beside(6, 1.5)),
            Wait(240),
        ]);
    }

    /// <summary>
    /// <c>-- --dust</c>: in the testbed, the hero sprints one way and back, jumps running and lands, then jumps on the spot:
    /// its boots' dust and its landings (0075), for recordings.
    /// </summary>
    public static HeroAutopilot Dust()
    {
        var run = new HeroInput(-1, 0, false, Sprint: true);
        return new HeroAutopilot(
        [
            Wait(30),
            Hold(run, 24),
            Hold(run with { Jump = true }, 1),
            Hold(run, 30),
            Until(Still, (h, p) => p.Stopped(h)),
            Jump(),
            Wait(60),
            Hold(run with { X = 1 }, 40),
            Wait(60),
        ]);
    }

    /// <summary>
    /// <c>-- --fly</c> (<c>FarDemo</c>): floating out past the stream with a jetpack worn, it waits while
    /// the far field spawns round it, then flies right, up and back left through it, waiting between.
    /// </summary>
    public static HeroAutopilot Fly()
    {
        var thrust = new HeroInput(0, 0, false, Sprint: true);
        return new HeroAutopilot(
        [
            Wait(60 * 25),
            Hold(thrust with { X = 1 }, 60 * 4),
            Wait(60 * 8),
            Hold(thrust with { X = 1, Y = -1 }, 60 * 3),
            Wait(60 * 8),
            Hold(thrust with { X = -1 }, 60 * 6),
            Wait(60 * 30),
        ]);
    }

    /// <summary>
    /// <c>-- --aside</c>: a few steps left of the spawn, then still, so a window recording another peer's
    /// hero at the spawn does not draw its own over it.
    /// </summary>
    public static HeroAutopilot Aside() => new([Wait(20), Hold(new HeroInput(-1, 0, false), 25), Wait(1)]);

    /// <summary>
    /// <c>-- --hills --autopilot</c> (<c>HillsDemo</c>, 0069): over the first hill and back walking, then running right
    /// over it, through the valley and up the long ramp, where it falls into the hole at its top, the high plateau's top
    /// at row <paramref name="plateau"/>; it jumps out and runs on over the peak and off the lone ◢'s cliff to the lone
    /// ◣'s closed side; it jumps that, walks up the stair with air under its steps beyond and back down, and on to the ◢'s.
    /// </summary>
    public static HeroAutopilot HillsRun(int plateau)
    {
        var run = new HeroInput(1, 0, false, Sprint: true);
        return new HeroAutopilot(
        [
            Wait(30),
            Walk(1, h => Column(h) >= 17),
            Walk(-1, h => Column(h) <= 3),
            Wait(20),
            Until(run, (h, p) => p.Stopped(h) || Column(h) == 30 && h.Y > plateau * Hero.Unit),
            Until(Still, (h, p) => p.Stopped(h)),
            Wait(20),
            Jump(),
            Hold(new HeroInput(1, 0, false), 40),
            Until(run, (h, p) => p.Stopped(h)),
            Wait(30),
            Jump(1),
            Hold(new HeroInput(1, 0, false), 20),
            Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
            Wait(30),
            Until(new HeroInput(-1, 0, false), (h, p) => p.Stopped(h)),
            Wait(60),
        ]);
    }

    /// <summary>
    /// <c>--strip</c>'s script (0076): beamed down into the shelter at the cliff's foot, out of it, a jump onto the cliff
    /// five cells up that only a planet's lower gravity allows, over the hills and down the shaft into the dark cave, to
    /// its far wall.
    /// </summary>
    public static HeroAutopilot PlanetRun() => new(
    [
        Wait(90),
        Walk(-1, h => Column(h) <= 27),
        Jump(1),
        Hold(new HeroInput(1, 0, false), 90),
        Walk(1, h => Column(h) >= 71),
        Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
        Wait(120),
    ]);

    /// <summary>
    /// <c>--planet --autopilot</c>: a sprint <paramref name="cells"/> to the right across the planet's surface and back, jumping
    /// whatever stops it, to measure what running past new ground costs a frame (<c>--frames</c>).
    /// </summary>
    public static HeroAutopilot Sprint(int cells)
    {
        int? from = null;
        int legSteps = 0;
        var acts = new List<Act> { Wait(120) };
        foreach (int dir in (int[])[1, -1])
        {
            // A leg ends at its column, or stuck a long while at a cliff too high to jump.
            bool told = false;
            bool Done(Hero h)
            {
                int start = from ??= Column(h);
                bool done = (dir > 0 ? Column(h) >= start + cells : Column(h) <= start) || legSteps > cells * 12;
                if (done && !told) Godot.GD.Print($"sprint: leg {dir} ended at column {Column(h)}, row {h.Y / Hero.Unit}, from {start}, {legSteps} steps");
                told |= done;
                return done;
            }
            var run = new HeroInput(dir, 0, false, Sprint: true);
            acts.Add(new Act(Still, (_, _) => (legSteps = 0) == 0));
            for (int i = 0; i < cells; i++)
            {
                acts.Add(Until(run, (h, p) => ++legSteps > 0 && (Done(h) || p.Stopped(h))));
                acts.Add(new Act(run with { Jump = true }, (h, p) => Done(h) || p.Steps >= 1));
                acts.Add(new Act(run, (h, p) => ++legSteps > 0 && (Done(h) || p.Steps >= 10)));
            }
        }
        acts.Add(Wait(60));
        return new HeroAutopilot(acts);
    }

    /// <summary>
    /// Where <see cref="Sprint"/> starts: the cell over the ground at the start of the longest run of columns whose ground steps
    /// up or down at most four cells from one to the next, so a sprint there is never caught in a pit.
    /// </summary>
    public static int SprintStart(Stillwake.Sim.Grid.IGridQueries grid)
    {
        int w = grid.Width;
        int Top(int x)
        {
            for (int y = 0; y < grid.Height; y++)
                if (grid.StructureAt(y * w + x) != Stillwake.Sim.Grid.Structure.Empty) return y;
            return grid.Height;
        }
        int best = 0, bestLength = 0, start = 0, last = Top(0);
        for (int x = 1; x < w; x++)
        {
            int top = Top(x);
            if (Math.Abs(top - last) > 4) start = x;
            if (x - start > bestLength) (best, bestLength) = (start, x - start);
            last = top;
        }
        return (Top(best + 2) - 1) * w + best + 2;
    }

    /// <summary><c>--slopes</c>'s script: into the corridor's far corner, under its sloped ceiling, and stand there.</summary>
    public static HeroAutopilot SlopesRun() => new([Wait(150), Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)), Wait(1)]);

    /// <summary>
    /// The <see cref="Hall"/>'s script (0090), its hands empty (the hotbar's empty third slot): a sprint started, reversed
    /// and stopped, a tap and a held jump, the eight-cell floor walked off, the twenty-one-cell one walked off, then the drill
    /// taken out and held aimed ahead through a jump on the spot and a running one.
    /// </summary>
    public static HeroAutopilot PosesRun(int wait = 40)
    {
        var run = new HeroInput(1, 0, false, Sprint: true);
        var up = new HeroInput(0, -1, false);
        var ahead = Beside(2.5, 0.4);
        return new HeroAutopilot(
        [
            new(Still, (_, p) => p.Steps >= wait, Slot: 3),
            Hold(run, 36),
            Hold(run with { X = -1 }, 40),
            Until(Still, (h, p) => p.Stopped(h)),
            Wait(30),
            Jump(),
            new(Still, (_, p) => p.Steps >= 50, LetsGo: true),
            Jump(),
            Wait(80),
            Walk(-1, h => Column(h) <= 8),
            Until(up, (h, p) => p.Steps > 2 && !h.Climbing),
            Walk(1, h => Column(h) >= 16),
            Until(Still, (h, p) => p.Stopped(h)),
            Wait(30),
            Walk(1, h => Column(h) >= 32),
            Until(up, (h, p) => p.Steps > 2 && !h.Climbing),
            Walk(1, h => Column(h) >= 42),
            Until(Still, (h, p) => p.Stopped(h)),
            Wait(40),
            new(Still, (_, p) => p.Steps >= 30, ahead, Slot: 2),
            new(new HeroInput(0, 0, true), (_, p) => p.Steps >= 1, ahead),
            Hold(Still, 80, ahead),
            new(run with { X = -1, Jump = true }, (_, p) => p.Steps >= 1, Beside(-2.5, 0.4)),
            Hold(run with { X = -1 }, 50, Beside(-2.5, 0.4)),
            Hold(Still, 40, Beside(-2.5, 0.4)),
        ])
        { _trace = true };
    }

    /// <summary>
    /// The <see cref="Ladders"/> script (0092): a run at the ladder with up held, grabbed on the way and climbed, pulled up through
    /// the hatch; back down, slid the rest of the way with Shift; a running jump over the pit, caught on its ladder mid-air;
    /// up it a little, and a leap off it to the side onto the floor beyond.
    /// </summary>
    public static HeroAutopilot LaddersRun()
    {
        var up = new HeroInput(0, -1, false);
        return new HeroAutopilot(
        [
            Wait(40),
            Until(new HeroInput(1, -1, false, Sprint: true), (h, _) => h.Climbing),
            Until(up, (h, p) => p.Steps > 2 && !h.Climbing),
            Wait(40),
            Until(new HeroInput(0, 1, false), (h, _) => h.Y > 12 * Hero.Unit),
            Until(new HeroInput(0, 1, false, Sprint: true), (h, p) => p.Stopped(h)),
            Wait(30),
            Walk(1, h => Column(h) >= 12),
            Hold(new HeroInput(1, 0, false, Sprint: true), 6),
            Hold(new HeroInput(1, -1, true, Sprint: true), 1),
            Until(new HeroInput(1, -1, false, Sprint: true), (h, p) => h.Climbing || p.Steps > 90),
            Hold(up, 40),
            Wait(30),
            Hold(new HeroInput(1, 0, true), 1),
            Hold(new HeroInput(1, 0, false), 70),
            Wait(40),
        ])
        { _trace = true };
    }

    /// <summary>
    /// The climb out of a spire (0095, <c>--ground=spire-climb --autopilot</c>): from its chamber floor two columns left to the
    /// ladder's foot, a jump at it with up held, up it to the room's floor and out left through the plug the demo dug.
    /// </summary>
    public static HeroAutopilot SpireClimb()
    {
        var up = new HeroInput(0, -1, false);
        int? from = null;
        return new HeroAutopilot(
        [
            Wait(240),
            Until(new HeroInput(-1, 0, false), (h, _) => (from ??= Column(h)) - Column(h) >= 2 && h.X % Hero.Unit is > 350 and < 650),
            Wait(20),
            Hold(new HeroInput(0, -1, true), 1),
            Until(up, (h, p) => h.Climbing || p.Steps > 60),
            Until(up, (h, p) => p.Steps > 2 && !h.Climbing),
            Wait(30),
            Hold(new HeroInput(-1, 0, false), 50),
            Wait(60),
        ])
        { _trace = true };
    }

    /// <summary>
    /// The <see cref="Ledges"/> script (0092): a held jump at the four-cell ledge, stepped up onto; off its far side, and a held
    /// jump at the five-cell one, falling short.
    /// </summary>
    public static HeroAutopilot LedgesRun() => new(
    [
        Wait(40),
        Walk(1, h => Column(h) >= 6),
        Jump(1),
        Hold(new HeroInput(1, 0, false), 70),
        Walk(1, h => Column(h) >= 15 && h.Vy == 0 && h.Y > 10 * Hero.Unit),
        Walk(1, h => Column(h) >= 18),
        Jump(1),
        Hold(new HeroInput(1, 0, false), 80),
        Wait(40),
    ])
    { _trace = true };

    /// <summary>
    /// <c>--strip --ladders</c>: the planet's test strip's ladder (0092): out of the shelter to the shaft, down its ladder sliding,
    /// up again and pulled out over its top onto the ground, under the planet's gravity.
    /// </summary>
    public static HeroAutopilot StripLaddersRun() => new(
    [
        Wait(90),
        Walk(-1, h => Column(h) <= 27),
        Jump(1),
        Hold(new HeroInput(1, 0, false), 90),
        Walk(1, h => Column(h) >= 70),
        Until(new HeroInput(0, 1, false), (h, p) => p.Steps > 12),
        Until(new HeroInput(0, 1, false, Sprint: true), (h, p) => p.Stopped(h)),
        Wait(30),
        Until(new HeroInput(0, -1, false), (h, p) => p.Steps > 2 && !h.Climbing),
        Wait(30),
        Walk(-1, h => Column(h) <= 66),
        Wait(30),
    ])
    { _trace = true };

    /// <summary>The <see cref="Gym"/>'s script.</summary>
    public static HeroAutopilot GymRun()
    {
        return new HeroAutopilot(
        [
            Wait(15),
            Walk(1, h => Column(h) == 5),
            Until(new HeroInput(0, -1, false), (h, p) => p.Steps > 2 && !h.Climbing),
            Walk(1, h => Column(h) >= 12),
            Until(Still, (h, p) => p.Stopped(h)),
            Wait(20),
            Walk(-1, h => Column(h) <= 8),
            Jump(),
            Wait(70),
            Walk(-1, h => Column(h) <= 5),
            Until(new HeroInput(0, 1, false), (h, p) => p.Stopped(h)),
            Walk(-1, h => Column(h) <= 2),
            Jump(1),
            Wait(60),
        ]);
    }

    /// <summary>
    /// <c>-- --feel</c> (0089), tracing every step: a tap jump, a held one, a run started and let go, then a jump three steps
    /// after walking off a ledge. With <c>--hills</c> (<paramref name="hills"/>) the ledge is the floor off the ladder's top;
    /// with <c>--strip</c> (<paramref name="strip"/>), out of the shelter, the cliff's far step, jumped onto first; else none.
    /// </summary>
    public static HeroAutopilot Feel(bool hills, bool strip)
    {
        int run = hills ? 1 : -1;
        var acts = new List<Act> { Wait(hills ? 30 : 90) };
        if (strip) acts.Add(Walk(-1, h => Column(h) <= 27));
        acts.AddRange(
        [
            Wait(20),
            Jump(),
            new(Still, (_, p) => p.Steps >= 70, LetsGo: true),
            Jump(),
            Wait(90),
            Hold(new HeroInput(run, 0, false, Sprint: true), 15),
            Wait(40),
        ]);
        var off = hills ? -1 : 1;
        if (hills)
            acts.AddRange(
            [
                Walk(1, h => Column(h) == 24),
                Until(new HeroInput(0, -1, false), (h, p) => p.Steps > 2 && !h.Climbing),
                Wait(20),
            ]);
        if (strip) acts.AddRange([Walk(1, h => Column(h) >= 27), Jump(1), Hold(new HeroInput(1, 0, false), 90)]);
        if (hills || strip)
            acts.AddRange(
            [
                Until(new HeroInput(off, 0, false), (h, _) => h.Vy > 0),
                Hold(new HeroInput(off, 0, false), 3),
                Jump(off),
                Hold(new HeroInput(off, 0, false), 60),
            ]);
        acts.Add(Wait(60));
        return new HeroAutopilot(acts) { _trace = true };
    }
}
