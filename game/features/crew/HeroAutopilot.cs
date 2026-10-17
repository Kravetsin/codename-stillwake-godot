using System;
using System.Collections.Generic;
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

    /// <summary>One act: the keys held until <see cref="Done"/> says so, where the pointer is meanwhile, and whether it starts with E.</summary>
    private sealed record Act(HeroInput Keys, Func<Hero, Progress, bool> Done, Func<Hero, (double X, double Y)?>? Point = null, bool Press = false);

    /// <summary>How far into an act the script is: its steps so far, and the hero a step ago.</summary>
    private readonly record struct Progress(int Steps, Hero? Last)
    {
        /// <summary>Whether the hero stood still the last step, a few steps in: stopped by a wall or a door.</summary>
        public bool Stopped(Hero hero) => Steps > 2 && Last is Hero last && last.X == hero.X && last.Y == hero.Y;
    }

    private static readonly HeroInput Still = new(0, 0, false);
    private readonly IReadOnlyList<Act> _acts;
    private int _at, _steps;
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

    /// <summary>The keys for the next controller step of <paramref name="hero"/>.</summary>
    public HeroInput Next(Hero hero)
    {
        while (_at < _acts.Count)
        {
            var act = _acts[_at];
            if (_steps == 0 && act.Press) _press = true;
            if (!act.Done(hero, new Progress(_steps, _last))) break;
            _at++;
            _steps = 0;
        }
        _last = hero;
        if (_at >= _acts.Count) return Still;
        _steps++;
        return _acts[_at].Keys;
    }

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
            Press(35, 6),
            Walk(1, h => Column(h) >= 37),
            Press(35, 6),
            Until(new HeroInput(1, 0, false), (h, p) => p.Stopped(h)),
            Press(39, 6),
            Walk(1, h => Column(h) >= 41),
            Hold(thrust with { X = 1 }, 40),
            Hold(thrust with { Y = -1 }, 18),
            Wait(50),
            Hold(thrust with { X = -1 }, 30),
            Hold(thrust, 45, Beside(6, 1.5)),
            Wait(240),
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
}
