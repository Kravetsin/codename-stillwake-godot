using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The sim's shots (shots-are-the-sims), every peer's but a guest's own, which its hero's hands fly ahead of the sim's
/// (<see cref="ShotDraws"/>): each drawn a tick behind, between where it was and is, heard fired where it first shows but the
/// host's own, heard at the click, and every hit shown and heard as what it met (<see cref="ShotEnds"/>, 2026-10-27-shots-pierce). A
/// charge's burst (blasting-charge) shown and heard wherever its shot ends, whoever threw it. And the creatures' arcs (2026-10-27-sentry-arc), each shown once the tick it struck: an electric arc
/// as the relic's, a flash and sparks where it struck, heard where it left and where it struck.
/// </summary>
public partial class ShotsView : Node2D
{
    private readonly Registry _m;
    private readonly Func<double> _progress;
    private readonly Func<int> _own, _tick;
    private readonly Func<bool> _hosts;
    private readonly ShotStreaks _streaks = new();
    private readonly Voices _fired = new(S.World, voices: 3), _ends = new(S.World, voices: 4);
    // The host's own hits are its suit's, heard in space too, as a guest's copy's are.
    private readonly Voices _ownEnds = new(S.Suit, voices: 3);
    // Arcs strike in space, where the World bus is silent: heard on the Suit bus, as the sentry is.
    private readonly Voices _arcs = new(S.Suit, voices: 4);

    /// <summary>Each shot drawn, by id: where it was a tick ago and is now.</summary>
    private readonly Dictionary<int, (ShotMotion Was, ShotMotion Now)> _seen = [];
    /// <summary>
    /// Each charge in flight, by id: who threw it and the walls round it as the last tick left them, with what they were; those gone
    /// at its burst are what it broke.
    /// </summary>
    private readonly Dictionary<int, (Ref By, List<(int Cell, Spec.Material Wall)> Round)> _charges = [];
    /// <summary>The arcs shown, by id, forgotten once they are gone.</summary>
    private readonly HashSet<int> _arcsShown = [];
    private int _shownTick = -1;

    private static readonly (int R, int G, int B) Cyan = (95, 216, 255);

    /// <summary>The sparks an arc throws where it strikes: the hit's, cyan, weightless, as it strikes in space.</summary>
    private static readonly Luminous Spark = Luminous.HitSpark with
    {
        Ramp = [(244, 254, 255), (160, 232, 255), (85, 214, 255), (40, 110, 200)],
        LifeMs = (250, 600),
        Gravity = 0,
        Halo = 2,
    };

    public ShotsView(Registry m, Func<double> progress, Func<int> own, Func<int> tick, Func<bool> hosts)
    {
        (_m, _progress, _own, _tick, _hosts) = (m, progress, own, tick, hosts);
        AddChild(_streaks);
        AddChild(_fired);
        AddChild(_ends);
        AddChild(_ownEnds);
        AddChild(_arcs);
    }

    public override void _Process(double delta)
    {
        var creatures = _m.Creatures;
        int tick = _tick();
        if (tick != _shownTick)
        {
            _shownTick = tick;
            Ticked(creatures, _own(), _hosts());
            Struck(creatures.Arcs);
        }
        double progress = _progress();
        var streaks = new List<(Vector2, Vector2, ShotLook)>();
        foreach (var shot in creatures.Shots)
            if (_seen.TryGetValue(shot.Id, out var flight))
            {
                var (at, way) = ShotStreaks.Between(flight.Was, flight.Now, progress);
                streaks.Add((at, way, ShotLooks.Of(shot.Projectile, shot.Gun)));
            }
        _streaks.Show(streaks);
    }

