using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Fauna;
using Stillwake.Sim.Grid;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Fauna;

/// <summary>
/// The creatures (features/fauna, 0102): each drawn between ticks where the sim put it, facing the way it goes, its body moved
/// by <see cref="CreatureMotion"/>. A strike of this player's own reacts the frame it is sent — white flash, squash, flinch, a
/// wide eye, a shake, its hit thrown and heard by what it struck (0093, 0094) — and the knock follows as the sim moves
/// it; a friend's shows as the sim's damage rises. One killed in view bursts into a puff. A creature still watches the hero
/// nearest it. Only creatures within the camera's view and a margin are drawn, from a pool that never frees.
/// </summary>
public partial class FaunaView(Ui ui) : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = FaunaModule.Unit, Margin = 4;

    /// <summary>How long after this player's own strike on a creature the sim's damage rising is that strike's, already shown.</summary>
    private const double OwnMs = 700;

    /// <summary>How near a hero a creature watches it, in pixels.</summary>
    private const float Watch = 9 * Cell;

    /// <summary>How long a creature killed is drawn swelling white and fading, in milliseconds.</summary>
    private const double GhostMs = 170;

    /// <summary>How quickly a body eases down onto a slope under it, or back up off one, in milliseconds.</summary>
    private const double LowerMs = 70;

    /// <summary>The deepest a foot is drawn reaching down, in thousandths, and the most a body tilts to its feet, radians: over an edge it teeters, never tips.</summary>
    private const int Overhang = 600;
    private const float MostLean = 0.4f;

    /// <summary>A rise faster than this a tick is a jump, not a blow's hop (an axe's is 180).</summary>
    private const int JumpTell = 250;

    /// <summary>What is shown of a creature: its body, its motion, gait and pose, where it was on the tick before and on the last, its damage.</summary>
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
        public string Species { get; set; } = "";
        /// <summary>How far under the sim's place it is drawn, in pixels: down onto a slope's diagonal, eased.</summary>
        public float Lower { get; set; }
        /// <summary>How far it is drawn tilted to the ground under its feet, radians clockwise, eased.</summary>
        public float Lean { get; set; }
        /// <summary>Its speed down as last seen, and the fastest it fell since it left the ground: for its jumps and landings.</summary>
        public int LastVy { get; set; }
        public int Fall { get; set; }
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Stack<CreatureBody> _pool = new();
    private readonly HashSet<int> _out = [], _drawn = [];
    private readonly List<int> _gone = [];
    private readonly List<(CreatureBody Body, double LeftMs)> _ghosts = [];
    // By reference: two strikes of one creature are equal records, and one may run while the other flies.
    private HashSet<Command> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    private readonly Voices _ownHits = new(S.Suit, voices: 4), _worldHits = new(S.World, voices: 4);
    private double _clockMs;
    private FaunaModule? _of;

    /// <summary>This player's own axe bit a creature, knocking it <c>away</c>: the view nudges (<see cref="ShipCamera.Nudge"/>).</summary>
    public event Action<Vector2>? Nudged;

    /// <summary>The creatures drawn last frame, and the bodies ever made: for the frame log and scripted runs.</summary>
    public (int Drawn, int Made) Bodies => (_shown.Count, _shown.Count + _pool.Count + _ghosts.Count);

    private readonly List<(Vector2 At, Color Colour)> _lights = [];

    /// <summary>Where each glowing part of a creature drawn shines from this frame, in pixels of the world, and its colour: the light view's.</summary>
    public IReadOnlyList<(Vector2 At, Color Colour)> Lights => _lights;

    public override void _Ready()
    {
        ZIndex = Layers.Creatures;
        AddChild(_ownHits);
        AddChild(_worldHits);
    }

    /// <summary>
    /// Shows <paramref name="fauna"/>'s creatures within <paramref name="view"/> (cells) and <see cref="Margin"/> round it,
    /// <paramref name="progress"/> of the way from the last tick to the next, recoloured by <paramref name="palette"/> (the planet's,
    /// <see cref="FaunaLooks.PaletteOf"/>), each creature its own individual by its id and the surface's <paramref name="seed"/> (<see cref="CreatureVariety"/>).
    /// </summary>
    public void Frame(Client client, FaunaModule fauna, IGridQueries grid, Rect2 view, double progress, double elapsedMs, string palette, uint seed)
    {
        _clockMs += elapsedMs;
        // Another surface, or a load: what was shown was another world's, gone without a puff.
        if (fauna != _of)
        {
            foreach (var one in _shown.Values) Put(one.Body);
            _shown.Clear();
            _of = fauna;
        }
        (_palette, _seed) = (palette, seed);
        var all = fauna.Creatures;
        int left = (int)Math.Floor(view.Position.X - Margin) * Unit, right = (int)Math.Ceiling(view.End.X + Margin) * Unit;
        int top = (int)Math.Floor(view.Position.Y - Margin) * Unit, bottom = (int)Math.Ceiling(view.End.Y + Margin) * Unit;
        _out.Clear();
        _drawn.Clear();
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            _out.Add(c.Id);
            if (c.X >= left && c.X <= right && c.Y >= top && c.Y <= bottom) _drawn.Add(c.Id);
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
        Ghosts(elapsedMs);

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
                if (_clockMs - one.OwnAt > OwnMs) Struck(one, ToolOf(one.Drawn), own: false);
                one.Damage = c.Damage;
            }
            Place(one, c, grid, progress, elapsedMs);
        }
        // What their glowing parts light this frame: the fluff's spots in a cave, the tapir's pod at dusk.
        _lights.Clear();
        foreach (var one in _shown.Values) one.Body.Lights(_lights, one.Body.Transform, 1);
        foreach (var (body, leftMs) in _ghosts) body.Lights(_lights, body.Transform, (float)(leftMs / GhostMs));
        if (Trace != null) Traced(all);
    }

    /// <summary><c>--fauna-trace</c>: each creature drawn, once a second, its strides that second, its speed and what it did.</summary>
    public Action<string>? Trace { get; set; }

    private double _tracedAt;
    private readonly Dictionary<int, double> _tracedPhase = [];

    /// <summary><c>--fauna-trace=frames</c>: also every frame, each creature drawn, where on the screen and its strides, for recordings.</summary>
    public bool TraceFrames { get; set; }


    private void Traced(IReadOnlyList<Creature> all)
    {
        if (TraceFrames)
            foreach (var c in all)
                if (_shown.TryGetValue(c.Id, out var one))
                {
                    var at = GetViewport().GetCanvasTransform() * one.Body.Position;
                    Trace!($"fauna frame {Engine.GetProcessFrames()}: {c.Species} {c.Id} {c.Mode} at {at.X:F0},{at.Y:F0} stride {one.Gait.Phase:F3} gallop {one.Gait.Gallop:F2} air {one.Gait.Air:F2}");
                }
        if (_clockMs - _tracedAt < 1000) return;
        double seconds = (_clockMs - _tracedAt) / 1000;
        _tracedAt = _clockMs;
        foreach (var c in all)
        {
            if (!_shown.TryGetValue(c.Id, out var one)) continue;
            double was = _tracedPhase.GetValueOrDefault(c.Id, one.Gait.Phase);
            _tracedPhase[c.Id] = one.Gait.Phase;
            Trace!($"fauna trace: {c.Species} {c.Id} {c.Mode}: {(one.Gait.Phase - was) / seconds:F2} strides/s at {one.Gait.Speed:F0} px/s, gallop {one.Gait.Gallop:F2}");
        }
    }

    private string _palette = FaunaLooks.Palette;
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
            if (_flying.Contains(strike) || !_shown.TryGetValue(strike.Creature, out var one)) continue;
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
        var spec = Spec.Fauna.Get(one.Species);
        var middle = one.Drawn + new Vector2(0, -spec.Height * Cell / 2 * one.Body.Size);
        var hero = NearestHero(middle);
        int away = middle.X >= hero.X ? 1 : -1;
        double share = Impacts.ShareOf(tool);
        one.Motion.Hit(away, share);
        var impact = HitLooks.Creatures.GetValueOrDefault(one.Species, Impact.Flesh);
        var side = middle + new Vector2(-away * spec.Width * Cell * 0.4f * one.Body.Size, Mathf.Clamp(hero.Y - middle.Y, -6, 6));
        HitBurst.Shared.Throw(HitLooks.Of[impact], side.X, side.Y, -away, -0.25, share, floor: one.Drawn.Y);
        if (S.Creatures.TryGetValue(one.Species, out var cues))
        {
            var voices = own ? _ownHits : _worldHits;
            float db = Features.Sound.GroundSounds.HitDbOf(tool);
            voices.Play(cues.Hit, side, cues.Hit.Tune, db);
            voices.Play(cues.Thump, side, cues.Thump.Tune, db);
        }
        if (own && !HeldLook.IsDrill(tool)) Nudged?.Invoke(new Vector2(away, 0));
    }

    /// <summary>The tool a friend most likely struck with at <paramref name="at"/>: a running drill whose bit is by it, else the axe.</summary>
    private string ToolOf(Vector2 at)
    {
        foreach (var (x, y, tool) in ui.Bits)
            if (new Vector2(x, y).DistanceTo(at) < 3 * Cell) return tool;
        return Spec.Weapons.Axe.Item;
    }

    /// <summary><paramref name="one"/>'s tick positions kept, the one before and the last, for drawing between them.</summary>
    private static void Follow(Shown one, Creature c, int tick)
    {
        var at = new Vector2(c.X, c.Y);
        if (one.Tick < 0 || tick - one.Tick > 1) (one.From, one.To) = (at, at);
        else if (tick != one.Tick) (one.From, one.To) = (one.To, at);
        one.Tick = tick;
    }

    private void Place(Shown one, Creature c, IGridQueries grid, double progress, double elapsedMs)
    {
        var at = one.From.Lerp(one.To, (float)Math.Clamp(progress, 0, 1)) / Unit * Cell;
        var spec = Spec.Fauna.Get(c.Species);
        bool grounded = c.VY == 0;
        // Onto the ground under each foot, the diagonals of slopes and the step below an overhang: the body drawn between the
        // two and tilted to them, eased, so a step on or off a slope glides rather than pops.
        int span = spec.Width * Unit * 22 / 100;
        var (back, front) = grounded ? (GroundUnder(grid, c.X - span, c.Y), GroundUnder(grid, c.X + span, c.Y)) : (0, 0);
        float lower = (back + front) / 2f / Unit * Cell, lean = Math.Clamp((float)Math.Atan2(front - back, 2.0 * span) * 0.8f, -MostLean, MostLean);
        // Eased by time, not by frame: the same glide at 60 frames a second and at 144.
        float ease = (float)CreatureGait.Ease(elapsedMs, LowerMs);
        one.Lower += (lower - one.Lower) * ease;
        one.Lean += (lean - one.Lean) * ease;
        at.Y += one.Lower;
        var motion = one.Motion;
        // Never held where it was struck: a neutral creature bolts at the blow, and its drawing goes with it, only shaking.
        var drawn = at;
        var watched = NearestHero(drawn + new Vector2(0, -spec.Height * Cell / 2));
        var toHero = watched - (drawn + new Vector2(0, -spec.Height * Cell * 0.6f));
        bool near = toHero.Length() < Watch;
        int? watch = near && Math.Abs(toHero.X) > 2 ? Math.Sign(toHero.X) : null;
        // A jump read off the sim: a leap up faster than any blow's hop, from the ground; a landing as hard as the fall was fast.
        if (c.VY <= -JumpTell && one.LastVy >= 0) motion.Jump();
        if (!grounded) one.Fall = Math.Max(one.Fall, c.VY);
        else if (!one.Grounded)
        {
            motion.Land((double)one.Fall / Spec.Fauna.MaxFall);
            one.Fall = 0;
        }
        one.LastVy = c.VY;
        motion.Step(elapsedMs, drawn.X - one.Drawn.X, grounded && !one.Grounded, c.VX == 0 ? watch : null);
        // Its legs go by the ground the sim moved it on its last tick, never by a frame's step: a frame's is uneven, a tick's is not.
        double speed = Math.Abs(one.To.X - one.From.X) / Unit * Cell * World.TicksPerSecond;
        bool fleeing = c.Mode == Mode.Flee;
        var gait = one.Gait;
        gait.Step(elapsedMs, speed, grounded, fleeing);
        // Idle with no hero by, a while, it grazes; idle with one near, it watches it.
        bool idle = c.Mode == Mode.Idle && grounded;
        one.AloneMs = idle && !near ? one.AloneMs + elapsedMs : 0;
        one.Graze += ((one.AloneMs > FaunaLooks.GrazeAfterMs ? 1 : 0) - one.Graze) * CreatureGait.Ease(elapsedMs, FaunaLooks.GrazeMs);
        one.Notice += ((idle && near ? 1 : 0) - one.Notice) * CreatureGait.Ease(elapsedMs, FaunaLooks.BlendMs);
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
        body.Rotation = (float)motion.Tilt(gait.Flee) + one.Lean;
        body.Pose(pose, motion.Flash);
    }

    /// <summary>
    /// How far under a body standing at <paramref name="y"/> the ground lies at <paramref name="x"/>, in thousandths, at most a cell:
    /// the sim stands a creature on the top of the row under it, a floor slope's whole, with its body overhanging a step (0102); a
    /// foot is drawn on the diagonal there, or on the step a cell down.
    /// </summary>
    private static int GroundUnder(IGridQueries grid, int x, int y)
    {
        int column = x / Unit, row = y / Unit;
        if (x < 0 || column >= grid.Width) return 0;
        for (int r = row; r <= row + 1 && r < grid.Height; r++)
        {
            int cell = r * grid.Width + column;
            if (grid.StructureAt(cell) is not (Structure.Wall or Structure.Floor or Structure.DoorClosed)) continue;
            int shape = grid.ShapeAt(cell);
            return Math.Min(Overhang, (r - row) * Unit + (Shape.IsFloor(shape) ? Shape.TopAt(shape, x - column * Unit) : 0));
        }
        return Overhang;
    }

    /// <summary>A body for <paramref name="c"/> come into view, from the pool or new, unmoved and unstruck.</summary>
    private Shown Take(Creature c)
    {
        var body = _pool.Count > 0 ? _pool.Pop() : Made();
        var individual = CreatureVariety.Of(FaunaLooks.Varieties[c.Species], _seed, c.Id);
        body.Dress(c.Species, _palette, individual);
        Trace?.Invoke($"fauna look: {c.Species} {c.Id} {individual}");
        body.Visible = true;
        var at = new Vector2(c.X, c.Y);
        var design = body.Design!;
        var run = FaunaLooks.Runs[design.Id];
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
    /// <paramref name="one"/> killed where it was drawn: a burst of its hit at full, a soft puff of fur and dust, a white glint
    /// and shock ring, its body swelling white and gone; heard where it fell.
    /// </summary>
    private void Die(Shown one)
    {
        var spec = Spec.Fauna.Get(one.Species);
        var middle = one.Drawn + new Vector2(0, -spec.Height * Cell / 2 * one.Body.Size);
        var impact = HitLooks.Creatures.GetValueOrDefault(one.Species, Impact.Flesh);
        HitBurst.Shared.Throw(HitLooks.Of[impact], middle.X, middle.Y, 0, -1, 1, floor: one.Drawn.Y);
        var glims = Glims.Shared;
        for (int i = 0; i < 14; i++)
        {
            double a = glims.Between(0, Math.Tau), s = glims.Between(10, 46);
            glims.Add(HitLooks.TuftOf(impact), middle.X + Math.Cos(a) * 4, middle.Y + Math.Sin(a) * 4, Math.Cos(a) * s, Math.Sin(a) * s - 10, one.Drawn.Y);
        }
        for (int i = 0; i < 7; i++)
        {
            double a = glims.Between(0, Math.Tau), s = glims.Between(4, 14);
            glims.Add(Puff, middle.X + Math.Cos(a) * 6, middle.Y + Math.Sin(a) * 5, Math.Cos(a) * s, Math.Sin(a) * s - 4, size: glims.Between(3, 5));
        }
        Flares.Shared.Glint(middle.X, middle.Y, 12, (255, 255, 255));
        Flares.Shared.Shock(middle.X, middle.Y, 3, 30, 200, (255, 246, 236), 0.5 * Fx.Glow, 0.35);
        if (S.Creatures.TryGetValue(one.Species, out var cues))
            (_clockMs - one.OwnAt < OwnMs * 2 ? _ownHits : _worldHits).Play(cues.Death, middle, cues.Death.Tune, 0);
        one.Body.Dying = 0;
        _ghosts.Add((one.Body, GhostMs));
    }

    /// <summary>The soft puff a creature leaves: a warm wisp, swelling and gone.</summary>
    private static readonly Luminous Puff = Luminous.Vapour with { Ramp = [(240, 226, 206)], Alpha = 0.2, LifeMs = (500, 900) };

    /// <summary>Bodies of the killed swelling white and fading; each back to the pool once gone.</summary>
    private void Ghosts(double elapsedMs)
    {
        for (int i = _ghosts.Count - 1; i >= 0; i--)
        {
            var (body, left) = _ghosts[i];
            left -= elapsedMs;
            if (left <= 0)
            {
                body.Dying = null;
                _ghosts.RemoveAt(i);
                Put(body);
                continue;
            }
            _ghosts[i] = (body, left);
            double t = 1 - left / GhostMs;
            body.Dying = t;
            body.Scale = new Vector2(Math.Sign(body.Scale.X) * (float)(1 + 0.45 * t), (float)(1 + 0.25 * t)) * body.Size;
            body.Fade((float)(1 - t));
        }
    }

    /// <summary>The chest of the hero nearest <paramref name="at"/>, as the crew drew them this frame; a cell over it with none.</summary>
    private Vector2 NearestHero(Vector2 at)
    {
        var nearest = at + new Vector2(0, -Cell * 40);
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
