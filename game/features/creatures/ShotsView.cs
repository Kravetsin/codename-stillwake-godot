using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The sim's shots (shots-are-the-sims), every peer's but this player's own, which its hero's hands fly ahead of the sim's: each
/// drawn a tick behind, between where it was and is, heard fired where it first shows, and its end shown and heard as what it met
/// (<see cref="ShotEnds"/>).
/// </summary>
public partial class ShotsView : Node2D, ILightSource
{
    private readonly Registry _m;
    private readonly Func<double> _progress;
    private readonly Func<int> _own, _tick;
    private readonly ShotStreaks _streaks = new();
    private readonly Voices _fired = new(S.World, voices: 3), _ends = new(S.World, voices: 4);

    /// <summary>Each shot drawn, by id: where it was a tick ago and is now.</summary>
    private readonly Dictionary<int, (ShotMotion Was, ShotMotion Now)> _seen = [];
    private int _shownTick = -1;
    private readonly List<LightSource> _lights = [];

    /// <summary>The lights of the bolts in flight this frame.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    public ShotsView(Registry m, Func<double> progress, Func<int> own, Func<int> tick)
    {
        (_m, _progress, _own, _tick) = (m, progress, own, tick);
        AddChild(_streaks);
        AddChild(_fired);
        AddChild(_ends);
    }

    public override void _Process(double delta)
    {
        var creatures = _m.Creatures;
        int tick = _tick();
        if (tick != _shownTick)
        {
            _shownTick = tick;
            Ticked(creatures, Ref.Hero(_own()));
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
        // A bolt lights its way, a little cyan, flickering as it crackles; a nail gives none.
        _lights.Clear();
        foreach (var shot in creatures.Shots)
            if (shot.Projectile == Content.Shots.Bolt.Id && _seen.TryGetValue(shot.Id, out var lit))
            {
                var (at, _) = ShotStreaks.Between(lit.Was, lit.Now, progress);
                _lights.Add(new LightSource(at.X, at.Y, (0.55f, 0.9f, 1f), 0.8f + 0.2f * (float)Random.Shared.NextDouble(), 4, 0.4f, 1));
            }
    }

    /// <summary>A tick of the world seen: the shots that ended shown ending, those new heard fired, each moved on a step.</summary>
    private void Ticked(CreaturesModule creatures, Ref own)
    {
        foreach (var end in creatures.Ended)
            if (_seen.Remove(end.Id, out var flew))
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
                _fired.Play(S.FiredOf(shot.Projectile), new Vector2(motion.X, motion.Y) * (float)(Camera.Cell / 1000.0));
            }
        }
        foreach (var id in new List<int>(_seen.Keys))
            if (!flying.Contains(id)) _seen.Remove(id);
    }
}