    /// <summary>
    /// A tick of the world seen: every hit of a shot drawn here shown, the own hero's on its suit; one born and ended this tick
    /// heard fired where it left, another's; those new heard fired, each moved on a step.
    /// </summary>
    private void Ticked(CreaturesModule creatures, int own, bool hosts)
    {
        var mine = Ref.Hero(own);
        int bornAndEnded = -1;
        foreach (var hit in creatures.Hits)
        {
            if (!ShotDraws.DrawsSims(hit.By, own, hosts)) continue;
            ShotEnds.Show(_m, hit.By == mine ? _ownEnds : _ends, hit);
            if (hit.By != mine && hit.Shot != bornAndEnded && !_seen.ContainsKey(hit.Shot) && !Flying(creatures, hit.Shot))
            {
                bornAndEnded = hit.Shot;
                _fired.Play(S.FiredBy(hit.Gun), new Vector2(hit.From.X, hit.From.Y) * (float)(Camera.Cell / 1000.0));
            }
        }
        foreach (var end in creatures.Ended)
            if (end.Burst >= 0) Burst(end, own);
        Round(creatures);
        var flying = new HashSet<int>();
        foreach (var shot in creatures.Shots)
        {
            if (!ShotDraws.DrawsSims(shot.By, own, hosts)) continue;
            flying.Add(shot.Id);
            var motion = new ShotMotion(shot.X, shot.Y, shot.VX, shot.VY, shot.Left);
            if (_seen.TryGetValue(shot.Id, out var was)) _seen[shot.Id] = (was.Now, motion);
            else
            {
                // Seen first a step out of the muzzle: drawn from where it was fired, a step back.
                _seen[shot.Id] = (motion with { X = motion.X - motion.VX, Y = motion.Y - motion.VY }, motion);
                if (shot.By != Ref.Hero(own)) _fired.Play(S.FiredBy(shot.Gun), new Vector2(motion.X, motion.Y) * (float)(Camera.Cell / 1000.0));
            }
        }
        foreach (var id in new List<int>(_seen.Keys))
            if (!flying.Contains(id)) _seen.Remove(id);
    }

    /// <summary>The cells round a charge kept, either way of its cell: its reach, and a step it may fly before it bursts.</summary>
    private static readonly int RoundReach = Spec.Blasting.Charge.Reach + 2;

    /// <summary>Every charge in flight's walls round it, as this tick leaves them; a charge gone, forgotten.</summary>
    private void Round(CreaturesModule creatures)
    {
        var grid = _m.Grid;
        foreach (var id in new List<int>(_charges.Keys))
            if (!Flying(creatures, id)) _charges.Remove(id);
        foreach (var shot in creatures.Shots)
        {
            if (Spec.Shots.Get(shot.Projectile).Bursts == null) continue;
            if (!_charges.TryGetValue(shot.Id, out var kept)) _charges[shot.Id] = kept = (shot.By, []);
            kept.Round.Clear();
            int cx = shot.X / CreaturesModule.Unit, cy = shot.Y / CreaturesModule.Unit;
            for (int y = Math.Max(0, cy - RoundReach); y <= Math.Min(grid.Height - 1, cy + RoundReach); y++)
                for (int x = Math.Max(0, cx - RoundReach); x <= Math.Min(grid.Width - 1, cx + RoundReach); x++)
                    if (grid.WallAt(y * grid.Width + x) is { } wall) kept.Round.Add((y * grid.Width + x, wall));
        }
    }

    private static readonly (int R, int G, int B) White = (244, 242, 234);
    /// <summary>The burst's dust, grey, thrown out in a ring; a cell's chips its ground's colour.</summary>
    private static readonly Luminous BurstDust = Luminous.BreakDust with { Ramp = [(140, 138, 132), (94, 92, 88)], Drag = 2.2 },
        BurstCloud = Luminous.Vapour with { Ramp = [(140, 138, 132)], Alpha = 0.18, Drag = 1.6 },
        BurstChip = Luminous.Chip with { Gravity = 420, Bounce = 0.25, Size = 1.6, Low = true };

    /// <summary>
    /// The burst's sparks: a spray of hot streaks thrown every way, curling as they slow and arcing down where there is weight; a few
    /// long ones flung far; burning sulfur drifting after, rising slowly as it dims. Counts are once a burst, a rare thing.
    /// </summary>
    private static readonly Luminous BurstSpark = Luminous.BreakSpark with { LifeMs = (500, 1300), Drag = 1.1, Gravity = 260, Bounce = 0.3 },
        BurstStreak = Luminous.ShardEmber with { LifeMs = (700, 1200), Drag = 0.6, Gravity = 200, Bounce = 0.3, Bright = 1 },
        BurstEmber = Luminous.Ember with { LifeMs = (1400, 2600) };

    private const int BurstSparks = 90, BurstStreaks = 14, BurstEmbers = 22;
    private static readonly (int R, int G, int B) Afterglow = (255, 176, 84);

