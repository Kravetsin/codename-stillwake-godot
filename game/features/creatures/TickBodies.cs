using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The creatures drawn as the tick (the sentry, its page's candidate 3 «Клещ»): each its sprite drawn anew each frame from its
/// pose (<see cref="TickArt"/>, <see cref="TickMotion"/>), lit as the world, its eye and cracks over it unlit; its eye lamp
/// lighting the wreck in its colour, red arcs jumping between its legs as it winds up, an amber burner trailing sparks and
/// smoke while it flies, its feet's magnets glowing while it grips; struck a white flash and fine sparks; a harpoon line it
/// cuts snipped in sparks at its claw; killed, cracks flashing over it till it bursts into sparks and its own pixels (2026-10-29-kill-feel). Heard on the
/// Suit bus, as the wreck it guards is (<see cref="S.Ticks"/>). Drawn between ticks where the sim put it.
/// </summary>
public partial class TickBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    private static readonly (int, int, int) Yellow = (255, 209, 51), Plume = (255, 190, 90), Magnet = (255, 170, 84);

    // Turned about its middle over its feet, as the page turns it; in ink that is higher, on its stilts.
    private static readonly float Middle = HiRes.On ? (float)-CreatureArt.InkTick.Centre.Y : 8;

    private sealed partial class Shown : PosedSprite
    {
        public readonly TickArt Art = new();
        private readonly TickRig _rig = new();
        private readonly InkRig? _ink;

        public Shown() : base(TickArt.Width, TickArt.Height, TickArt.OriginX, TickArt.OriginY, Middle)
        {
            if (!HiRes.On) return;
            _ink = new InkRig(CreatureArt.InkTick.Id, CreatureArt.InkTick.Parts) { Position = new Vector2(0, Middle) };
            AddChild(_ink);
        }

        public TickMotion Motion = null!;
        public Heard2D? Burner;
        public LoopEnvelope Burning;
        public TickArt.Anchors Anchors = null!;
        public S.TickCues? Cues;
        public Vector2 From, To, Drawn;
        public int Tick = -1, Damage, Cut, Piece;
        public double AlongFrom, AlongTo, OwnAt = double.NegativeInfinity, ShotAt = double.NegativeInfinity, ArcsOwed, BurnOwed, SmokeOwed, MotesOwed, SpitOwed, SawOwed, WhipLeft;
        /// <summary>The way the line it saws runs off, from its claw: what its snap whips along.</summary>
        public Vector2 LineWay = Vector2.Left;
        public string Species = "";

        /// <summary>Its sprite drawn in its motion's pose, at its place, turned and facing as it flies.</summary>
        public void Show(Vector2 feet)
        {
            if (_ink != null)
            {
                Anchors = _rig.Place(Motion.Pose);
                _ink.Show(_rig.Slots, Motion.Flash, 0);
            }
            else
            {
                Anchors = Art.Draw(Motion.Pose);
                Paint(Art.Albedo, Art.Silhouette, Art.Emission);
            }
            Place(feet, Motion.Shake, Motion.Angle, Motion.Face, Motion.Flash);
        }

        /// <summary>Where a point of its sprite (<see cref="TickArt.Anchors"/>) is in the world this frame, the middle of its pixel if it has pixels.</summary>
        public Vector2 World((double X, double Y) at) => Transform * (_ink != null ? new Vector2((float)at.X, (float)at.Y + Middle) : new Vector2((float)at.X + 0.5f, (float)at.Y + 8.5f));

        /// <summary>Its pixels in the world as they stand now, for a burst.</summary>
        public void PaintInto(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into)
        {
            if (_ink != null) _ink.Pixels(into, Transform);
            else PaintInto(into, Art.Albedo);
        }
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Stack<Shown> _pool = new();
    private readonly List<Shown> _dying = [];
    private readonly HashSet<int> _seen = [], _alive = [];
    private readonly List<int> _gone = [];
    private readonly Voices _suit = new(S.Suit, voices: 4), _world = new(S.World, voices: 4);
    private readonly Random _random = new();
    private double _clockMs;
    private IReadOnlyList<(float X, float Y)> _heroes = [];

    public override void _Ready()
    {
        AddChild(_suit);
        AddChild(_world);
    }

    /// <summary>
    /// A frame of the creatures in <paramref name="ticks"/> (those drawn as the tick), tick <paramref name="tick"/> drawn
    /// <paramref name="progress"/> of the way to the next, watching the heroes drawn at <paramref name="heroes"/> (their chests);
    /// what their lamps and burners light goes into <paramref name="lights"/>; <paramref name="shotByOwn"/> those this player's own shots hit.
    /// </summary>
    public void Frame(CreaturesModule creatures, IDebrisQueries debris, IReadOnlyList<Creature> ticks, int tick, double progress,
        double elapsedMs, IReadOnlyList<(float X, float Y)> heroes, List<LightSource> lights, IReadOnlySet<int> shotByOwn)
    {
        _clockMs += elapsedMs;
        foreach (int id in shotByOwn)
            if (_shown.TryGetValue(id, out var shot)) shot.ShotAt = _clockMs;
        _heroes = heroes;
        double dt = Math.Max(0, elapsedMs / 1000);
        _seen.Clear();
        foreach (var c in ticks)
        {
            _seen.Add(c.Id);
            if (!_shown.TryGetValue(c.Id, out var one)) _shown[c.Id] = one = Take(c, creatures, tick);
            Follow(one, c, creatures, debris, tick);
            var feet = one.From.Lerp(one.To, (float)Math.Clamp(progress, 0, 1)) / Unit * Cell;
            if (c.Damage > one.Damage)
            {
                // A friend's strike, or one of this player's own already shown as it was sent.
                if (_clockMs - one.OwnAt > CreatureLooks.OwnMs) Struck(one, Spec.Weapons.Axe.Item, own: _clockMs - one.ShotAt < CreatureLooks.OwnMs);
                one.Damage = c.Damage;
            }
            var moments = one.Motion.Step(SeenOf(one, c, creatures, debris, progress), dt);
            one.Drawn = feet;
            one.Show(feet);
            Moments(one, moments);
            Cut(one, c);
            Sawing(one, debris, dt, lights);
            Whip(one, dt);
            Effects(one, dt, lights);
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
            // Gone from the world while in view: killed, as nothing is taken away where a hero sees it.
            if (_alive.Contains(id)) Put(one);
            else Die(one);
        }
        Dying(dt, lights);
    }

    /// <summary>This player's own strike on <paramref name="id"/> with <paramref name="tool"/>, seen in flight for the first time: it reacts now, a round trip before the sim.</summary>
    public bool OwnStrike(int id, string tool)
    {
        if (!_shown.TryGetValue(id, out var one)) return false;
        one.OwnAt = _clockMs;
        Struck(one, tool, own: true);
        return true;
    }

    /// <summary>Another surface, or a load: what was shown was another world's, gone without a spark.</summary>
    public void Reset()
    {
        foreach (var one in _shown.Values) Put(one);
        foreach (var one in _dying) Put(one);
        _shown.Clear();
        _dying.Clear();
    }

    private Shown Take(Creature c, CreaturesModule creatures, int tick)
    {
        var one = _pool.Count > 0 ? _pool.Pop() : Made();
        var at = new Vector2(c.X, c.Y);
        one.Motion = new TickMotion(_random.NextDouble);
        (one.From, one.To, one.Tick, one.Damage, one.Species) = (at, at, tick, c.Damage, c.Species);
        (one.Cut, one.OwnAt, one.Piece) = (CreaturesModule.LastCut(c), double.NegativeInfinity, -1);
        one.Cues = S.Ticks.GetValueOrDefault(c.Species);
        one.Burning = default;
        (one.WhipLeft, one.LineWay) = (0, Vector2.Left);
        one.Visible = true;
        one.Modulate = Colors.White;
        if (one.Cues is { } cues && one.Burner == null)
        {
            one.Burner = Cues.Player2D(cues.Thrust, S.Suit);
            AddChild(one.Burner);
        }
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
        one.Burner?.Stop();
        _pool.Push(one);
    }

    /// <summary><paramref name="one"/>'s tick positions kept, and how far along its wreck it sat: for drawing between them and its crawl.</summary>
    private static void Follow(Shown one, Creature c, CreaturesModule creatures, IDebrisQueries debris, int tick)
    {
        var at = new Vector2(c.X, c.Y);
        int piece = creatures.ClingsTo(c) ?? -1;
        double along = debris.PieceById(piece) is { } p ? c.X - p.X : 0;
        if (one.Tick < 0 || tick - one.Tick > 1 || piece != one.Piece) (one.From, one.To, one.AlongFrom, one.AlongTo) = (at, at, along, along);
        else if (tick != one.Tick) (one.From, one.To, one.AlongFrom, one.AlongTo) = (one.To, at, one.AlongTo, along);
        (one.Tick, one.Piece) = (tick, piece);
    }

    /// <summary>What the sim says of <paramref name="c"/> as its motion reads it, <paramref name="progress"/> of the way to the next tick.</summary>
    private TickMotion.Seen SeenOf(Shown one, Creature c, CreaturesModule creatures, IDebrisQueries debris, double progress)
    {
        var spec = Spec.Creatures.Get(c.Species);
        bool clamped = CreaturesModule.Clamped(c);
        var act = c.Act;
        // An arc is no pounce: it crouches to aim, its eye flickering the arc's colour, held once its aim is taken, and recovers
        // clamped on.
        bool shoots = act != null && spec.Moves[act.Move] is Spec.Creatures.Shoot;
        bool winding = act is { Phase: ActPhase.WindUp } && !shoots, pouncing = act is { Phase: ActPhase.Active } && !shoots;
        bool aiming = act is { Phase: ActPhase.WindUp } && shoots;
        bool locked = aiming && spec.Moves[act!.Move] is Spec.Creatures.Shoot shot && act.Left <= shot.Lock;
        double windUpS = 0;
        if (winding && spec.Moves[act!.Move] is Spec.Creatures.Pounce pounce)
            windUpS = (pounce.WindUp - act.Left + Math.Clamp(progress, 0, 1)) / World.TicksPerSecond;
        double perTick = Cell / (double)Unit * World.TicksPerSecond;
        double crawl = clamped ? (one.AlongTo - one.AlongFrom) * perTick : 0;
        var middle = one.Drawn + new Vector2(0, -8);
        (double, double)? hero = Nearest(middle) is { } h ? (h.X - middle.X, h.Y - middle.Y) : null;
        var (cutLeft, cutOf) = creatures.CutLeft(c);
        bool cutting = cutOf > 0;
        (double, double)? line = cutting && LineEnd(debris, one.Piece) is { } end ? (end.X - middle.X, end.Y - middle.Y) : null;
        double done = cutting ? Math.Clamp((cutOf - cutLeft + Math.Clamp(progress, 0, 1)) / cutOf, 0, 1) : 0;
        return new TickMotion.Seen(clamped, CreaturesModule.Alerted(c), winding, windUpS, pouncing, pouncing ? act!.Left : 0,
            c.VX * perTick, c.VY * perTick, crawl, hero, cutting, done, line, aiming, locked);
    }

    /// <summary>
    /// Where the harpoon line on <paramref name="piece"/> runs off to, in pixels: the hero reeling it as drawn, the one nearest the
    /// point the sim reels the piece to (by that hero's muzzle); that point itself with none drawn near. None with no line on it.
    /// </summary>
    private Vector2? LineEnd(IDebrisQueries debris, int piece)
    {
        if (debris.PieceById(piece)?.Reel is not { } reel) return null;
        var to = new Vector2(reel.ToX, reel.ToY) / Unit * Cell;
        return Nearest(to) is { } hero && hero.DistanceTo(to) < 6 * Cell ? hero : to;
    }

    /// <summary>The chest of the hero nearest <paramref name="at"/>, as the crew drew them this frame; none with no hero.</summary>
    private Vector2? Nearest(Vector2 at) => CreatureFx.NearestOf(_heroes, at);

    /// <summary>A blow on <paramref name="one"/>: it flashes and shakes, its eye red, fine sparks thrown toward the hero who struck, heard where it lands.</summary>
    private void Struck(Shown one, string tool, bool own)
    {
        one.Motion.Struck();
        var middle = one.World(one.Anchors?.Centre ?? (-3, -7));
        var hero = Nearest(middle) ?? middle + new Vector2(one.Motion.Face * 20, -10);
        var to = hero - middle;
        float d = Math.Max(to.Length(), 1);
        var at = middle + new Vector2(to.X / d * 7, to.Y / d * 5);
        double heading = Math.Atan2(to.Y, to.X);
        CreatureFx.Sparks(Stages.Of(Layers.Effects), at, heading, 1);
        if (one.Cues is { } cues) (own ? _suit : _world).Play(own ? S.Felt(cues.Hit) : cues.Hit, at, 1, GroundSounds.HitDbOf(tool));
    }

    /// <summary>Its moments seen and heard: a start and a flash at its eye, the wind-up's servo, letting go and clamping on, a braking flight, a hero struck.</summary>
    private void Moments(Shown one, TickMotion.Moment m)
    {
        if (m == TickMotion.Moment.None) return;
        var glims = Stages.Of(Layers.Creatures).Glims;
        var eye = one.World(one.Anchors.Eye);
        var cues = one.Cues;
        if (m.HasFlag(TickMotion.Moment.Notice))
        {
            Stages.Of(Layers.Creatures).Flares.Flash(eye.X, eye.Y, Yellow, 0.6 * Fx.Glow, 8, CreatureLooks.LightKeep, 0.8);
            Stages.Of(Layers.Creatures).Flares.Glint(eye.X, eye.Y, 4, (255, 250, 220));
            if (cues != null) _suit.Play(cues.Alert, eye);
        }
        if (m.HasFlag(TickMotion.Moment.WindUp) && cues != null) _suit.Play(cues.WindUp, eye);
        if (m.HasFlag(TickMotion.Moment.Aim) && cues != null) _suit.Play(cues.Charge, eye);
        if (m.HasFlag(TickMotion.Moment.Unclamp))
        {
            Clack(one);
            var nozzle = one.World(one.Anchors.Nozzle);
            for (int i = 0; i < 6; i++) glims.Add(Luminous.BurnerSmoke, nozzle.X, nozzle.Y, glims.Between(-25, 25), glims.Between(-25, 25));
            for (int i = 0; i < 8; i++) glims.Add(Luminous.BurnerMote, nozzle.X, nozzle.Y, glims.Between(-50, 50), glims.Between(-50, 50));
            if (cues != null) _suit.Play(cues.Unclamp, eye);
        }
        if (m.HasFlag(TickMotion.Moment.Clamp))
        {
            Clack(one);
            if (cues != null) _suit.Play(cues.Clamp, one.Drawn);
        }
        if (m.HasFlag(TickMotion.Moment.Brake))
        {
            // Its burner turned forward a moment: a burst ahead, smoke rolling on.
            var (vx, vy) = one.Motion.Velocity;
            double heading = Math.Atan2(-one.Motion.Plume.Y, -one.Motion.Plume.X);
            for (int i = 0; i < 10; i++)
            {
                double a = heading + glims.Between(-0.5, 0.5), s = glims.Between(120, 200);
                glims.Add(Luminous.Burner, eye.X, eye.Y, vx + Math.Cos(a) * s, vy + Math.Sin(a) * s);
            }
            for (int i = 0; i < 3; i++)
                glims.Add(Luminous.BurnerSmoke, eye.X, eye.Y, Math.Cos(heading) * 30 + glims.Between(-10, 10), Math.Sin(heading) * 30 + glims.Between(-10, 10));
        }
        if (m.HasFlag(TickMotion.Moment.HeroHit) && Nearest(eye) is { } hero)
        {
            double heading = Math.Atan2(-one.Motion.Plume.Y, -one.Motion.Plume.X);
            Stages.Of(Layers.Effects).Glims.Spray(Luminous.HitSpark, hero.X, hero.Y, (int)Math.Round(10 * Fx.Count), heading, (-1, 1), (60, 180));
            Stages.Of(Layers.Effects).Flares.Flash(hero.X, hero.Y, (255, 200, 140), 0.6 * Fx.Glow, 6, CreatureLooks.LightKeep, 0.8);
        }
    }

    /// <summary>Its feet meeting or leaving the hull: pale streaks along it, a glint, dust and a veil at each near foot.</summary>
    private static void Clack(Shown one)
    {
        var glims = Stages.Of(Layers.Creatures).Glims;
        foreach (var foot in one.Anchors.Feet)
        {
            var at = one.World((foot.X, foot.Y + 0.5));
            for (int i = 0; i < 3; i++)
                glims.Add(Luminous.LandStreak, at.X, at.Y, (glims.Between(0, 1) < 0.5 ? -1 : 1) * glims.Between(50, 120), -glims.Between(0, 15));
            Stages.Of(Layers.Creatures).Flares.Glint(at.X, at.Y - 0.5, 2.5, (255, 255, 255));
            for (int i = 0; i < 2; i++) glims.Add(Luminous.HitDust, at.X, at.Y, glims.Between(-12, 12), glims.Between(-14, -2));
            glims.Add(Luminous.DustVeil, at.X, at.Y - 1, glims.Between(-10, 10), glims.Between(-6, -1));
        }
    }

    /// <summary>A harpoon line <paramref name="c"/> cut since last seen (<see cref="CreaturesModule.LastCut"/>): snipped at its claw, sparks and a snap.</summary>
    private void Cut(Shown one, Creature c)
    {
        int cut = CreaturesModule.LastCut(c);
        if (cut <= one.Cut) return;
        one.Cut = cut;
        one.Motion.Snipped();
        var claw = one.World(one.Anchors.Cut);
        // The cut end flies off along the line: sparks and white slivers that way, a whip of light down it.
        double along = Math.Atan2(one.LineWay.Y, one.LineWay.X);
        CreatureFx.Sparks(Stages.Of(Layers.Creatures), claw, Math.Atan2(one.LineWay.Y - 0.5, one.LineWay.X), 1.1);
        Stages.Of(Layers.Creatures).Glims.Spray(Luminous.Snip, claw.X, claw.Y, 5, along, (-0.35, 0.35), (120, 220));
        one.WhipLeft = WhipS;
        Stages.Of(Layers.Creatures).Flares.Flash(claw.X, claw.Y, (255, 245, 220), 0.45 * Fx.Glow, 10, CreatureLooks.LightKeep, 0.8);
        if (one.Cues is { } cues) _suit.Play(cues.Snap, claw);
    }

    /// <summary>
    /// Its claw at a harpoon line (<see cref="CreaturesModule.CutLeft"/>), sawing: thin cutting sparks thrown off the bite toward the
    /// line and up, thicker as the cut goes on, now and then a white sliver; the bite flickering with each stroke, a little light.
    /// </summary>
    private void Sawing(Shown one, IDebrisQueries debris, double dt, List<LightSource> lights)
    {
        var motion = one.Motion;
        if (motion.Cut is not double done) return;
        var glims = Stages.Of(Layers.Creatures).Glims;
        var claw = one.World(one.Anchors.Cut);
        var toward = LineEnd(debris, one.Piece) is { } end && end.DistanceTo(claw) > 1 ? (end - claw).Normalized() : new Vector2(one.Motion.Face, 0);
        one.LineWay = toward;
        // Off the bite: along the line, and up off the hull.
        double heading = Math.Atan2(toward.Y - 0.8, toward.X);
        double stroke = motion.Stroke;
        for (int i = Due(ref one.SawOwed, (18 + 130 * done) * (0.4 + 0.6 * stroke) * Fx.Count, dt); i > 0; i--)
        {
            double a = heading + glims.Between(-0.7, 0.7), s = glims.Between(50, 150 + 60 * done);
            glims.Add(glims.Between(0, 1) < 0.1 ? Luminous.Snip : Luminous.FineSpark, claw.X, claw.Y, Math.Cos(a) * s, Math.Sin(a) * s);
        }
        var hot = (255, (int)(225 + 25 * stroke), (int)(190 + 50 * stroke));
        Stages.Of(Layers.Creatures).Flares.Bloom(claw.X, claw.Y, 3 + 4 * stroke * (0.5 + 0.5 * done), hot, 0.25 + 0.45 * stroke);
        lights.Add(new LightSource(claw.X, claw.Y, (1f, 0.88f, 0.7f), (float)((0.15 + 0.35 * stroke) * (0.5 + 0.5 * done)), 6,
            CreatureLooks.LightKeep, 1));
    }

    /// <summary>How long the line's cut end whips away as light.</summary>
    private const double WhipS = 0.16;

    /// <summary>The line just cut whipping off along its way from the claw: a thin line of light running out and fading.</summary>
    private static void Whip(Shown one, double dt)
    {
        if (one.WhipLeft <= 0) return;
        one.WhipLeft -= dt;
        double u = 1 - Math.Max(0, one.WhipLeft) / WhipS;
        var claw = one.World(one.Anchors.Cut);
        var from = claw + one.LineWay * (float)(2 + 10 * u);
        var to = claw + one.LineWay * (float)(8 + 34 * u);
        Stages.Of(Layers.Creatures).Flares.Trail(to.X, to.Y, from.X, from.Y, (255, 236, 210), 0.8, 0.9 * (1 - u), 0.3);
    }

    /// <summary>What glows of it this frame: its eye's glow and light, its burner's plume, flame, sparks, smoke and light, its magnets, its wind-up's arcs.</summary>
    private void Effects(Shown one, double dt, List<LightSource> lights)
    {
        var motion = one.Motion;
        var flares = Stages.Of(Layers.Creatures).Flares;
        var glims = Stages.Of(Layers.Creatures).Glims;
        var pose = motion.Pose;
        var eye = one.World(one.Anchors.Eye);
        double amp = Math.Clamp(pose.EyeAmp, 0, 1.7);
        var colour = ((int)pose.Eye.R, (int)pose.Eye.G, (int)pose.Eye.B);
        var whiter = TickArt.Rgb.Mix(pose.Eye, TickArt.White, 0.3);
        flares.Bloom(eye.X, eye.Y, 2 * (4 + 2.5 * amp + (motion.Hot ? 2 : 0)), colour, 0.45 * amp);
        flares.Bloom(eye.X, eye.Y, 3.2, ((int)whiter.R, (int)whiter.G, (int)whiter.B), 0.7 * amp);
        lights.Add(new LightSource(eye.X, eye.Y, ((float)pose.Eye.R / 255, (float)pose.Eye.G / 255, (float)pose.Eye.B / 255),
            CreatureLooks.TickEyeLight * (float)amp * (motion.Hot ? CreatureLooks.TickActingLight : 1), CreatureLooks.LightReach, CreatureLooks.LightKeep, 1));
        if (motion.Mags > 0.05)
            foreach (var foot in one.Anchors.Feet)
            {
                var at = one.World(foot);
                flares.Bloom(at.X, at.Y, 3.2, Magnet, 0.35 * motion.Mags);
            }
        var nozzle = one.World(one.Anchors.Nozzle);
        double thrust = motion.Thrust;
        if (thrust > 0.01)
        {
            var (px, py) = motion.Plume;
            var (vx, vy) = motion.Velocity;
            flares.Bloom(nozzle.X, nozzle.Y, 6.4, Plume, 0.5 * thrust);
            double length = (4 + glims.Between(0, 2.5)) * thrust;
            flares.Trail(nozzle.X, nozzle.Y, nozzle.X + px * length, nozzle.Y + py * length, Plume, 0.6, 0.95, 0.45);
            lights.Add(new LightSource(nozzle.X, nozzle.Y, (1f, 190 / 255f, 90 / 255f), CreatureLooks.TickBurnerLight * (float)thrust,
                CreatureLooks.TickBurnerReach, CreatureLooks.LightKeep, 1));
            double plume = Math.Atan2(py, px);
            for (int i = Due(ref one.BurnOwed, 120 * thrust * Fx.Count, dt); i > 0; i--)
            {
                double a = plume + glims.Between(-0.3, 0.3), s = glims.Between(160, 240);
                glims.Add(Luminous.Burner, nozzle.X + glims.Between(-0.5, 0.5), nozzle.Y + glims.Between(-0.5, 0.5), vx * 0.85 + Math.Cos(a) * s,
                    vy * 0.85 + Math.Sin(a) * s);
            }
            for (int i = Due(ref one.MotesOwed, 36 * thrust * Fx.Count, dt); i > 0; i--)
            {
                double a = plume + glims.Between(-0.5, 0.5), s = glims.Between(40, 110);
                glims.Add(Luminous.BurnerMote, nozzle.X, nozzle.Y, vx * 0.7 + Math.Cos(a) * s, vy * 0.7 + Math.Sin(a) * s);
            }
            for (int i = Due(ref one.SmokeOwed, 22 * thrust * Fx.Count, dt); i > 0; i--)
            {
                double a = plume + glims.Between(-0.4, 0.4), s = glims.Between(20, 50);
                glims.Add(Luminous.BurnerSmoke, nozzle.X, nozzle.Y, vx * 0.6 + Math.Cos(a) * s, vy * 0.6 + Math.Sin(a) * s);
            }
        }
        if (motion.Charge > 0)
        {
            var points = one.Anchors.Arcs;
            for (int i = Due(ref one.ArcsOwed, 16 * motion.Charge + 4, dt); i > 0; i--)
            {
                var (a, b) = (points[_random.Next(points.Count)], points[_random.Next(points.Count)]);
                if (a == b) continue;
                Stages.Of(Layers.Creatures).Bolts.Add(() => (one.World(a).X, one.World(a).Y, one.World(b).X, one.World(b).Y, 0), _random.Next(3, 7), 0.9, 0.3, tint: BoltTint.Red);
            }
        }
        var cues = one.Cues;
        if (one.Burner is { } burner && cues != null)
        {
            one.Burning.Step(thrust > 0.05, dt, S.ThrustAttackS, S.ThrustReleaseS);
            burner.GlobalPosition = nozzle;
            burner.OffsetDb = one.Burning.GainDb;
            if (one.Burning.Playing && !burner.Playing) burner.Play();
            else if (!one.Burning.Playing && burner.Playing) burner.Stop();
        }
    }

    /// <summary>How many of something due at <paramref name="rate"/> a second come in <paramref name="dt"/>, the fraction carried over.</summary>
    private static int Due(ref double owed, double rate, double dt)
    {
        owed += rate * dt;
        int due = (int)Math.Floor(owed);
        owed -= due;
        return Math.Min(due, 8);
    }

    /// <summary><paramref name="one"/> killed where it was drawn: it cracks, heard powering down, and bursts once the cracks have spread.</summary>
    private void Die(Shown one)
    {
        one.Motion.Die();
        if (one.Cues is { } cues) _suit.Play(_clockMs - one.OwnAt < CreatureLooks.OwnMs * 2 || _clockMs - one.ShotAt < CreatureLooks.OwnMs ? S.Felt(cues.Death) : cues.Death, one.Drawn + new Vector2(0, -8));
        _dying.Add(one);
    }

    /// <summary>The killed cracking, spitting sparks from their cracks; each bursting as they have spread, back to the pool.</summary>
    private void Dying(double dt, List<LightSource> lights)
    {
        for (int i = _dying.Count - 1; i >= 0; i--)
        {
            var one = _dying[i];
            one.Motion.Step(default, dt);
            one.Show(one.Drawn);
            var middle = one.World(one.Anchors.Centre);
            var glims = Stages.Of(Layers.Effects).Glims;
            for (int k = Due(ref one.SpitOwed, 30 * Fx.Count, dt); k > 0; k--)
                glims.Add(Luminous.CrackSpit, middle.X + glims.Between(-6, 6), middle.Y + glims.Between(-3, 3), glims.Between(-60, 60), glims.Between(-60, 60));
            Effects(one, dt, lights);
            if (one.Motion.Dying < TickMotion.CracksS) continue;
            Burst(one, middle);
            _dying.RemoveAt(i);
            Put(one);
        }
    }

    /// <summary>
    /// The page's burst, its shell now its pixels (2026-10-29-kills-crumble): sparks every way, dust and smoke, a warm flash, and its sprite
    /// left standing as its pixels to crumble (<see cref="PixelBursts"/>).
    /// </summary>
    private void Burst(Shown one, Vector2 at)
    {
        var glims = Stages.Of(Layers.Effects).Glims;
        glims.Spray(Luminous.BreakSpark, at.X, at.Y, (int)Math.Round(34 * Fx.Count), 0, (-Math.PI, Math.PI), (50, 200));
        for (int i = 0; i < 16; i++) glims.Add(Luminous.BreakDust, at.X, at.Y, glims.Between(-30, 30), glims.Between(-30, 30));
        for (int i = 0; i < 3; i++)
            glims.Add(Luminous.Vapour, at.X + glims.Between(-3, 3), at.Y + glims.Between(-3, 3), glims.Between(-18, 18), glims.Between(-18, 18));
        Stages.Of(Layers.Effects).Flares.Flash(at.X, at.Y, (255, 214, 150), 1.6 * Fx.Glow, 14, CreatureLooks.LightKeep, 0.86);
        if (Bursts == null) return;
        _pixels.Clear();
        one.PaintInto(_pixels);
        Bursts.Crumble(_pixels, at, 0);
    }

    /// <summary>What a killed tick bursts into, shared with the grazers.</summary>
    public PixelBursts? Bursts { get; set; }

    private readonly Dictionary<(int X, int Y), (byte R, byte G, byte B)> _pixels = [];
}
