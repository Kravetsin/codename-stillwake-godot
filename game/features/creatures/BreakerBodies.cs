using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The breakers let out of their nests (breakers-look): the pair leaving the rim they worked at, the rest flying out from under the
/// piece one after another, then flying and fighting as the sim says (<see cref="BreakerMotion"/>), lit as the world, their slits
/// unlit over them and lighting it; killed, they come apart into their own pieces. Heard with the stand-ins of <see cref="S.Ticks"/>.
/// </summary>
public partial class BreakerBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    /// <summary>How far past its piece's box a pack's bodies are looked for, in pixels: past where the sim wakes the farthest.</summary>
    private const float PackReach = 6 * Cell;

    private sealed class Shown(BreakerSprite sprite)
    {
        public readonly BreakerSprite Sprite = sprite;
        public BreakerMotion Motion = null!;
        public S.TickCues? Cues;
        public string Species = "";
        public Vector2 From, To, Drawn, Offset;
        public int Tick, Damage, After;
        public double OwnAt, ShotAt, WentAt, LeftAngle;
        public bool Leaving, Worked;
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Dictionary<BreakerKind, Stack<BreakerSprite>> _pool = new();
    private readonly List<Shown> _dying = [], _came = [];
    private readonly HashSet<int> _seen = [], _alive = [];
    private readonly List<int> _gone = [];
    private readonly Voices _suit = new(S.Suit, voices: 4), _world = new(S.World, voices: 4);
    private double _clockMs, _seconds;
    private IReadOnlyList<(float X, float Y)> _heroes = [];

    public override void _Ready()
    {
        AddChild(_suit);
        AddChild(_world);
    }

    /// <summary>
    /// A frame of the creatures in <paramref name="breakers"/>, tick <paramref name="tick"/> drawn <paramref name="progress"/> of the way to
    /// the next, those just come flown out of <paramref name="nests"/>; what their slits light goes into <paramref name="lights"/>.
    /// </summary>
    public void Frame(CreaturesModule creatures, IReadOnlyList<Creature> breakers, SleepingNests nests, int tick, double progress, double elapsedMs,
        IReadOnlyList<(float X, float Y)> heroes, List<LightSource> lights, IReadOnlySet<int> shotByOwn)
    {
        _clockMs += elapsedMs;
        _seconds = nests.Seconds;
        _heroes = heroes;
        foreach (int id in shotByOwn)
            if (_shown.TryGetValue(id, out var shot)) shot.ShotAt = _clockMs;
        double dt = Math.Max(0, elapsedMs / 1000), part = Math.Clamp(progress, 0, 1);
        _seen.Clear();
        _came.Clear();
        for (int i = 0; i < breakers.Count; i++)
        {
            var c = breakers[i];
            _seen.Add(c.Id);
            var at = new Vector2(c.X, c.Y);
            if (!_shown.TryGetValue(c.Id, out var one))
            {
                _shown[c.Id] = one = Take(c, at, tick);
                _came.Add(one);
            }
            if (tick - one.Tick > 1 || tick < one.Tick) (one.From, one.To) = (at, at);
            else if (tick != one.Tick) (one.From, one.To) = (one.To, at);
            one.Tick = tick;
            float half = Spec.Creatures.Get(c.Species).Body.Height * Cell / 2;
            one.Drawn = one.From.Lerp(one.To, (float)part) / Unit * Cell - new Vector2(0, half);
        }
        if (_came.Count > 0) ComeOut(nests.Left);
        for (int i = 0; i < breakers.Count; i++) Fly(_shown[breakers[i].Id], breakers[i], dt, lights);

        _alive.Clear();
        var all = creatures.Creatures;
        for (int i = 0; i < all.Count; i++) _alive.Add(all[i].Id);
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var one = _shown[id];
            _shown.Remove(id);
            // Gone from the world while in view: killed, as nothing is taken away where a hero sees it.
            if (_alive.Contains(id)) Put(one);
            else Die(one);
        }
        Dying(dt);
    }

    /// <summary>This player's own strike on <paramref name="id"/> with <paramref name="tool"/>: it reacts now, a round trip before the sim.</summary>
    public bool OwnStrike(int id, string tool)
    {
        if (!_shown.TryGetValue(id, out var one)) return false;
        one.OwnAt = _clockMs;
        Struck(one, tool, own: true);
        return true;
    }

    /// <summary>Another surface, or a load: all put away, nothing coming apart.</summary>
    public void Reset()
    {
        foreach (var one in _shown.Values) Put(one);
        foreach (var one in _dying) Put(one);
        _shown.Clear();
        _dying.Clear();
    }

    private Shown Take(Creature c, Vector2 at, int tick)
    {
        var spec = Spec.Creatures.Get(c.Species);
        var kind = CreatureLooks.Breakers[spec.Look];
        if (!_pool.TryGetValue(kind, out var pool)) _pool[kind] = pool = new();
        if (!pool.TryPop(out var sprite))
        {
            sprite = new BreakerSprite(kind);
            AddChild(sprite);
        }
        sprite.Beneath(false);
        sprite.Visible = true;
        double cruise = spec.Brain[0] is Spec.Creatures.Swarm swarm ? swarm.Speed * Cell / Unit * World.TicksPerSecond : 1;
        return new Shown(sprite)
        {
            Motion = new BreakerMotion(cruise, c.Id * 1.37),
            Cues = S.Ticks.GetValueOrDefault(c.Species),
            Species = c.Species,
            From = at,
            To = at,
            Tick = tick,
            Damage = c.Damage,
            OwnAt = double.NegativeInfinity,
            ShotAt = double.NegativeInfinity,
        };
    }

    /// <summary>Those just come given their places in the pack that went: the nearest to each at work leaves the rim, the rest the piece's underside, in turn.</summary>
    private void ComeOut(List<SleepingNests.Pack> left)
    {
        for (int p = left.Count - 1; p >= 0; p--)
        {
            var pack = left[p];
            bool taken = false;
            for (int k = 0; k < pack.Count; k++)
            {
                var worker = pack.Workers[k];
                Shown? nearest = null;
                foreach (var one in _came)
                    if (Of(one, pack) && (nearest == null || one.Drawn.DistanceSquaredTo(worker.At) < nearest.Drawn.DistanceSquaredTo(worker.At))) nearest = one;
                if (nearest == null) break;
                (nearest.Leaving, nearest.Worked, nearest.WentAt, nearest.After) = (true, true, pack.WentAt, 0);
                (nearest.Offset, nearest.LeftAngle) = (worker.At - nearest.Drawn, worker.Angle);
                nearest.Motion.Turn(worker.Face, worker.Angle);
                nearest.Motion.Clock = worker.Clock;
                taken = true;
            }
            int after = 0;
            foreach (var one in _came)
            {
                if (!Of(one, pack)) continue;
                (one.Leaving, one.WentAt, one.After, one.Offset) = (true, pack.WentAt, after++, pack.Middle - one.Drawn);
                one.Motion.Turn(one.Drawn.X < pack.Middle.X ? -1 : 1, 0);
                taken = true;
            }
            if (taken) left.RemoveAt(p);
        }
    }

    private static bool Of(Shown one, SleepingNests.Pack pack)
    {
        var off = (one.Drawn - pack.Middle).Abs() - pack.Half;
        return !one.Leaving && one.Species == pack.Species && off.X <= PackReach && off.Y <= PackReach;
    }

    private void Fly(Shown one, Creature c, double dt, List<LightSource> lights)
    {
        bool blown = c.Damage > one.Damage;
        if (blown)
        {
            // A friend's strike, or one of this player's own already shown as it was sent.
            if (_clockMs - one.OwnAt > CreatureLooks.OwnMs) Struck(one, Spec.Weapons.Axe.Item, own: _clockMs - one.ShotAt < CreatureLooks.OwnMs);
            one.Damage = c.Damage;
        }
        double perTick = Cell / (double)Unit * World.TicksPerSecond;
        (double, double)? hero = CreatureFx.NearestOf(_heroes, one.Drawn) is { } h ? (h.X - one.Drawn.X, h.Y - one.Drawn.Y) : null;
        bool active = c.Act is { Phase: ActPhase.Active };
        var moments = one.Motion.Step(new BreakerMotion.Seen(c.Act is { Phase: ActPhase.WindUp }, active, active ? c.Act!.Left : 0, c.VX * perTick, c.VY * perTick, hero, blown), dt);

        var middle = one.Drawn;
        var pose = one.Motion.Pose;
        double angle = one.Motion.Angle;
        bool seen = true;
        if (one.Leaving)
        {
            double u = BreakerPack.Out(_seconds - one.WentAt, one.After);
            middle += one.Offset * (float)(1 - BreakerPack.Gone(u));
            if (one.Worked)
            {
                pose = pose with { Work = 1 - TickMotion.Ease(u) };
                angle += Math.IEEERemainder(one.LeftAngle - angle, Math.Tau) * (1 - TickMotion.Ease(u));
            }
            else
            {
                seen = u > 0;
                one.Sprite.Beneath(u < BreakerPack.UnderTill);
            }
            if (u >= 1) one.Leaving = false;
        }
        one.Sprite.Visible = seen;
        one.Sprite.Show(pose, one.Motion.Flash);
        one.Sprite.Lies(middle, one.Motion.Shake, angle, one.Motion.Side);

        if ((moments & BreakerMotion.Moment.WindUp) != 0 && one.Cues is { } cues) _suit.Play(cues.WindUp, middle);
        if ((moments & BreakerMotion.Moment.HeroHit) != 0 && CreatureFx.NearestOf(_heroes, middle) is { } struck)
        {
            double heading = Math.Atan2(struck.Y - middle.Y, struck.X - middle.X);
            Stages.Of(Layers.Effects).Glims.Spray(Luminous.HitSpark, struck.X, struck.Y, (int)Math.Round(10 * Fx.Count), heading, (-1, 1), (60, 180));
            Stages.Of(Layers.Effects).Flares.Flash(struck.X, struck.Y, (255, 200, 140), 0.6 * Fx.Glow, 6, CreatureLooks.LightKeep, 0.8);
        }
        if (!seen) return;
        var tint = pose.Tint;
        lights.Add(new LightSource(middle.X, middle.Y, ((float)tint.R / 255, (float)tint.G / 255, (float)tint.B / 255),
            CreatureLooks.TickEyeLight * (float)pose.Lamp * (float)(1 + (CreatureLooks.TickActingLight - 1) * pose.Tell), CreatureLooks.LightReach, CreatureLooks.LightKeep, 1));
    }

    private void Struck(Shown one, string tool, bool own)
    {
        one.Motion.Struck();
        var middle = one.Sprite.Position;
        var hero = CreatureFx.NearestOf(_heroes, middle) ?? middle + new Vector2(one.Motion.Face * 20, 0);
        var to = hero - middle;
        var at = middle + to / Math.Max(to.Length(), 1) * 6;
        CreatureFx.Sparks(Stages.Of(Layers.Effects), at, Math.Atan2(to.Y, to.X), 1);
        if (one.Cues is { } cues) (own ? _suit : _world).Play(own ? S.Felt(cues.Hit) : cues.Hit, at, 1, GroundSounds.HitDbOf(tool));
    }

    /// <summary><paramref name="one"/> killed where it was drawn: sparks and dust where its pieces part.</summary>
    private void Die(Shown one)
    {
        var at = one.Sprite.Position;
        one.Drawn = at;
        var away = CreatureFx.NearestOf(_heroes, at) is { } hero ? at - hero : Vector2.Zero;
        one.Motion.Die((away.X, away.Y));
        one.Sprite.Visible = true;
        one.Sprite.Beneath(false);
        if (one.Cues is { } cues) _suit.Play(_clockMs - one.OwnAt < CreatureLooks.OwnMs * 2 || _clockMs - one.ShotAt < CreatureLooks.OwnMs ? S.Felt(cues.Death) : cues.Death, at);
        var glims = Stages.Of(Layers.Effects).Glims;
        glims.Spray(Luminous.BreakSpark, at.X, at.Y, (int)Math.Round(14 * Fx.Count), 0, (-Math.PI, Math.PI), (50, 160));
        for (int i = 0; i < 6; i++) glims.Add(Luminous.BreakDust, at.X, at.Y, glims.Between(-25, 25), glims.Between(-25, 25));
        _dying.Add(one);
    }

    private void Dying(double dt)
    {
        for (int i = _dying.Count - 1; i >= 0; i--)
        {
            var one = _dying[i];
            one.Motion.Step(default, dt);
            one.Sprite.Show(one.Motion.Pose, one.Motion.Flash);
            one.Sprite.Lies(one.Drawn + new Vector2((float)one.Motion.Drift.X, (float)one.Motion.Drift.Y), 0, one.Motion.Angle, one.Motion.Side);
            if (!one.Motion.Gone) continue;
            _dying.RemoveAt(i);
            Put(one);
        }
    }

    private void Put(Shown one)
    {
        one.Sprite.Visible = false;
        _pool[one.Sprite.Kind].Push(one.Sprite);
    }
}
