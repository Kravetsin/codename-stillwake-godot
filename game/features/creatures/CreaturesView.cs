using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The creatures (features/creatures, 0102): each drawn between ticks where the sim put it, facing the way it goes, its body moved
/// by <see cref="CreatureMotion"/>. A strike of this player's own reacts the frame it is sent — white flash, squash, flinch, a
/// wide eye, a shake, its hit thrown and heard by what it struck (0093, 0094) — and the knock follows as the sim moves
/// it; a friend's shows as the sim's damage rises. One killed in view bursts into a puff. A creature still watches the hero
/// nearest it. Only creatures within the camera's view and a margin are drawn, from a pool that never frees.
/// </summary>
public partial class CreaturesView(Ui ui) : Node2D, ILightSource
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit, Margin = 8;

    /// <summary>How long after this player's own strike on a creature the sim's damage rising is that strike's, already shown.</summary>
    private const double OwnMs = 700;

    /// <summary>How near a hero a creature watches it, in pixels.</summary>
    private const float Watch = 18 * Cell;


    /// <summary>A rise faster than this a tick is a jump, not a blow's hop (an axe's is 360).</summary>
    private const int JumpTell = 500;

    /// <summary>What is shown of a creature: its body, its motion, gait and pose, where it was on the tick before and on the last, its damage, its steps.</summary>
    private sealed class Shown(CreatureBody body, CreatureGait gait, CreaturePose pose)
    {
        public CreatureBody Body { get; } = body;
        public CreatureMotion Motion { get; set; } = new();
        public CreatureGait Gait { get; } = gait;
        public CreaturePose Pose { get; } = pose;
        /// <summary>How long it has stood idle with no hero near, how much it grazes and how much it watches a hero, eased.</summary>
        public double AloneMs { get; set; }
        public double Graze { get; set; }
        public double Notice { get; set; }
        public Vector2 From { get; set; }
        public Vector2 To { get; set; }
        public int Tick { get; set; } = -1;
        public int Damage { get; set; }
        public Vector2 Drawn { get; set; }
        public bool Grounded { get; set; }
        public double OwnAt { get; set; } = double.NegativeInfinity;
        /// <summary>When this player's own shot last hit it: its blow and kill heard as its own, from afar.</summary>
        public double ShotAt { get; set; } = double.NegativeInfinity;
        public string Species { get; set; } = "";
        /// <summary>Its steps up and down, two cells at once in the sim, drawn eased (0106): upright on them, never tilted.</summary>
        public StepEase Steps { get; set; } = NewSteps();
        /// <summary>Its speed down as last seen, and the fastest it fell since it left the ground: for its jumps and landings.</summary>
        public int LastVy { get; set; }
        public int Fall { get; set; }
        /// <summary>Its mode as last drawn, so a wind-up begun snorts once; and the time to the next scrape of its feet.</summary>
        public Posture ModeWas { get; set; }
        public double ScrapeMs { get; set; }
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Stack<CreatureBody> _pool = new();
    private readonly HashSet<int> _out = [], _drawn = [];
    private readonly List<int> _gone = [];
    // By reference: two strikes of one creature are equal records, and one may run while the other flies.
    private HashSet<Command> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    private readonly Voices _ownHits = new(S.Suit, voices: 4), _worldHits = new(S.World, voices: 4);
    private double _clockMs;
    private CreaturesModule? _of;

    /// <summary>The creatures drawn last frame, and the bodies ever made: for the frame log and scripted runs.</summary>
    public (int Drawn, int Made) Bodies => (_shown.Count, _shown.Count + _pool.Count + _held.Count);

    private readonly List<(Vector2 At, Color Colour)> _lights = [];
    private readonly List<LightSource> _sources = [];

    /// <summary>The light of each glowing part of a creature drawn this frame, faded as it dies: the light view's.</summary>
    public IReadOnlyList<LightSource> Lights => _sources;

    public override void _Ready()
    {
        ZIndex = Layers.Creatures;
        AddChild(_ownHits);
        AddChild(_worldHits);
        AddChild(_ticks);
        AddChild(_boxes);
        AddChild(_bursts);
        _ticks.Bursts = _bursts;
    }

    /// <summary>What every killed creature bursts into, the tick too (2026-10-29-kill-feel).</summary>
    private readonly PixelBursts _bursts = new();

    /// <summary>The creatures drawn as the tick (<see cref="CreatureLooks.Ticks"/>): posed and drawn anew each frame, not a rig.</summary>
    private readonly TickBodies _ticks = new();

    private readonly List<Creature> _ticked = [];

    /// <summary>The creatures with no art yet (<see cref="CreatureLooks.Boxes"/>).</summary>
    private readonly PlaceholderBodies _boxes = new();

    private readonly List<Creature> _boxed = [];

    /// <summary>The creatures this player's own shots hit this tick (<see cref="OwnShots"/>).</summary>
    private readonly HashSet<int> _shotByOwn = [];

    /// <summary>
    /// Shows <paramref name="creatures"/>'s creatures within <paramref name="view"/> (cells) and <see cref="Margin"/> round it,
    /// <paramref name="progress"/> of the way from the last tick to the next, recoloured by <paramref name="palette"/> (the planet's,
    /// <see cref="CreatureLooks.PaletteOf"/>), each creature its own individual by its id and the surface's <paramref name="seed"/> (<see cref="CreatureVariety"/>).
    /// </summary>
    public void Frame(Client client, CreaturesModule creatures, IDebrisQueries debris, IGridQueries grid, IRoomsQueries rooms, Spec.Surfaces.Kind rules,
        Rect2 view, double progress, double elapsedMs, string palette, uint seed)
    {
        _clockMs += elapsedMs;
        // Another surface, or a load: what was shown was another world's, gone without a puff.
        if (creatures != _of)
        {
            foreach (var one in _shown.Values) Put(one.Body);
            foreach (var held in _held) Put(held.Body);
            _shown.Clear();
            _held.Clear();
            _bursts.Clear();
            _ticks.Reset();
            _of = creatures;
        }
        (_palette, _seed) = (palette, seed);
        _bursts.On(grid, rooms, rules);
        OwnShots.Of(creatures.Hits, client.Id, _shotByOwn);
        foreach (int id in _shotByOwn)
            if (_shown.TryGetValue(id, out var shot)) shot.ShotAt = _clockMs;
        var all = creatures.Creatures;
        int left = (int)Math.Floor(view.Position.X - Margin) * Unit, right = (int)Math.Ceiling(view.End.X + Margin) * Unit;
        int top = (int)Math.Floor(view.Position.Y - Margin) * Unit, bottom = (int)Math.Ceiling(view.End.Y + Margin) * Unit;
        _out.Clear();
        _drawn.Clear();
        _ticked.Clear();
        _boxed.Clear();
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            _out.Add(c.Id);
            if (c.X < left || c.X > right || c.Y < top || c.Y > bottom) continue;
            if (CreatureLooks.Varieties.ContainsKey(c.Species)) _drawn.Add(c.Id);
            else if (CreatureLooks.Ticks.Contains(Spec.Creatures.Get(c.Species).Look)) _ticked.Add(c);
            else if (CreatureLooks.Boxes.Contains(Spec.Creatures.Get(c.Species).Look)) _boxed.Add(c);
        }
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_drawn.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var gone = _shown[id];
            _shown.Remove(id);
            // Gone from the world while in view: killed, as nothing is taken away where a hero sees it.
            if (!_out.Contains(id)) Die(gone);
            else Put(gone.Body);
        }
        Holds(elapsedMs);
        _bursts.Frame(elapsedMs);

        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (!_drawn.Contains(c.Id)) continue;
            if (!_shown.TryGetValue(c.Id, out var one))
            {
                one = Take(c);
                _shown[c.Id] = one;
            }
            Follow(one, c, client.World.Tick);
        }
        OwnStrikes(client.InFlight);

        foreach (var c in all)
        {
            if (!_shown.TryGetValue(c.Id, out var one)) continue;
            if (c.Damage > one.Damage)
            {
                // A friend's strike, or one of this player's own already shown as it was sent.
                if (_clockMs - one.OwnAt > OwnMs) Struck(one, ToolOf(one.Drawn), own: _clockMs - one.ShotAt < OwnMs);
                one.Damage = c.Damage;
            }
            Place(one, c, progress, elapsedMs);
        }
        // What their glowing parts light this frame: the fluff's spots in a cave, the tapir's pod at dusk.
        _lights.Clear();
        foreach (var one in _shown.Values) one.Body.Lights(_lights, one.Body.Transform, 1);
        foreach (var held in _held) held.Body.Lights(_lights, held.Body.Transform, 1);
        _sources.Clear();
        foreach (var (at, colour) in _lights)
            _sources.Add(new LightSource(at.X, at.Y, (colour.R, colour.G, colour.B), CreatureLooks.LightStrength * colour.A, CreatureLooks.LightReach,
                CreatureLooks.LightKeep, 1));
        _boxes.Frame(_boxed);
        _ticks.Frame(creatures, debris, _ticked, client.World.Tick, progress, elapsedMs, ui.Heroes, _sources, _shotByOwn);
        if (Trace != null) Traced(all);
    }

    /// <summary><c>--creatures-trace</c>: each creature drawn, once a second, its strides that second, its speed and what it did.</summary>
    public Action<string>? Trace { get; set; }

    private double _tracedAt;
    private readonly Dictionary<int, double> _tracedPhase = [];

    /// <summary><c>--creatures-trace=frames</c>: also every frame, each creature drawn, where on the screen and its strides, for recordings.</summary>
    public bool TraceFrames { get; set; }


    private void Traced(IReadOnlyList<Creature> all)
    {
        if (TraceFrames)
            foreach (var c in all)
                if (_shown.TryGetValue(c.Id, out var one))
                {
                    var at = GetViewport().GetCanvasTransform() * one.Body.Position;
                    Trace!($"creatures frame {Engine.GetProcessFrames()}: {c.Species} {c.Id} {c.Posture} at {at.X:F0},{at.Y:F0} stride {one.Gait.Phase:F3} gallop {one.Gait.Gallop:F2} air {one.Gait.Air:F2}");
                }
        if (_clockMs - _tracedAt < 1000) return;
        double seconds = (_clockMs - _tracedAt) / 1000;
        _tracedAt = _clockMs;
        foreach (var c in all)
        {
            if (!_shown.TryGetValue(c.Id, out var one)) continue;
            double was = _tracedPhase.GetValueOrDefault(c.Id, one.Gait.Phase);
            _tracedPhase[c.Id] = one.Gait.Phase;
            Trace!($"creatures trace: {c.Species} {c.Id} {c.Posture}: {(one.Gait.Phase - was) / seconds:F2} strides/s at {one.Gait.Speed:F0} px/s, gallop {one.Gait.Gallop:F2}");
        }
    }

    private string _palette = CreatureLooks.Palette;
    private uint _seed;

    /// <summary>
    /// Every strike of this player's own seen in flight for the first time: its creature reacts now, a round trip before the sim's
    /// knock (0014). Read every frame after the hero's strikes are sent.
    /// </summary>
    private void OwnStrikes(IReadOnlyList<Command> inFlight)
    {
        _still.Clear();
        for (int i = 0; i < inFlight.Count; i++)
        {
            if (inFlight[i] is not Strike strike) continue;
            _still.Add(strike);
            if (_flying.Contains(strike)) continue;
            if (!_shown.TryGetValue(strike.Creature, out var one))
            {
                _ticks.OwnStrike(strike.Creature, strike.Tool);
                continue;
            }
            one.OwnAt = _clockMs;
            Struck(one, strike.Tool, own: true);
        }
        (_flying, _still) = (_still, _flying);
    }

    /// <summary>
    /// A blow on <paramref name="one"/> with <paramref name="tool"/>: it flashes, squashes, flinches away from the hero nearest it
    /// and shakes a moment, moving on as the sim moves it; its hit thrown from the side facing that hero and heard, this player's own through the suit.
    /// </summary>
    private void Struck(Shown one, string tool, bool own)
    {
        var spec = Spec.Creatures.Get(one.Species);
        var middle = one.Drawn + new Vector2(0, -spec.Body.Height * Cell / 2 * one.Body.Size);
        var hero = NearestHero(middle);
        int away = middle.X >= hero.X ? 1 : -1;
        double share = Impacts.ShareOf(tool);
        one.Motion.Hit(away, share);
        var impact = HitLooks.Creatures.GetValueOrDefault(one.Species, Impact.Flesh);
        var side = middle + new Vector2(-away * spec.Body.Width * Cell * 0.4f * one.Body.Size, Mathf.Clamp(hero.Y - middle.Y, -6, 6));
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], side.X, side.Y, -away, -0.25, share, floor: one.Drawn.Y);
        if (S.Creatures.TryGetValue(one.Species, out var cues))
        {
            var voices = own ? _ownHits : _worldHits;
            float db = Features.Sound.GroundSounds.HitDbOf(tool);
            voices.Play(own ? S.Felt(cues.Hit) : cues.Hit, side, cues.Hit.Tune, db);
            voices.Play(own ? S.Felt(cues.Thump) : cues.Thump, side, cues.Thump.Tune, db);
        }
    }

    /// <summary>The tool a friend most likely struck with at <paramref name="at"/>: a running drill whose bit is by it, else the axe.</summary>
    private string ToolOf(Vector2 at)
    {
        foreach (var (x, y, tool) in ui.Bits)
            if (new Vector2(x, y).DistanceTo(at) < 6 * Cell) return tool;
        return Spec.Weapons.Axe.Item;
    }

    /// <summary><paramref name="one"/>'s tick positions kept, the tick before's and the last's, for drawing between them.</summary>
    private static void Follow(Shown one, Creature c, int tick)
    {
        var at = new Vector2(c.X, c.Y);
        if (one.Tick < 0 || tick - one.Tick > 1) (one.From, one.To) = (at, at);
        else if (tick != one.Tick) (one.From, one.To) = (one.To, at);
        one.Tick = tick;
    }

    /// <summary>A body's steps, closing one of <c>Creatures.StepUp</c> cells in <see cref="CreatureLooks.StepMs"/>.</summary>
    private static StepEase NewSteps() => new(Spec.Creatures.StepUp * Cell, CreatureLooks.StepMs);

    private void Place(Shown one, Creature c, double progress, double elapsedMs)
    {
        float p = (float)Math.Clamp(progress, 0, 1);
        var at = one.From.Lerp(one.To, p) / Unit * Cell;
        var spec = Spec.Creatures.Get(c.Species);
        bool grounded = c.VY == 0;
        // Where the sim stands it, drawn between ticks as it moves; its whole steps up and down eased, by time, not by frame.
        at.Y += (float)one.Steps.Offset(at.X, at.Y, grounded && one.Grounded, elapsedMs);
        var motion = one.Motion;
        // Never held where it was struck: a neutral creature bolts at the blow, and its drawing goes with it, only shaking.
        var drawn = at;
        var watched = NearestHero(drawn + new Vector2(0, -spec.Body.Height * Cell / 2));
        var toHero = watched - (drawn + new Vector2(0, -spec.Body.Height * Cell * 0.6f));
        bool near = toHero.Length() < Watch;
        int? watch = near && Math.Abs(toHero.X) > 2 ? Math.Sign(toHero.X) : null;
        // A jump read off the sim: a leap up faster than any blow's hop, from the ground; a landing as hard as the fall was fast.
        if (c.VY <= -JumpTell && one.LastVy >= 0) motion.Jump();
        if (!grounded) one.Fall = Math.Max(one.Fall, c.VY);
        else if (!one.Grounded)
        {
            motion.Land((double)one.Fall / Spec.Creatures.MaxFall);
            one.Fall = 0;
        }
        one.LastVy = c.VY;
        motion.Bracing = c.Posture == Posture.WindUp;
        // Winding up it faces the hero it will charge, however far: it turned back on it (0103).
        if (c.Posture == Posture.WindUp && Math.Abs(toHero.X) > 2) watch = Math.Sign(toHero.X);
        motion.Step(elapsedMs, drawn.X - one.Drawn.X, grounded && !one.Grounded, c.VX == 0 ? watch : null);
        Rams(one, c, spec, drawn, grounded, elapsedMs);
        // Its legs go by the ground the sim moved it on its last tick, never by a frame's step: a frame's is uneven, a tick's is not.
        double speed = Math.Abs(one.To.X - one.From.X) / Unit * Cell * World.TicksPerSecond;
        bool fleeing = c.Fleeing;
        var gait = one.Gait;
        gait.Step(elapsedMs, speed, grounded, fleeing);
        // Idle with no hero by, a while, it grazes; idle with one near, it watches it.
        bool idle = c.Posture == Posture.Idle && grounded;
        one.AloneMs = idle && !near ? one.AloneMs + elapsedMs : 0;
        one.Graze += ((one.AloneMs > CreatureLooks.GrazeAfterMs ? 1 : 0) - one.Graze) * CreatureGait.Ease(elapsedMs, CreatureLooks.GrazeMs);
        one.Notice += ((idle && near ? 1 : 0) - one.Notice) * CreatureGait.Ease(elapsedMs, CreatureLooks.BlendMs);
        one.Grounded = grounded;
        one.Drawn = drawn;
        // Its eye on the hero it watches, in the body's own way: mirrored with it.
        (double, double)? look = null;
        if (near && toHero.LengthSquared() > 0)
        {
            var l = new Vector2(toHero.X * motion.Facing, toHero.Y).Normalized();
            look = (l.X, l.Y);
        }
        var pose = one.Pose;
        pose.Set(new CreaturePose.Doing(gait.Phase, gait.Walking, gait.Gallop, gait.Air, gait.Flee, one.Graze, one.Notice, motion.Clock,
            motion.Ears(gait.Flee), motion.Eye, look));
        var (sx, sy) = motion.Scale(gait.Flee);
        var body = one.Body;
        body.Position = drawn + new Vector2((float)motion.Shake, 0);
        body.Scale = new Vector2((float)(sx * (1 + pose.Stretch)) * motion.Facing, (float)(sy * (1 - pose.Stretch * 0.7))) * body.Size;
        body.Rotation = (float)motion.Tilt(gait.Flee);
        body.Pose(pose, motion.Flash);
    }

    /// <summary>
    /// A ram's tell (0103): a wind-up begun snorts a puff off its snout, heard where it is; crouched (<see cref="CreatureMotion.Bracing"/>)
    /// its hind feet scrape dust back, and charging it throws dust behind it faster.
    /// </summary>
    private void Rams(Shown one, Creature c, Spec.Creatures.Species spec, Vector2 feet, bool grounded, double elapsedMs)
    {
        int facing = one.Motion.Facing;
        float wide = spec.Body.Width * Cell * one.Body.Size;
        if (c.Posture == Posture.WindUp && one.ModeWas != Posture.WindUp)
        {
            var snout = feet + new Vector2(facing * wide * 0.5f, -spec.Body.Height * Cell * 0.45f * one.Body.Size);
            var glims = Stages.Of(Layers.Creatures).Glims;
            for (int i = 0; i < 6; i++)
                glims.Add(Puff, snout.X, snout.Y, facing * glims.Between(10, 28), glims.Between(-8, 2), size: glims.Between(2, 3.5));
            _worldHits.Play(S.Snort, snout, S.Snort.Tune, 0);
            one.ScrapeMs = 0;
        }
        one.ModeWas = c.Posture;
        if (!grounded || c.Posture is not (Posture.WindUp or Posture.Charge)) return;
        one.ScrapeMs -= elapsedMs;
        if (one.ScrapeMs > 0) return;
        bool charging = c.Posture == Posture.Charge;
        one.ScrapeMs = charging ? ChargeDustMs : ScrapeEveryMs;
        var heel = feet + new Vector2(-facing * wide * 0.32f, -0.5f);
        var dust = Stages.Of(Layers.Creatures).Glims;
        for (int i = 0; i < (charging ? 2 : 3); i++)
            dust.Add(Luminous.LandDust, heel.X + dust.Between(-1.5, 1.5), heel.Y, -facing * dust.Between(18, charging ? 40 : 60), -dust.Between(4, 16), feet.Y);
    }

    /// <summary>How often a crouched rammer scrapes its feet, and a charging one throws dust, in milliseconds.</summary>
    private const double ScrapeEveryMs = 130, ChargeDustMs = 60;

    /// <summary>A body for <paramref name="c"/> come into view, from the pool or new, unmoved and unstruck.</summary>
    private Shown Take(Creature c)
    {
        var body = _pool.Count > 0 ? _pool.Pop() : Made();
        var individual = CreatureVariety.Of(CreatureLooks.Varieties[c.Species], _seed, c.Id);
        body.Dress(c.Species, _palette, individual);
        Trace?.Invoke($"creatures look: {c.Species} {c.Id} {individual}");
        body.Visible = true;
        var at = new Vector2(c.X, c.Y);
        var design = body.Design!;
        var run = CreatureLooks.Runs[design.Id];
        return new Shown(body, new CreatureGait(run), new CreaturePose(design, run))
        {
            Damage = c.Damage,
            Species = c.Species,
            From = at,
            To = at,
            Drawn = at / Unit * Cell,
            Grounded = c.VY == 0,
        };
    }

    private CreatureBody Made()
    {
        var body = new CreatureBody { Visible = false };
        AddChild(body);
        return body;
    }

    private void Put(CreatureBody body)
    {
        body.Visible = false;
        _pool.Push(body);
    }

    /// <summary>
    /// <paramref name="one"/> killed where it was drawn (2026-10-29-kill-feel): a white glint, a shock ring and its death
    /// heard at once, its body held white <see cref="KillLooks.PopMs"/>, the world running on; then it crumbles (<see cref="Pop"/>).
    /// </summary>
    private void Die(Shown one)
    {
        var spec = Spec.Creatures.Get(one.Species);
        var middle = one.Drawn + new Vector2(0, -spec.Body.Height * Cell / 2 * one.Body.Size);
        Stages.Of(Layers.Effects).Flares.Glint(middle.X, middle.Y, 12, (255, 255, 255));
        Stages.Of(Layers.Effects).Flares.Shock(middle.X, middle.Y, 3, 30, 200, (255, 246, 236), 0.5 * Fx.Glow, 0.35);
        bool own = _clockMs - one.OwnAt < OwnMs * 2 || _clockMs - one.ShotAt < OwnMs;
        if (S.Creatures.TryGetValue(one.Species, out var cues))
            (own ? _ownHits : _worldHits).Play(own ? S.Felt(cues.Death) : cues.Death, middle, cues.Death.Tune, 0);
        one.Body.Whiten();
        _held.Add(new Held(one.Body, KillLooks.PopMs, one.Species, middle, one.Drawn.Y, middle.X >= NearestHero(middle).X ? 1 : -1));
    }

    /// <summary>A killed body held white: how long yet, what it was, its middle and feet, the striker's other side.</summary>
    private sealed record Held(CreatureBody Body, double LeftMs, string Species, Vector2 Middle, float Feet, int Away);

    private readonly List<Held> _held = [];
    private readonly Dictionary<(int X, int Y), (byte R, byte G, byte B)> _pixels = [];

    /// <summary>The soft puff a creature leaves: a warm wisp, swelling and gone.</summary>
    private static readonly Luminous Puff = Luminous.Vapour with { Ramp = [(240, 226, 206)], Alpha = 0.2, LifeMs = (500, 900) };

    /// <summary>The held counting down; each crumbling once its hold is over, back to the pool.</summary>
    private void Holds(double elapsedMs)
    {
        for (int i = _held.Count - 1; i >= 0; i--)
        {
            var held = _held[i];
            if (held.LeftMs > elapsedMs)
            {
                _held[i] = held with { LeftMs = held.LeftMs - elapsedMs };
                continue;
            }
            _held.RemoveAt(i);
            Pop(held);
            Put(held.Body);
        }
    }

    /// <summary>A held body left standing as its pixels to crumble (<see cref="PixelBursts"/>), with its hit thrown whole and a soft puff.</summary>
    private void Pop(Held held)
    {
        _pixels.Clear();
        held.Body.Pixels(_pixels);
        _bursts.Crumble(_pixels, held.Middle, held.Away);
        var (middle, feet) = (held.Middle, held.Feet);
        var impact = HitLooks.Creatures.GetValueOrDefault(held.Species, Impact.Flesh);
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], middle.X, middle.Y, held.Away, -1, 1, floor: feet);
        var glims = Stages.Of(Layers.Effects).Glims;
        for (int i = 0; i < 7; i++)
        {
            double a = glims.Between(0, Math.Tau), s = glims.Between(4, 14);
            glims.Add(Puff, middle.X + Math.Cos(a) * 6, middle.Y + Math.Sin(a) * 5, Math.Cos(a) * s, Math.Sin(a) * s - 4, size: glims.Between(3, 5));
        }
    }


    /// <summary>The chest of the hero nearest <paramref name="at"/>, as the crew drew them this frame; a cell over it with none.</summary>
    private Vector2 NearestHero(Vector2 at)
    {
        var nearest = at + new Vector2(0, -Cell * 80);
        float best = float.MaxValue;
        foreach (var (x, y) in ui.Heroes)
        {
            var hero = new Vector2(x, y);
            float d = hero.DistanceSquaredTo(at);
            if (d >= best) continue;
            (best, nearest) = (d, hero);
        }
        return nearest;
    }
}
