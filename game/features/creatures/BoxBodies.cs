using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The placeholder look of a creature with no rig yet (the sentry, 2026-10-26-creatures-are-composed): a dark box its size, one
/// eye lamp lighting round it in its colour — teal guarding, yellow alerted, red and brighter from its crouch on — squashed
/// as it crouches. Struck it throws its sparks, killed a burst of them; a harpoon line it cuts snaps in a spark at the wreck.
/// Drawn between ticks where the sim put it. The art and its sounds come from a pick page.
/// </summary>
public partial class BoxBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    /// <summary>How long a cut line's spark shines.</summary>
    private const double SnapMs = 180;

    private static readonly Color Hull = new(0.2f, 0.22f, 0.26f), Rim = new(0.5f, 0.53f, 0.58f);
    private static readonly Color Guarding = new(0.25f, 0.95f, 0.85f), Alerted = new(1f, 0.82f, 0.2f), Attacking = new(1f, 0.22f, 0.18f);

    private sealed class Shown
    {
        public Vector2 From, To;
        public int Tick = -1, Damage;
        public Color Eye;
        public float Crouch;
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly List<(Vector2 At, double LeftMs)> _snaps = [];
    private readonly Dictionary<int, int> _cuts = new();
    private readonly HashSet<int> _seen = [];
    private readonly List<int> _gone = [];
    private readonly List<(int Id, Vector2 At, Vector2 Size, Color Eye, float Crouch)> _drawn = [];

    /// <summary>The eyes and sparks, unshaded: the world's darkness does not grey a lamp.</summary>
    private readonly Lamps _lamps = new() { Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };

    public override void _Ready() => AddChild(_lamps);

    /// <summary>
    /// A frame of the creatures in <paramref name="boxed"/> (those with no rig), tick <paramref name="tick"/> drawn
    /// <paramref name="progress"/> of the way to the next; what their eyes light goes into <paramref name="lights"/>.
    /// </summary>
    public void Frame(CreaturesModule creatures, IDebrisQueries debris, IReadOnlyList<Creature> boxed, int tick, double progress,
        double elapsedMs, List<LightSource> lights)
    {
        _seen.Clear();
        _drawn.Clear();
        foreach (var c in boxed)
        {
            _seen.Add(c.Id);
            var spec = Spec.Creatures.Get(c.Species);
            var at = new Vector2(c.X, c.Y) / Unit * Cell;
            if (!_shown.TryGetValue(c.Id, out var one)) _shown[c.Id] = one = new Shown { From = at, To = at, Tick = tick, Damage = c.Damage };
            if (one.Tick != tick) (one.From, one.To, one.Tick) = (one.To, at, tick);
            if (c.Damage > one.Damage)
            {
                Sparks(one.To, spec, 0.6);
                one.Damage = c.Damage;
            }
            var eye = EyeOf(c, spec);
            float crouch = c.Act is { Phase: ActPhase.WindUp } ? 1 : 0;
            float ease = (float)(1 - Math.Exp(-elapsedMs / 60));
            one.Eye = one.Eye.Lerp(eye, ease);
            one.Crouch += (crouch - one.Crouch) * ease;
            var drawn = one.From.Lerp(one.To, (float)progress);
            var size = new Vector2(spec.Body.Width, spec.Body.Height) * Cell;
            _drawn.Add((c.Id, drawn, size, one.Eye, one.Crouch));
            bool hot = c.Act != null;
            var lamp = drawn + new Vector2(0, -size.Y * (1 - 0.25f * one.Crouch) * 0.55f);
            lights.Add(new LightSource(lamp.X, lamp.Y, (one.Eye.R, one.Eye.G, one.Eye.B), CreatureLooks.LightStrength * (hot ? 1.6f : 1f),
                CreatureLooks.LightReach, CreatureLooks.LightKeep, 1));
            Snaps(creatures, debris, c);
        }
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            // Gone from the world while in view: killed, a burst of its sparks.
            if (creatures.Creatures.All(c => c.Id != id)) Sparks(_shown[id].To, null, 1);
            _shown.Remove(id);
        }
        for (int i = _snaps.Count - 1; i >= 0; i--)
        {
            var (snapAt, left) = _snaps[i];
            left -= elapsedMs;
            if (left <= 0) _snaps.RemoveAt(i);
            else
            {
                _snaps[i] = (snapAt, left);
                lights.Add(new LightSource(snapAt.X, snapAt.Y, (1f, 0.95f, 0.8f), CreatureLooks.LightStrength * (float)(left / SnapMs), CreatureLooks.LightReach,
                    CreatureLooks.LightKeep, 1));
            }
        }
        _lamps.Show(_drawn, _snaps);
    }

    /// <summary>The colour its eye shows: red from its crouch on, yellow alerted, else teal.</summary>
    private static Color EyeOf(Creature c, Spec.Creatures.Species spec)
    {
        if (c.Act != null) return Attacking;
        return CreaturesModule.Alerted(c) ? Alerted : Guarding;
    }

    /// <summary>A harpoon line <paramref name="c"/> cut since last seen (<see cref="CreaturesModule.LastCut"/>): a spark where it held, on the wreck.</summary>
    private void Snaps(CreaturesModule creatures, IDebrisQueries debris, Creature c)
    {
        int cut = CreaturesModule.LastCut(c);
        if (_cuts.TryGetValue(c.Id, out int was) && cut > was && creatures.ClingsTo(c) is int id && debris.PieceById(id) is { } piece)
            _snaps.Add((new Vector2(piece.X, piece.Y) / Unit * Cell, SnapMs));
        _cuts[c.Id] = cut;
    }

    /// <summary>Another surface, or a load: what was shown was another world's, gone without a spark.</summary>
    public void Reset()
    {
        _shown.Clear();
        _cuts.Clear();
        _snaps.Clear();
        _drawn.Clear();
        _lamps.Show(_drawn, _snaps);
    }

    private static void Sparks(Vector2 at, Spec.Creatures.Species? spec, double share) =>
        HitBurst.Shared.Throw(HitLooks.Of[Impact.Wreck], at.X, at.Y - Cell, 0, -1, share);

    /// <summary>
    /// Each box, its eye and each cut line's spark, drawn unshaded: a placeholder, so the world's darkness out in space does not
    /// swallow it.
    /// </summary>
    private sealed partial class Lamps : Node2D
    {
        private IReadOnlyList<(int Id, Vector2 At, Vector2 Size, Color Eye, float Crouch)> _eyes = [];
        private IReadOnlyList<(Vector2 At, double LeftMs)> _snaps = [];

        public void Show(IReadOnlyList<(int Id, Vector2 At, Vector2 Size, Color Eye, float Crouch)> eyes, IReadOnlyList<(Vector2 At, double LeftMs)> snaps)
        {
            (_eyes, _snaps) = (eyes, snaps);
            QueueRedraw();
        }

        public override void _Draw()
        {
            foreach (var (_, at, size, eye, crouch) in _eyes)
            {
                var body = new Vector2(size.X * (1 + 0.12f * crouch), size.Y * (1 - 0.25f * crouch));
                var box = new Rect2(at.X - body.X / 2, at.Y - body.Y, body.X, body.Y);
                DrawRect(box, Hull);
                DrawRect(box, Rim, filled: false, width: 1);
                DrawCircle(new Vector2(at.X, at.Y - size.Y * (1 - 0.25f * crouch) * 0.55f), Cell * 0.45f, eye);
            }
            foreach (var (snapAt, left) in _snaps)
            {
                float r = Cell * (float)(left / SnapMs) * 1.5f;
                DrawLine(snapAt + new Vector2(-r, 0), snapAt + new Vector2(r, 0), Colors.White, 1);
                DrawLine(snapAt + new Vector2(0, -r), snapAt + new Vector2(0, r), Colors.White, 1);
            }
        }
    }
}