    /// <summary>
    /// A charge's burst (blasting-charge), the user's pick: a white flash, a grey ring of dust thrown out as wide as what broke, and
    /// out of every wall it broke chips of its ground flung away from the burst, falling where there is weight; heard as far as a
    /// wreck breaks and farther, the own one through the suit.
    /// </summary>
    private void Burst(ShotEnd end, int own)
    {
        var grid = _m.Grid;
        double cell = Camera.Cell, x = (end.Burst % grid.Width + 0.5) * cell, y = (end.Burst / grid.Width + 0.5) * cell;
        bool afloat = _m.Rooms.Afloat(end.Burst);
        var stage = Stages.Of(Layers.Effects);
        var glims = stage.Glims;
        double farthest = 2 * cell;
        Ref? by = null;
        if (_charges.Remove(end.Id, out var kept))
        {
            by = kept.By;
            foreach (var (at, wall) in kept.Round)
            {
                if (grid.WallAt(at) != null) continue;
                double wx = (at % grid.Width + 0.5) * cell, wy = (at / grid.Width + 0.5) * cell, dx = wx - x, dy = wy - y;
                double far = Math.Sqrt(dx * dx + dy * dy);
                farthest = Math.Max(farthest, far);
                double away = far > 0 ? Math.Atan2(dy, dx) : glims.Between(0, Math.Tau);
                var chip = BurstChip with
                {
                    Ramp = GroundLooks.Of.TryGetValue(wall.Id, out var look) ? [look.Fleck, look.Colour] : BurstChip.Ramp,
                    Gravity = afloat ? 0 : BurstChip.Gravity,
                };
                for (int i = 0; i < 2; i++)
                {
                    double a = away + glims.Between(-0.7, 0.7), s = glims.Between(60, 170);
                    glims.Add(chip, wx + glims.Between(-3, 3), wy + glims.Between(-3, 3), Math.Cos(a) * s, Math.Sin(a) * s - (afloat ? 0 : 60), size: glims.Between(1, 2.4));
                }
                if (glims.Between(0, 1) < 0.5)
                    glims.Add(BurstDust, wx, wy, Math.Cos(away) * glims.Between(10, 40), Math.Sin(away) * glims.Between(10, 40), bright: glims.Between(0.6, 0.9));
            }
        }
        double ring = farthest + cell;
        for (int i = 0; i < 40; i++)
        {
            double a = glims.Between(0, Math.Tau), s = ring * glims.Between(1.6, 2.4);
            glims.Add(BurstDust, x + Math.Cos(a) * 4, y + Math.Sin(a) * 4, Math.Cos(a) * s, Math.Sin(a) * s, bright: glims.Between(0.7, 1), size: glims.Between(0.7, 1.2));
        }
        for (int i = 0; i < 8; i++)
        {
            double a = glims.Between(0, Math.Tau), s = ring * glims.Between(0.5, 0.9);
            glims.Add(BurstCloud, x, y, Math.Cos(a) * s, Math.Sin(a) * s, bright: 0.7, size: glims.Between(1.6, 2.4));
        }
        Sparks(glims, x, y, ring, afloat, Floor(end.Burst));
        stage.Flares.Glint(x, y, 1.5 * cell, (255, 255, 255));
        stage.Flares.Bloom(x, y, 4 * cell, White, 0.9 * Fx.Glow);
        stage.Flares.Shock(x, y, 4, ring, 450, (200, 198, 190), 0.5 * Fx.Glow, 0.5);
        stage.Flares.Flash(x, y, White, 1.4 * Fx.Glow, 20, 0.6f, 0.8);
        // The sulfur burning on: a warm light lingering in the dark after the white is gone.
        stage.Flares.Flash(x, y, Afterglow, 0.7 * Fx.Glow, 16, 0.5f, 0.96);
        (by == Ref.Hero(own) ? _ownEnds : _ends).Play(S.ChargeBurst, new Vector2((float)x, (float)y));
    }

    /// <summary>The sparks, streaks and embers of a burst at (<paramref name="x"/>, <paramref name="y"/>), thrown about as far as <paramref name="ring"/>; weightless afloat, else bouncing on <paramref name="floor"/>.</summary>
    private static void Sparks(Glims glims, double x, double y, double ring, bool afloat, double? floor)
    {
        var (spark, streak, ember) = afloat
            ? (BurstSpark with { Gravity = 0 }, BurstStreak with { Gravity = 0 }, BurstEmber with { Gravity = 0 })
            : (BurstSpark, BurstStreak, BurstEmber);
        double reach = Math.Max(ring, 4 * Camera.Cell);
        for (int i = 0; i < Math.Round(BurstSparks * Fx.Count); i++)
        {
            double a = glims.Between(0, Math.Tau), s = reach * glims.Between(1.2, 3.2);
            glims.Add(spark, x + glims.Between(-3, 3), y + glims.Between(-3, 3), Math.Cos(a) * s, Math.Sin(a) * s, floor, size: glims.Between(0.8, 1.4));
        }
        for (int i = 0; i < Math.Round(BurstStreaks * Fx.Count); i++)
        {
            double a = glims.Between(0, Math.Tau), s = reach * glims.Between(3, 4.5);
            glims.Add(streak, x, y, Math.Cos(a) * s, Math.Sin(a) * s, floor, size: glims.Between(1.2, 1.8));
        }
        for (int i = 0; i < Math.Round(BurstEmbers * Fx.Count); i++)
        {
            double a = glims.Between(0, Math.Tau), s = reach * glims.Between(0.15, 0.6);
            glims.Add(ember, x + glims.Between(-6, 6), y + glims.Between(-6, 6), Math.Cos(a) * s, Math.Sin(a) * s, size: glims.Between(0.6, 1.1));
        }
    }

