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
/// The creatures drawn as the escort A1 «Конвоир» (derelict-enemies), posed each frame by <see cref="EscortRig"/>: asleep stowed in its
/// stand, hung its <see cref="StandWake.Hang"/> over the floor, then lowered and unfolding as it wakes (the stand's own parts are
/// <see cref="DocksView"/>'s); awake striding, its rail gun turning to its aim as it charges and kicking back as it fires, its claw reaching
/// out as it dashes. Its red head lamp shines and lights the hall; each gauss is a bright cyan-white trace to where it stopped, sparks
/// there. Struck it flashes; killed it bursts into sparks and crumbles. Heard on World (<see cref="S.Escorts"/>): each step, a servo as
/// it turns round or swings its gun up, its gauss charging, fired and stopping, its dash, its pry, a blow on it and its death; its
/// powering up in its stand is <see cref="DocksView"/>'s.
/// </summary>
public partial class EscortBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    /// <summary>The height over its feet it turns about, in pixels: its middle.</summary>
    private static readonly float Middle = Spec.Creatures.Escort.Body.Height * Cell / 2;

    /// <summary>How long a gauss's trace shows, and its first flash, in seconds; how far a shot kicks it back, fading over seconds.</summary>
    private const double TraceS = 0.45, FlashS = 0.08, RecoilS = 0.3;

    private static readonly (int R, int G, int B) Edge = (111, 208, 255), Core = (244, 251, 255), LampRed = (255, 43, 28);

    private sealed partial class Shown : PosedSprite
    {
        private readonly EscortRig _rig = new();
        private readonly InkRig _ink;

        public Shown() : base(Middle)
        {
            _ink = new InkRig(CreatureArt.InkEscort.Id, CreatureArt.InkEscort.Parts) { Position = new Vector2(0, Middle) };
            AddChild(_ink);
        }

        public EscortRig.Pose Pose;
        public EscortRig.Anchors Anchors;
        public S.EscortCues? Cues;
        public Footfalls Feet = null!;
        public int? Prying;
        public Vector2 From, To, Drawn;
        public int Tick = -1, Damage, Face = -1;
        public double Reach, Phase, Move, AimMix, Coil, Claw, Open, Lamp, Flash, FiredAgo = double.PositiveInfinity, ServoAgo,
            OwnAt = double.NegativeInfinity, ShotAt = double.NegativeInfinity;
        public bool Firing, Charging, Dashing;

        /// <summary>Its sprite in its pose, at its place, facing its way.</summary>
        public void Show(Vector2 feet)
        {
            Anchors = _rig.Place(Pose);
            _ink.Show(_rig.Slots, Flash, 0);
            Place(feet, 0, 0, Face);
        }

        /// <summary>Where a point of its own (<see cref="EscortRig.Anchors"/>) is in the world this frame.</summary>
        public Vector2 World((double X, double Y) at) => Transform * new Vector2((float)at.X, (float)at.Y + Middle);

        public void PaintInto(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into) => _ink.Pixels(into, Transform);
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Stack<Shown> _pool = new();
    private readonly HashSet<int> _seen = [], _alive = [];
    private readonly List<int> _gone = [];
    private readonly List<(Vector2 From, Vector2 To, double Age)> _traces = [];
    private readonly Dictionary<(int X, int Y), (byte R, byte G, byte B)> _pixels = [];
    private readonly Voices _world = new(S.World, voices: 4);
    private double _clockMs;
    private IReadOnlyList<(float X, float Y)> _heroes = [];

    /// <summary>What a killed one crumbles into, shared with the other creatures.</summary>
    public PixelBursts? Bursts { get; set; }

    public override void _Ready()
    {
        ZAsRelative = false;
        ZIndex = Layers.Escorts;
        AddChild(_world);
    }

    /// <summary>
    /// A frame of the creatures in <paramref name="escorts"/> (those drawn as the escort), tick <paramref name="tick"/> drawn
    /// <paramref name="progress"/> of the way to the next, aiming at the heroes drawn at <paramref name="heroes"/> (their chests); what their
    /// lamps light goes into <paramref name="lights"/>; the grid <paramref name="width"/> cells wide; <paramref name="shotByOwn"/> those this
    /// player's own shots hit.
    /// </summary>
    public void Frame(CreaturesModule creatures, int width, IReadOnlyList<Creature> escorts, int tick, double progress, double elapsedMs,
        IReadOnlyList<(float X, float Y)> heroes, List<LightSource> lights, IReadOnlySet<int> shotByOwn)
    {
        _clockMs += elapsedMs;
        foreach (int id in shotByOwn)
            if (_shown.TryGetValue(id, out var shot)) shot.ShotAt = _clockMs;
        _heroes = heroes;
        double dt = Math.Max(0, elapsedMs / 1000);
        _seen.Clear();
        foreach (var c in escorts)
        {
            _seen.Add(c.Id);
            if (!_shown.TryGetValue(c.Id, out var one)) _shown[c.Id] = one = Take(c, tick);
            Follow(one, c, tick);
            var feet = one.From.Lerp(one.To, (float)Math.Clamp(progress, 0, 1)) / Unit * Cell;
            if (c.Damage > one.Damage)
            {
                // A friend's strike, or one of this player's own already heard as it was sent.
                if (_clockMs - one.OwnAt > CreatureLooks.OwnMs) Struck(one, Spec.Weapons.Axe.Item, own: _clockMs - one.ShotAt < CreatureLooks.OwnMs);
                one.Damage = c.Damage;
            }
            Pose(one, c, feet, progress, dt);
            if (one.Firing) Fire(creatures, width, one);
            Heard(one, c, dt);
            Glows(one, lights);
        }
        _alive.Clear();
        foreach (var c in creatures.Creatures) _alive.Add(c.Id);
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var one = _shown[id];
            _shown.Remove(id);
            if (!_alive.Contains(id)) Killed(one);
            Put(one);
        }
        Traces(dt, lights);
    }

    /// <summary>This player's own strike on <paramref name="id"/> with <paramref name="tool"/>, seen in flight for the first time: it reacts now, a round trip before the sim.</summary>
    public bool OwnStrike(int id, string tool)
    {
        if (!_shown.TryGetValue(id, out var one)) return false;
        one.OwnAt = _clockMs;
        Struck(one, tool, own: true);
        return true;
    }

    /// <summary>A blow on <paramref name="one"/> with <paramref name="tool"/>: it flashes, heard at its middle, this player's own as far as felt.</summary>
    private void Struck(Shown one, string tool, bool own)
    {
        one.Flash = 1;
        if (one.Cues is { } cues) _world.Play(own ? S.Felt(cues.Hit) : cues.Hit, one.Drawn + new Vector2(0, -Middle), 1, GroundSounds.HitDbOf(tool));
    }

    /// <summary>
    /// Its sounds this frame, from what the sim says of it and its pose: a step as a foot comes down (each half stride, as the rig plants
    /// it); a servo as it turns round, and as its gun swings up to aim with its charge; its dash's swing; a pry begun at a door.
    /// </summary>
    private void Heard(Shown one, Creature c, double dt)
    {
        if (one.Cues is not { } cues) return;
        var spec = Spec.Creatures.Get(c.Species);
        var middle = one.Drawn + new Vector2(0, -Middle);
        bool awake = c.Posture is not (Posture.Docked or Posture.Waking);
        // A foot comes down at the front of its stride: its phase at a quarter turn, the far foot half a turn on.
        if (one.Feet.Step(one.Phase - Math.PI / 2, awake && one.Move > 0.5, dt)) _world.Play(cues.Step, one.Drawn);
        one.ServoAgo += dt;
        bool charging = c.Act is { Phase: ActPhase.WindUp } a && spec.Moves[a.Move] is Spec.Creatures.Shoot;
        bool dashing = c.Act is { Phase: ActPhase.Active } d && spec.Moves[d.Move] is Spec.Creatures.Lunge;
        if (charging && !one.Charging)
        {
            var muzzle = one.World(one.Anchors.Muzzle);
            _world.Play(cues.Charge, muzzle);
            _world.Play(cues.Servo, muzzle);
            one.ServoAgo = 0;
        }
        if (dashing && !one.Dashing) _world.Play(cues.Lunge, middle);
        (one.Charging, one.Dashing) = (charging, dashing);
        int? prying = CreaturesModule.PryingAt(c);
        if (prying != null && prying != one.Prying) _world.Play(cues.Pry, middle);
        one.Prying = prying;
    }

    /// <summary>It turned round to face <paramref name="face"/>: a servo, unless one was heard a moment ago.</summary>
    private void Turned(Shown one, int face)
    {
        if (one.Face != -face || one.Cues is not { } cues || one.ServoAgo < S.EscortServoLeastS) return;
        _world.Play(cues.Servo, one.Drawn + new Vector2(0, -Middle));
        one.ServoAgo = 0;
    }

    /// <summary>Another surface, or a load: what was shown was another world's.</summary>
    public void Reset()
    {
        foreach (var one in _shown.Values) Put(one);
        _shown.Clear();
        _traces.Clear();
    }

    private Shown Take(Creature c, int tick)
    {
        var one = _pool.Count > 0 ? _pool.Pop() : Made();
        var at = new Vector2(c.X, c.Y);
        (one.From, one.To, one.Tick, one.Damage, one.Face) = (at, at, tick, c.Damage, -1);
        (one.Phase, one.Move, one.AimMix, one.Coil, one.Claw, one.Open, one.Lamp, one.Flash, one.FiredAgo, one.Firing) = (0, 0, 0, 0, 0, 0.3, 0, 0, double.PositiveInfinity, false);
        // Its servo first heard a moment after it is seen: seen walking left, it has not turned.
        (one.Cues, one.Feet, one.Prying, one.Charging, one.Dashing, one.ServoAgo) =
            (S.Escorts.GetValueOrDefault(c.Species), new Footfalls(S.EscortStepEvery, S.EscortStepLeastS), CreaturesModule.PryingAt(c), false, false, 0);
        (one.OwnAt, one.ShotAt) = (double.NegativeInfinity, double.NegativeInfinity);
        one.Drawn = at / Unit * Cell;
        one.Visible = true;
        return one;
    }

    private Shown Made()
    {
        var one = new Shown();
        AddChild(one);
        return one;
    }

    private void Put(Shown one)
    {
        one.Visible = false;
        _pool.Push(one);
    }

    private static void Follow(Shown one, Creature c, int tick)
    {
        var at = new Vector2(c.X, c.Y);
        if (one.Tick < 0 || tick - one.Tick > 1) (one.From, one.To) = (at, at);
        else if (tick != one.Tick) (one.From, one.To) = (one.To, at);
        one.Tick = tick;
    }

    /// <summary>Its pose this frame from what the sim says of it, eased toward each state; placed and drawn.</summary>
    private void Pose(Shown one, Creature c, Vector2 feet, double progress, double dt)
    {
        var spec = Spec.Creatures.Get(c.Species);
        double fold = 0, hung = 0, lamp = 1, walked = (feet - one.Drawn).Length() / Cell;
        if (c.Posture is Posture.Docked or Posture.Waking)
        {
            double p = c.Posture == Posture.Docked ? 0 : DockWake.Progress(c.Left, DocksView.WakeOf(c), progress);
            (fold, hung, lamp) = (StandWake.Folded(p), StandWake.Hung(p), StandWake.Lamp(p, p * DocksView.WakeOf(c) / World.TicksPerSecond));
        }
        var chest = CreatureFx.NearestOf(_heroes, feet + new Vector2(0, -Middle));
        bool shooting = c.Act is { } act && spec.Moves[act.Move] is Spec.Creatures.Shoot, winding = c.Act is { Phase: ActPhase.WindUp };
        // It shoots walking: faced to its aim then, though it walks away from it (to a door, its hero off its deck).
        int? toHero = c.Posture is not (Posture.Docked or Posture.Waking) && chest is { } h && Math.Abs(h.X - feet.X) > 2 ? (h.X > feet.X ? 1 : -1) : null;
        int face = toHero is int to && (shooting || c.VX == 0) ? to : c.VX != 0 ? Math.Sign(c.VX) : one.Face;
        Turned(one, face);
        one.Face = face;
        bool lunging = c.Act is { } a2 && spec.Moves[a2.Move] is Spec.Creatures.Lunge;
        if (c.Act is { } a3 && spec.Moves[a3.Move] is Spec.Creatures.Shoot gauss) one.Reach = gauss.Reach;
        one.Firing = shooting && !winding && one.FiredAgo == double.PositiveInfinity;
        if (one.Firing) one.FiredAgo = 0;
        else if (one.FiredAgo != double.PositiveInfinity) one.FiredAgo += dt;
        if (!shooting) one.FiredAgo = double.PositiveInfinity;
        double k = 1 - Math.Exp(-dt * 10);
        one.Move += ((walked > 0.01 && c.VX != 0 ? 1 : 0) - one.Move) * k;
        one.Phase += walked * EscortRig.PhasePerCell;
        one.AimMix += ((shooting ? 1 : 0) - one.AimMix) * k;
        one.Coil += ((shooting && winding ? 1 : 0) - one.Coil) * (shooting && winding ? k * 0.6 : k * 2);
        one.Claw += ((lunging && !winding ? 1 : 0) - one.Claw) * Math.Min(1, k * 2);
        one.Open += ((lunging ? (winding ? 1 : 0.3) : 0.3) - one.Open) * k;
        one.Lamp = lamp;
        one.Flash = Math.Max(0, one.Flash - dt * 6);
        double recoil = one.FiredAgo < RecoilS ? 1 - one.FiredAgo / RecoilS : 0;
        (double, double)? aim = chest is { } at ? ((at.X - feet.X) * one.Face, at.Y - feet.Y) : null;
        one.Pose = new EscortRig.Pose(one.Phase, one.Move, fold, aim, one.AimMix, recoil, one.Coil, one.Claw, one.Open, one.Lamp);
        one.Drawn = feet;
        one.Show(feet + new Vector2(0, (float)(-hung * Cell)));
    }

    /// <summary>
    /// Its gauss fired this tick: a trace from its muzzle to where the sim's shot stopped (its end this tick nearest it), else on along its
    /// gun to the first wall; a flash at the muzzle.
    /// </summary>
    private void Fire(CreaturesModule creatures, int width, Shown one)
    {
        var muzzle = one.World(one.Anchors.Muzzle);
        Vector2? end = null;
        float best = float.MaxValue;
        foreach (var e in creatures.Ended)
        {
            if (e.Projectile != Spec.Shots.Gauss.Id) continue;
            var at = new Vector2(e.Cell % width + 0.5f, e.Cell / width + 0.5f) * Cell;
            if (at.DistanceSquaredTo(muzzle) < best) (best, end) = (at.DistanceSquaredTo(muzzle), at);
        }
        if (end == null)
        {
            double turn = one.Anchors.GunTurn, reach = one.Reach;
            int x = (int)(muzzle.X / Cell * Unit), y = (int)(muzzle.Y / Cell * Unit);
            var (x1, y1) = creatures.BeamStop(x, y, (long)(Math.Cos(turn) * one.Face * reach * Unit), (long)(Math.Sin(turn) * reach * Unit));
            end = new Vector2(x1, y1) / Unit * Cell;
        }
        _traces.Add((muzzle, end.Value, 0));
        if (one.Cues is { } cues)
        {
            _world.Play(cues.Shot, muzzle);
            _world.Play(cues.Stop, end.Value);
        }
        var flares = Stages.Of(Layers.Effects).Flares;
        flares.Flash(muzzle.X, muzzle.Y, Edge, 0.9 * Fx.Glow, 8, CreatureLooks.LightKeep, 0.8);
        flares.Glint(muzzle.X, muzzle.Y, 3, Core);
        var to = end.Value - muzzle;
        CreatureFx.Sparks(Stages.Of(Layers.Effects), end.Value, Math.Atan2(-to.Y, -to.X), 0.8);
        flares.Flash(end.Value.X, end.Value.Y, Edge, 0.7 * Fx.Glow, 6, CreatureLooks.LightKeep, 0.8);
    }

    /// <summary>Each gauss's trace this frame: its cyan edge and white core fading over <see cref="TraceS"/>, its end struck white at first.</summary>
    private void Traces(double dt, List<LightSource> lights)
    {
        var flares = Stages.Of(Layers.Effects).Flares;
        for (int i = _traces.Count - 1; i >= 0; i--)
        {
            var (from, to, age) = _traces[i];
            if (age > TraceS)
            {
                _traces.RemoveAt(i);
                continue;
            }
            double fade = age < FlashS ? 1 : 1 - (age - FlashS) / (TraceS - FlashS);
            flares.Trail(to.X, to.Y, from.X, from.Y, Edge, 0.4, 0.55 * fade, 2);
            flares.Trail(to.X, to.Y, from.X, from.Y, Core, 1, fade, 0.7);
            if (age < FlashS) flares.Bloom(to.X, to.Y, 5, Core, 0.9);
            _traces[i] = (from, to, age + dt);
        }
    }

    /// <summary>Its red lamp's glow and the light it casts, as lit.</summary>
    private static void Glows(Shown one, List<LightSource> lights)
    {
        if (one.Lamp <= 0.01) return;
        var eye = one.World(one.Anchors.Lamp);
        var flares = Stages.Of(Layers.Creatures).Flares;
        flares.Bloom(eye.X, eye.Y, 7, LampRed, 0.4 * one.Lamp);
        flares.Bloom(eye.X, eye.Y, 2.4, (255, 200, 190), 0.6 * one.Lamp);
        lights.Add(new LightSource(eye.X, eye.Y, (1f, 0.17f, 0.11f), CreatureLooks.TickEyeLight * (float)one.Lamp, CreatureLooks.LightReach,
            CreatureLooks.LightKeep, 1));
    }

    /// <summary>Killed where it was drawn: a flash and sparks, heard shutting down, this player's own kill as far as felt; and it crumbles.</summary>
    private void Killed(Shown one)
    {
        var middle = one.Drawn + new Vector2(0, -Middle);
        if (one.Cues is { } cues)
            _world.Play(_clockMs - one.OwnAt < CreatureLooks.OwnMs * 2 || _clockMs - one.ShotAt < CreatureLooks.OwnMs ? S.Felt(cues.Killed) : cues.Killed, middle);
        CreatureFx.Sparks(Stages.Of(Layers.Effects), middle, -Math.PI / 2, 1.4);
        Stages.Of(Layers.Effects).Flares.Flash(middle.X, middle.Y, (255, 214, 150), 1.2 * Fx.Glow, 12, CreatureLooks.LightKeep, 0.86);
        if (Bursts == null) return;
        _pixels.Clear();
        one.PaintInto(_pixels);
        Bursts.Crumble(_pixels, one.Drawn, 0);
    }
}
