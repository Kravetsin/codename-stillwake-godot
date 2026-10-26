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
/// cuts snipped in sparks at its claw; killed, cracks spreading till it bursts into sparks and tumbling plates. Heard on the
/// Suit bus, as the wreck it guards is (<see cref="S.Ticks"/>). Drawn between ticks where the sim put it.
/// </summary>
public partial class TickBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    /// <summary>How long after this player's own strike the sim's damage rising is that strike's, already shown.</summary>
    private const double OwnMs = 700;

    private static readonly CanvasItemMaterial Unshaded = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private static readonly CanvasItemMaterial Added = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    private static readonly (int, int, int) Yellow = (255, 209, 51), Plume = (255, 190, 90), Magnet = (255, 170, 84);

    private sealed partial class Shown : Node2D
    {
        public readonly TickArt Art = new();
        public TickMotion Motion = null!;
        public readonly Sprite2D Body = Sprite(), White = Sprite(), Glow = Sprite();
        public readonly Image Albedo = Blank(), Silhouette = Blank(), Emission = Blank();
        public readonly ImageTexture AlbedoTexture, SilhouetteTexture, EmissionTexture;
        public Heard2D? Burner;
        public LoopEnvelope Burning;
        public TickArt.Anchors Anchors = null!;
        public S.TickCues? Cues;
        public Vector2 From, To, Drawn;
        public int Tick = -1, Damage, Cut, Piece;
        public double AlongFrom, AlongTo, OwnAt = double.NegativeInfinity, ArcsOwed, BurnOwed, SmokeOwed, MotesOwed, SpitOwed;
        public string Species = "";

        public Shown()
        {
            AlbedoTexture = ImageTexture.CreateFromImage(Albedo);
            SilhouetteTexture = ImageTexture.CreateFromImage(Silhouette);
            EmissionTexture = ImageTexture.CreateFromImage(Emission);
            (Body.Texture, White.Texture, Glow.Texture) = (AlbedoTexture, SilhouetteTexture, EmissionTexture);
            Lit.Set(Body);
            White.Material = Unshaded;
            Glow.Material = Unshaded;
            AddChild(Body);
            AddChild(Glow);
            AddChild(White);
        }

        private static Sprite2D Sprite() => new()
        {
            Centered = false,
            // Turned about its middle, eight pixels over its feet, as the page turns it.
            Offset = new Vector2(-TickArt.OriginX, -TickArt.OriginY + 8),
            TextureFilter = TextureFilterEnum.Nearest,
        };

        private static Image Blank() => Image.CreateEmpty(TickArt.Width, TickArt.Height, false, Image.Format.Rgba8);

        /// <summary>Its sprite drawn in its motion's pose, at its place, turned and facing as it flies.</summary>
        public void Show(Vector2 feet)
        {
            Anchors = Art.Draw(Motion.Pose);
            Albedo.SetData(TickArt.Width, TickArt.Height, false, Image.Format.Rgba8, Art.Albedo);
            Silhouette.SetData(TickArt.Width, TickArt.Height, false, Image.Format.Rgba8, Art.Silhouette);
            Emission.SetData(TickArt.Width, TickArt.Height, false, Image.Format.Rgba8, Art.Emission);
            AlbedoTexture.Update(Albedo);
            SilhouetteTexture.Update(Silhouette);
            EmissionTexture.Update(Emission);
            White.Modulate = new Color(1, 1, 1, (float)Motion.Flash);
            Position = feet + new Vector2((float)Motion.Shake, -8);
            Rotation = (float)Motion.Angle;
            Scale = new Vector2(Motion.Face, 1);
        }

        /// <summary>Where a point of its sprite (<see cref="TickArt.Anchors"/>) is in the world this frame, the middle of its pixel.</summary>
        public Vector2 World((double X, double Y) at) => Transform * new Vector2((float)at.X + 0.5f, (float)at.Y + 8.5f);
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Stack<Shown> _pool = new();
    private readonly List<Shown> _dying = [];
    private readonly HashSet<int> _seen = [], _alive = [];
    private readonly List<int> _gone = [];
    private readonly Voices _suit = new(S.Suit, voices: 4), _world = new(S.World, voices: 4);
    private readonly Scrap _scrap = new(), _scrapHot = new() { Hot = true };
    private readonly Random _random = new();
    private double _clockMs;
    private IReadOnlyList<(float X, float Y)> _heroes = [];

    public override void _Ready()
    {
        AddChild(_suit);
        AddChild(_world);
        Lit.Set(_scrap);
        _scrapHot.Material = Added;
        AddChild(_scrap);
        AddChild(_scrapHot);
    }

    /// <summary>
    /// A frame of the creatures in <paramref name="ticks"/> (those drawn as the tick), tick <paramref name="tick"/> drawn
    /// <paramref name="progress"/> of the way to the next, watching the heroes drawn at <paramref name="heroes"/> (their chests);
    /// what their lamps and burners light goes into <paramref name="lights"/>.
    /// </summary>
    public void Frame(CreaturesModule creatures, IDebrisQueries debris, IReadOnlyList<Creature> ticks, int tick, double progress,
        double elapsedMs, IReadOnlyList<(float X, float Y)> heroes, List<LightSource> lights)
    {
        _clockMs += elapsedMs;
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
                if (_clockMs - one.OwnAt > OwnMs) Struck(one, Spec.Weapons.Axe.Item, own: false);
                one.Damage = c.Damage;
            }
            var moments = one.Motion.Step(SeenOf(one, c, progress), dt);
            one.Drawn = feet;
            one.Show(feet);
            Moments(one, moments);
            Cut(one, c);
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
        _scrap.Step(dt);
        _scrapHot.Share(_scrap);
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
        _scrap.Clear();
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
    private TickMotion.Seen SeenOf(Shown one, Creature c, double progress)
    {
        var spec = Spec.Creatures.Get(c.Species);
        bool clamped = CreaturesModule.Clamped(c);
        var act = c.Act;
        bool winding = act is { Phase: ActPhase.WindUp }, pouncing = act is { Phase: ActPhase.Active };
        double windUpS = 0;
        if (winding && spec.Moves[act!.Move] is Spec.Creatures.Pounce pounce)
            windUpS = (pounce.WindUp - act.Left + Math.Clamp(progress, 0, 1)) / World.TicksPerSecond;
        double perTick = Cell / (double)Unit * World.TicksPerSecond;
        double crawl = clamped ? (one.AlongTo - one.AlongFrom) * perTick : 0;
        var middle = one.Drawn + new Vector2(0, -8);
        (double, double)? hero = Nearest(middle) is { } h ? (h.X - middle.X, h.Y - middle.Y) : null;
        return new TickMotion.Seen(clamped, CreaturesModule.Alerted(c), winding, windUpS, pouncing, pouncing ? act!.Left : 0,
            c.VX * perTick, c.VY * perTick, crawl, hero);
    }

    /// <summary>The chest of the hero nearest <paramref name="at"/>, as the crew drew them this frame; none with no hero.</summary>
    private Vector2? Nearest(Vector2 at)
    {
        Vector2? nearest = null;
        float best = float.MaxValue;
        foreach (var (x, y) in _heroes)
        {
            var hero = new Vector2(x, y);
            float d = hero.DistanceSquaredTo(at);
            if (d >= best) continue;
            (best, nearest) = (d, hero);
        }
        return nearest;
    }

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
        Sparks(at, heading, 1);
        if (one.Cues is { } cues) (own ? _suit : _world).Play(cues.Hit, at, 1, GroundSounds.HitDbOf(tool));
    }

    /// <summary>The page's spark burst, its <c>fine</c> sparks: a spray along <paramref name="heading"/>, dust, a warm flash.</summary>
    private static void Sparks(Vector2 at, double heading, double scale)
    {
        var glims = Glims.Shared;
        glims.Spray(Luminous.FineSpark, at.X, at.Y, (int)Math.Round(28 * scale * Fx.Count), heading, (-1.1, 1.1), (70, 230));
        for (int i = 0; i < Math.Round(5 * scale); i++) glims.Add(Luminous.HitDust, at.X, at.Y, glims.Between(-20, 20), glims.Between(-20, 20));
        Flares.Shared.Flash(at.X, at.Y, (255, 220, 140), 0.5 * scale * Fx.Glow, 8, CreatureLooks.LightKeep, 0.8);
    }

    /// <summary>Its moments seen and heard: a start and a flash at its eye, the wind-up's servo, letting go and clamping on, a braking flight, a hero struck.</summary>
    private void Moments(Shown one, TickMotion.Moment m)
    {
        if (m == TickMotion.Moment.None) return;
        var glims = Glims.Shared;
        var eye = one.World(one.Anchors.Eye);
        var cues = one.Cues;
        if (m.HasFlag(TickMotion.Moment.Notice))
        {
            Flares.Shared.Flash(eye.X, eye.Y, Yellow, 0.6 * Fx.Glow, 8, CreatureLooks.LightKeep, 0.8);
            Flares.Shared.Glint(eye.X, eye.Y, 4, (255, 250, 220));
            if (cues != null) _suit.Play(cues.Alert, eye);
        }
        if (m.HasFlag(TickMotion.Moment.WindUp) && cues != null) _suit.Play(cues.WindUp, eye);
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
            glims.Spray(Luminous.HitSpark, hero.X, hero.Y, (int)Math.Round(10 * Fx.Count), heading, (-1, 1), (60, 180));
            Flares.Shared.Flash(hero.X, hero.Y, (255, 200, 140), 0.6 * Fx.Glow, 6, CreatureLooks.LightKeep, 0.8);
        }
    }

    /// <summary>Its feet meeting or leaving the hull: pale streaks along it, a glint, dust and a veil at each near foot.</summary>
    private static void Clack(Shown one)
    {
        var glims = Glims.Shared;
        foreach (var foot in one.Anchors.Feet)
        {
            var at = one.World((foot.X, foot.Y + 0.5));
            for (int i = 0; i < 3; i++)
                glims.Add(Luminous.LandStreak, at.X, at.Y, (glims.Between(0, 1) < 0.5 ? -1 : 1) * glims.Between(50, 120), -glims.Between(0, 15));
            Flares.Shared.Glint(at.X, at.Y - 0.5, 2.5, (255, 255, 255));
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
        double up = -Math.PI / 2 - 0.6 * one.Motion.Face;
        Sparks(claw, up, 0.9);
        Glims.Shared.Spray(Luminous.Snip, claw.X, claw.Y, 3, -Math.PI / 2 + (Math.PI / 2 - 0.7) * one.Motion.Face, (-0.5, 0.5), (90, 160));
        Flares.Shared.Flash(claw.X, claw.Y, (255, 245, 220), 0.45 * Fx.Glow, 10, CreatureLooks.LightKeep, 0.8);
        if (one.Cues is { } cues) _suit.Play(cues.Snap, claw);
    }

    /// <summary>What glows of it this frame: its eye's glow and light, its burner's plume, flame, sparks, smoke and light, its magnets, its wind-up's arcs.</summary>
    private void Effects(Shown one, double dt, List<LightSource> lights)
    {
        var motion = one.Motion;
        var flares = Flares.Shared;
        var glims = Glims.Shared;
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
                Bolts.Shared.Add(() => (one.World(a).X, one.World(a).Y, one.World(b).X, one.World(b).Y, 0), _random.Next(3, 7), 0.9, 0.3, tint: BoltTint.Red);
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
        if (one.Cues is { } cues) _suit.Play(cues.Death, one.Drawn + new Vector2(0, -8));
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
            var glims = Glims.Shared;
            for (int k = Due(ref one.SpitOwed, 30 * Fx.Count, dt); k > 0; k--)
                glims.Add(Luminous.CrackSpit, middle.X + glims.Between(-6, 6), middle.Y + glims.Between(-3, 3), glims.Between(-60, 60), glims.Between(-60, 60));
            Effects(one, dt, lights);
            if (one.Motion.Dying < TickMotion.CracksS) continue;
            Burst(one, middle);
            _dying.RemoveAt(i);
            Put(one);
        }
    }

    /// <summary>The page's burst: sparks every way, dust and smoke, a warm flash, four plates of its shell tumbling off hot.</summary>
    private void Burst(Shown one, Vector2 at)
    {
        var glims = Glims.Shared;
        glims.Spray(Luminous.BreakSpark, at.X, at.Y, (int)Math.Round(34 * Fx.Count), 0, (-Math.PI, Math.PI), (50, 200));
        for (int i = 0; i < 16; i++) glims.Add(Luminous.BreakDust, at.X, at.Y, glims.Between(-30, 30), glims.Between(-30, 30));
        for (int i = 0; i < 3; i++)
            glims.Add(Luminous.Vapour, at.X + glims.Between(-3, 3), at.Y + glims.Between(-3, 3), glims.Between(-18, 18), glims.Between(-18, 18));
        Flares.Shared.Flash(at.X, at.Y, (255, 214, 150), 1.6 * Fx.Glow, 14, CreatureLooks.LightKeep, 0.86);
        for (int i = 0; i < 4; i++) _scrap.Throw(at, _random);
    }

    /// <summary>
    /// The plates a tick bursts into (the page's chunks): each a few pixels of its shell, its top edge lit and its outline dark,
    /// tumbling and slowing, trailing embers its first half second, glowing hot over its first second; gone in 2.4 s. One drawn
    /// lit, its twin (<see cref="Hot"/>) the glow added over it.
    /// </summary>
    private sealed partial class Scrap : Node2D
    {
        private sealed class Plate
        {
            public Vector2 At, Velocity;
            public float Angle, Spin, Age, EmberOwed;
            public int Width, Height;
        }

        private List<Plate> _plates = [];

        /// <summary>Whether this draws the plates' glow rather than the plates.</summary>
        public bool Hot { get; init; }

        public void Throw(Vector2 at, Random random)
        {
            double a = random.NextDouble() * Math.Tau, s = 18 + random.NextDouble() * 37;
            _plates.Add(new Plate
            {
                At = at,
                Velocity = new Vector2((float)(Math.Cos(a) * s), (float)(Math.Sin(a) * s - 10)),
                Angle = (float)(random.NextDouble() * Math.Tau),
                Spin = (float)(random.NextDouble() * 10 - 5),
                Width = 3 + random.Next(3),
                Height = 2 + random.Next(2),
            });
        }

        public void Step(double dt)
        {
            var glims = Glims.Shared;
            for (int i = _plates.Count - 1; i >= 0; i--)
            {
                var p = _plates[i];
                p.Age += (float)dt;
                if (p.Age > 2.4f)
                {
                    _plates.RemoveAt(i);
                    continue;
                }
                float keep = (float)Math.Exp(-0.5 * dt);
                p.Velocity *= keep;
                p.At += p.Velocity * (float)dt;
                p.Angle += p.Spin * (float)dt;
                p.Spin *= (float)Math.Exp(-0.3 * dt);
                float hot = Math.Clamp(1 - p.Age / 1.3f, 0, 1);
                if (hot > 0) Flares.Shared.Bloom(p.At.X, p.At.Y, 4.4, (255, 130, 50), hot * 0.25);
                if (p.Age >= 0.55f) continue;
                p.EmberOwed += (float)(40 * dt);
                for (; p.EmberOwed >= 1; p.EmberOwed--)
                    glims.Add(Luminous.ShardEmber, p.At.X, p.At.Y, -p.Velocity.X * 0.3 + glims.Between(-10, 10), -p.Velocity.Y * 0.3 + glims.Between(-10, 10));
            }
            QueueRedraw();
        }

        /// <summary>Draws the plates <paramref name="of"/> throws, as their glow.</summary>
        public void Share(Scrap of)
        {
            _plates = of._plates;
            QueueRedraw();
        }

        public void Clear() => _plates.Clear();

        public override void _Draw()
        {
            foreach (var p in _plates)
            {
                DrawSetTransform(p.At, p.Angle);
                var box = new Rect2(-p.Width / 2f, -p.Height / 2f, p.Width, p.Height);
                if (Hot)
                {
                    float a = Math.Clamp(1 - p.Age / 1.3f, 0, 1);
                    if (a <= 0) continue;
                    DrawRect(box, new Color(1, 130 / 255f, 50 / 255f, a * 0.85f));
                    continue;
                }
                float fade = p.Age > 1.8f ? 1 - (p.Age - 1.8f) / 0.6f : 1;
                var outline = Tone(TickArt.Outline, fade);
                DrawRect(box, outline);
                DrawRect(new Rect2(box.Position, box.Size - Vector2.One), Tone(TickArt.Dark[2], fade));
                DrawRect(new Rect2(box.Position, new Vector2(box.Size.X - 1, 1)), Tone(TickArt.Dark[4], fade));
            }
            DrawSetTransform(Vector2.Zero, 0);
        }

        private static Color Tone(TickArt.Rgb c, float alpha) => new((float)c.R / 255, (float)c.G / 255, (float)c.B / 255, Math.Clamp(alpha, 0, 1));
    }
}