    /// <summary>The top of the first solid cell under <paramref name="cell"/>, in pixels of the world, the crater's floor sparks bounce on; none within reach.</summary>
    private double? Floor(int cell)
    {
        var grid = _m.Grid;
        for (int r = cell / grid.Width; r < Math.Min(grid.Height, cell / grid.Width + Spec.Blasting.Charge.Reach + 2); r++)
            if (grid.Solid(r * grid.Width + cell % grid.Width)) return r * Camera.Cell;
        return null;
    }

    private static bool Flying(CreaturesModule creatures, int id)
    {
        foreach (var shot in creatures.Shots)
            if (shot.Id == id) return true;
        return false;
    }

    /// <summary>Each arc new this tick shown striking: its arc, a flash and sparks where it struck.</summary>
    private void Struck(IReadOnlyList<Arc> arcs)
    {
        _arcsShown.RemoveWhere(id => !Lingers(arcs, id));
        float k = (float)(Camera.Cell / 1000.0);
        foreach (var arc in arcs)
        {
            // A capback's glob is no electric arc: LurkerFxView flies it.
            if (!_arcsShown.Add(arc.Id) || arc.Projectile == Stillwake.Content.Shots.Glob.Id) continue;
            var (from, to) = (new Vector2(arc.X0, arc.Y0) * k, new Vector2(arc.X1, arc.Y1) * k);
            Chain(from, to, 9, 1.2, 1);
            Stages.Of(Layers.Effects).Flares.Flash(to.X, to.Y, Cyan, 1.0 * Fx.Glow, 10, CreatureLooks.LightKeep, 0.8);
            Stages.Of(Layers.Effects).Flares.Flash(from.X, from.Y, Cyan, 0.6 * Fx.Glow, 6, CreatureLooks.LightKeep, 0.8);
            Stages.Of(Layers.Effects).Flares.Glint(to.X, to.Y, 5, (232, 251, 255));
            for (int i = 0; i < 12; i++)
            {
                double a = Between(0, Math.Tau), s = Between(30, 110);
                Stages.Of(Layers.Effects).Glims.Add(Spark, to.X, to.Y, Math.Cos(a) * s, Math.Sin(a) * s);
            }
            _arcs.Play(S.ArcStruck, from);
            _arcs.Play(S.ArcHit, to);
        }
    }

    /// <summary>The longest link of an arc's chain, in pixels of the world: an arc's folds stray by a share of its length.</summary>
    private const float Link = 40;

    /// <summary>
    /// An arc from <paramref name="from"/> to <paramref name="to"/> as a chain of short ones, their joints a little off the line:
    /// one long arc's folds would stray far, by a share of its length, and hide where it struck.
    /// </summary>
    private static void Chain(Vector2 from, Vector2 to, int life, double intensity, double width)
    {
        var way = to - from;
        int links = Math.Max(1, (int)Math.Ceiling(way.Length() / Link));
        var across = way.Length() > 0 ? new Vector2(-way.Y, way.X).Normalized() : Vector2.Zero;
        var a = from;
        for (int i = 1; i <= links; i++)
        {
            var b = i == links ? to : from + way * i / links + across * (float)Between(-3, 3);
            var (p, q) = (a, b);
            Stages.Of(Layers.Effects).Bolts.Add(() => (p.X, p.Y, q.X, q.Y, 0), life, intensity, width, branchy: true);
            a = b;
        }
    }

    private static bool Lingers(IReadOnlyList<Arc> arcs, int id)
    {
        foreach (var arc in arcs)
            if (arc.Id == id) return true;
        return false;
    }

    private static double Between(double from, double to) => from + (to - from) * Random.Shared.NextDouble();
}
