using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The sim's shots (shots-are-the-sims), every peer's but this player's own, which its hero's hands fly ahead of the sim's: each
/// drawn a tick behind, between where it was and is, heard fired where it first shows, and its end shown and heard as what it met
/// (<see cref="ShotEnds"/>). And the creatures' arcs (2026-10-27-sentry-arc), each shown once the tick it struck: an electric arc
/// as the relic's, a flash and sparks where it struck, heard where it left and where it struck.
/// </summary>
public partial class ShotsView : Node2D
{
    private readonly Registry _m;
    private readonly Func<double> _progress;
    private readonly Func<int> _own, _tick;
    private readonly ShotStreaks _streaks = new();
    private readonly Voices _fired = new(S.World, voices: 3), _ends = new(S.World, voices: 4);
    // Arcs strike in space, where the World bus is silent: heard on the Suit bus, as the sentry is.
    private readonly Voices _arcs = new(S.Suit, voices: 4);

    /// <summary>Each shot drawn, by id: where it was a tick ago and is now.</summary>
    private readonly Dictionary<int, (ShotMotion Was, ShotMotion Now)> _seen = [];
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

    public ShotsView(Registry m, Func<double> progress, Func<int> own, Func<int> tick)
    {
        (_m, _progress, _own, _tick) = (m, progress, own, tick);
        AddChild(_streaks);
        AddChild(_fired);
        AddChild(_ends);
        AddChild(_arcs);
    }

    public override void _Process(double delta)
    {
        var creatures = _m.Creatures;
        int tick = _tick();
        if (tick != _shownTick)
        {
            _shownTick = tick;
            Ticked(creatures, Ref.Hero(_own()));
            Struck(creatures.Arcs);
        }
        double progress = _progress();
        var streaks = new List<(Vector2, Vector2, ShotLook)>();
        foreach (var shot in creatures.Shots)
            if (_seen.TryGetValue(shot.Id, out var flight))
            {
                var (at, way) = ShotStreaks.Between(flight.Was, flight.Now, progress);
                streaks.Add((at, way, ShotLooks.Of(shot.Projectile)));
            }
        _streaks.Show(streaks);
    }

    /// <summary>A tick of the world seen: the shots that ended shown ending, those new heard fired, each moved on a step.</summary>
    private void Ticked(CreaturesModule creatures, Ref own)
    {
        foreach (var end in creatures.Ended)
            // A shot that ran out shows nothing: it met nothing.
            if (_seen.Remove(end.Id, out var flew) && end.Met)
                ShotEnds.Show(_m, _ends, end.Gun, end.Projectile, end.Cell, (flew.Now.X / 1000.0, flew.Now.Y / 1000.0));
        var flying = new HashSet<int>();
        foreach (var shot in creatures.Shots)
        {
            if (shot.By == own) continue;
            flying.Add(shot.Id);
            var motion = new ShotMotion(shot.X, shot.Y, shot.VX, shot.VY, shot.Left);
            if (_seen.TryGetValue(shot.Id, out var was)) _seen[shot.Id] = (was.Now, motion);
            else
            {
                // Seen first a step out of the muzzle: drawn from where it was fired, a step back.
                _seen[shot.Id] = (motion with { X = motion.X - motion.VX, Y = motion.Y - motion.VY }, motion);
                _fired.Play(S.NailShot, new Vector2(motion.X, motion.Y) * (float)(Camera.Cell / 1000.0));
            }
        }
        foreach (var id in new List<int>(_seen.Keys))
            if (!flying.Contains(id)) _seen.Remove(id);
    }

    /// <summary>Each arc new this tick shown striking: its arc and a thinner two along it, a flash and sparks where it struck.</summary>
    private void Struck(IReadOnlyList<Arc> arcs)
    {
        _arcsShown.RemoveWhere(id => !Lingers(arcs, id));
        float k = (float)(Camera.Cell / 1000.0);
        foreach (var arc in arcs)
        {
            if (!_arcsShown.Add(arc.Id)) continue;
            var (from, to) = (new Vector2(arc.X0, arc.Y0) * k, new Vector2(arc.X1, arc.Y1) * k);
            Chain(from, to, 9, 1.3, 1);
            Chain(from, to, 6, 0.8, 0.5);
            Flares.Shared.Flash(to.X, to.Y, Cyan, 1.0 * Fx.Glow, 10, CreatureLooks.LightKeep, 0.8);
            Flares.Shared.Flash(from.X, from.Y, Cyan, 0.6 * Fx.Glow, 6, CreatureLooks.LightKeep, 0.8);
            Flares.Shared.Glint(to.X, to.Y, 5, (232, 251, 255));
            for (int i = 0; i < 12; i++)
            {
                double a = Between(0, Math.Tau), s = Between(30, 110);
                Glims.Shared.Add(Spark, to.X, to.Y, Math.Cos(a) * s, Math.Sin(a) * s);
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
            Bolts.Shared.Add(() => (p.X, p.Y, q.X, q.Y, 0), life, intensity, width, branchy: true);
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
