using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The cave lurkers (<see cref="CreatureLooks.Lurkers"/>, 2026-11-07-cave-hostiles): each a rig in ink posed as its motion says
/// (<see cref="LurkerMotion"/>, <see cref="LurkerRig"/>), lit as the world, its moss, caps, veins and eyes shining over it unlit and
/// lighting the cave; dust where a capback lands and behind a rolling snail; struck its hit thrown and heard as a walker's is. Killed it
/// sags or cracks a moment where it was, then crumbles.
/// </summary>
public partial class LurkerBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    private sealed partial class Shown : PosedSprite
    {
        private readonly InkRig _ink;
        private readonly float _middle;

        public Shown(CreatureArt.LurkerDesign design, float middle) : base(middle)
        {
            (Design, Rig, _middle) = (design, new LurkerRig(design), middle);
            _ink = new InkRig(design.Id, design.Parts) { Position = new Vector2(0, middle) };
            AddChild(_ink);
        }

        public readonly CreatureArt.LurkerDesign Design;
        public readonly LurkerRig Rig;
        public LurkerMotion Motion = null!;
        public Vector2 From, To, Drawn;
        public int Tick = -1, Damage;
        public double OwnAt = double.NegativeInfinity, ShotAt = double.NegativeInfinity, DustMs;
        public string Species = "";

        /// <summary>Its sprite in its motion's pose, at its place, facing as it does.</summary>
        public void Show(Vector2 feet)
        {
            Rig.Place(Motion.Pose);
            _ink.Show(Rig.Slots, Motion.Flash, 0);
            Place(feet, 0, 0, Motion.Face);
        }

        /// <summary>Where a point of its rig is in the world this frame.</summary>
        public Vector2 World(double x, double y) => Transform * new Vector2((float)x, (float)y + _middle);

        /// <summary>Its pixels in the world as they stand now, for a burst.</summary>
        public void PaintInto(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into) => _ink.Pixels(into, Transform);
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Dictionary<string, Stack<Shown>> _pool = new();
    private readonly List<Shown> _dying = [];
    private readonly HashSet<int> _seen = [], _alive = [];
    private readonly List<int> _gone = [];
    private readonly Voices _ownHits = new(S.Suit, voices: 4), _worldHits = new(S.World, voices: 4);
    private readonly Dictionary<(int X, int Y), (byte R, byte G, byte B)> _pixels = [];
    private double _clockMs;
    private IReadOnlyList<(float X, float Y)> _heroes = [];

    /// <summary>What a killed one crumbles into, shared with the other creatures.</summary>
    public PixelBursts? Bursts { get; set; }

    /// <summary>The soft puff a creature leaves, as a walker's.</summary>
    private static readonly Luminous Puff = Luminous.Vapour with { Ramp = [(240, 226, 206)], Alpha = 0.2, LifeMs = (500, 900) };

    public override void _Ready()
    {
        AddChild(_ownHits);
        AddChild(_worldHits);
    }

    /// <summary>
    /// A frame of the creatures in <paramref name="lurkers"/> (those drawn as lurkers), tick <paramref name="tick"/> drawn
    /// <paramref name="progress"/> of the way to the next, watching the heroes drawn at <paramref name="heroes"/>; what they light goes into
    /// <paramref name="lights"/>; <paramref name="shotByOwn"/> those this player's own shots hit.
    /// </summary>
    public void Frame(CreaturesModule creatures, IReadOnlyList<Creature> lurkers, int tick, double progress, double elapsedMs,
        IReadOnlyList<(float X, float Y)> heroes, List<LightSource> lights, IReadOnlySet<int> shotByOwn)
    {
        _clockMs += elapsedMs;
        foreach (int id in shotByOwn)
            if (_shown.TryGetValue(id, out var shot)) shot.ShotAt = _clockMs;
        _heroes = heroes;
        double dt = Math.Max(0, elapsedMs / 1000);
        _seen.Clear();
        foreach (var c in lurkers)
        {
            _seen.Add(c.Id);
            if (!_shown.TryGetValue(c.Id, out var one)) _shown[c.Id] = one = Take(c, tick);
            Follow(one, c, tick);
            var feet = one.From.Lerp(one.To, (float)Math.Clamp(progress, 0, 1)) / Unit * Cell;
            if (c.Damage > one.Damage)
            {
                // A friend's strike, or one of this player's own already shown as it was sent.
                if (_clockMs - one.OwnAt > CreatureLooks.OwnMs) Struck(one, own: _clockMs - one.ShotAt < CreatureLooks.OwnMs);
                one.Damage = c.Damage;
            }
            bool landed = one.Motion.Step(SeenOf(one, c, feet), dt);
            one.Drawn = feet;
            one.Show(feet);
            Moments(one, landed, c.Act is { Phase: ActPhase.Active }, elapsedMs);
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
            if (_alive.Contains(id)) Put(one);
            else Killed(one);
        }
        Dying(dt, lights);
    }

    /// <summary>This player's own strike on <paramref name="id"/>, seen in flight for the first time: it reacts now, a round trip before the sim.</summary>
    public bool OwnStrike(int id)
    {
        if (!_shown.TryGetValue(id, out var one)) return false;
        one.OwnAt = _clockMs;
        Struck(one, own: true);
        return true;
    }

    /// <summary>Another surface, or a load: what was shown was another world's, gone without a puff.</summary>
    public void Reset()
    {
        foreach (var one in _shown.Values) Put(one);
        foreach (var one in _dying) Put(one);
        _shown.Clear();
        _dying.Clear();
    }

    private Shown Take(Creature c, int tick)
    {
        var spec = Spec.Creatures.Get(c.Species);
        var design = CreatureArt.Lurkers[spec.Look];
        var one = _pool.TryGetValue(design.Id, out var pool) && pool.Count > 0 ? pool.Pop() : Made(design, spec.Body.Height * Cell / 2);
        var at = new Vector2(c.X, c.Y);
        one.Motion = new LurkerMotion(design.Snail, design.Pose.Radius);
        (one.From, one.To, one.Tick, one.Damage, one.Species, one.OwnAt, one.ShotAt) = (at, at, tick, c.Damage, c.Species, double.NegativeInfinity, double.NegativeInfinity);
        one.Drawn = at / Unit * Cell;
        if (CreatureFx.NearestOf(_heroes, one.Drawn) is { } hero && Math.Abs(hero.X - one.Drawn.X) > 2) one.Motion.Face = Math.Sign(hero.X - one.Drawn.X);
        (one.Modulate, one.Visible) = (Colors.White, true);
        return one;
    }

    private Shown Made(CreatureArt.LurkerDesign design, float middle)
    {
        var one = new Shown(design, middle);
        AddChild(one);
        return one;
    }

    private void Put(Shown one)
    {
        one.Visible = false;
        if (!_pool.TryGetValue(one.Design.Id, out var pool)) _pool[one.Design.Id] = pool = new Stack<Shown>();
        pool.Push(one);
    }

    private static void Follow(Shown one, Creature c, int tick)
    {
        var at = new Vector2(c.X, c.Y);
        if (one.Tick < 0 || tick - one.Tick > 1) (one.From, one.To) = (at, at);
        else if (tick != one.Tick) (one.From, one.To) = (one.To, at);
        one.Tick = tick;
    }

    /// <summary>What the sim says of <paramref name="c"/> as its motion reads it, drawn at <paramref name="feet"/> this frame.</summary>
    private LurkerMotion.Seen SeenOf(Shown one, Creature c, Vector2 feet)
    {
        var spec = Spec.Creatures.Get(c.Species);
        var act = c.Act;
        bool winding = act is { Phase: ActPhase.WindUp };
        double windUpS = winding ? (WindUpOf(spec.Moves[act!.Move]) - act.Left) / (double)World.TicksPerSecond : 0;
        double perTick = Cell / (double)Unit * World.TicksPerSecond;
        double? hero = CreatureFx.NearestOf(_heroes, feet) is { } h ? h.X - feet.X : null;
        return new LurkerMotion.Seen(CreaturesModule.Alerted(c), winding, windUpS, act is { Phase: ActPhase.Active }, feet.X - one.Drawn.X, c.VX * perTick,
            spec.Life.Hp > 0 ? c.Damage / (double)spec.Life.Hp : 0, hero);
    }

    private static int WindUpOf(Spec.Creatures.Move move) => move switch
    {
        Spec.Creatures.Pounce p => p.WindUp,
        Spec.Creatures.Lunge l => l.WindUp,
        _ => 0,
    };

    /// <summary>A blow on <paramref name="one"/>: it flinches or shuts, its hit thrown toward the hero who struck and heard where it lands.</summary>
    private void Struck(Shown one, bool own)
    {
        one.Motion.Struck();
        var spec = Spec.Creatures.Get(one.Species);
        var middle = one.Drawn + new Vector2(0, -spec.Body.Height * Cell / 2);
        var hero = CreatureFx.NearestOf(_heroes, middle) ?? middle + new Vector2(one.Motion.Face * 20, -10);
        int away = middle.X >= hero.X ? 1 : -1;
        var side = middle + new Vector2(-away * spec.Body.Width * Cell * 0.4f, Mathf.Clamp(hero.Y - middle.Y, -6, 6));
        var impact = HitLooks.Creatures.GetValueOrDefault(one.Species, Impact.Flesh);
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], side.X, side.Y, -away, -0.25, 1, floor: one.Drawn.Y);
        if (!S.Creatures.TryGetValue(one.Species, out var cues)) return;
        var voices = own ? _ownHits : _worldHits;
        voices.Play(own ? S.Felt(cues.Hit) : cues.Hit, side, cues.Hit.Tune, 0);
        voices.Play(own ? S.Felt(cues.Thump) : cues.Thump, side, cues.Thump.Tune, 0);
    }

    /// <summary>Its moments seen and heard: a capback's landing raises dust and thumps; a rolling snail trails dust.</summary>
    private void Moments(Shown one, bool landed, bool acting, double elapsedMs)
    {
        if (landed && !one.Design.Snail)
        {
            Dust(one.Drawn, 9);
            if (S.Creatures.TryGetValue(one.Species, out var cues)) _worldHits.Play(cues.Thump, one.Drawn, cues.Thump.Tune, -4);
        }
        if (acting && one.Design.Snail && (one.DustMs -= elapsedMs) <= 0)
        {
            one.DustMs = 70;
            Dust(one.Drawn + new Vector2(-one.Motion.Face * 6, 0), 1);
        }
    }

    /// <summary>Dust raised at <paramref name="feet"/>: pale streaks along the floor and a veil.</summary>
    private static void Dust(Vector2 feet, int streaks)
    {
        var glims = Stages.Of(Layers.Creatures).Glims;
        for (int i = 0; i < streaks; i++)
        {
            float x = feet.X + (float)glims.Between(-10, 10);
            glims.Add(Luminous.HitDust, x, feet.Y - 1, glims.Between(-16, 16), glims.Between(-18, -2));
        }
        glims.Add(Luminous.DustVeil, feet.X + glims.Between(-6, 6), feet.Y - 1, glims.Between(-10, 10), glims.Between(-6, -1));
    }

    /// <summary>What it lights this frame: its rig's lights, in the world.</summary>
    private static void Glows(Shown one, List<LightSource> lights)
    {
        foreach (var l in one.Rig.Lights)
        {
            if (l.K <= 0.01) continue;
            var at = one.World(l.X, l.Y);
            lights.Add(new LightSource(at.X, at.Y, ((float)l.Colour.R / 255, (float)l.Colour.G / 255, (float)l.Colour.B / 255),
                CreatureLooks.LurkerLight * (float)l.K, (int)Math.Ceiling(l.R / Cell) + 2, CreatureLooks.LightKeep, 1));
        }
    }

    /// <summary><paramref name="one"/> killed where it was drawn: heard dying, it sags or cracks a moment before it crumbles.</summary>
    private void Killed(Shown one)
    {
        var middle = one.Drawn + new Vector2(0, -Spec.Creatures.Get(one.Species).Body.Height * Cell / 2);
        bool own = _clockMs - one.OwnAt < CreatureLooks.OwnMs * 2 || _clockMs - one.ShotAt < CreatureLooks.OwnMs;
        if (S.Creatures.TryGetValue(one.Species, out var cues)) (own ? _ownHits : _worldHits).Play(own ? S.Felt(cues.Death) : cues.Death, middle, cues.Death.Tune, 0);
        one.Motion.Die();
        _dying.Add(one);
    }

    /// <summary>The killed dying where they were; each crumbling as its time is up, back to the pool.</summary>
    private void Dying(double dt, List<LightSource> lights)
    {
        for (int i = _dying.Count - 1; i >= 0; i--)
        {
            var one = _dying[i];
            one.Motion.Step(default, dt);
            one.Show(one.Drawn);
            Glows(one, lights);
            if (one.Motion.Dying < LurkerMotion.DieS) continue;
            Crumble(one);
            _dying.RemoveAt(i);
            Put(one);
        }
    }

    /// <summary>Its pixels left to crumble, its hit thrown whole and a soft puff, as a walker's.</summary>
    private void Crumble(Shown one)
    {
        var middle = one.Drawn + new Vector2(0, -Spec.Creatures.Get(one.Species).Body.Height * Cell / 2);
        int away = CreatureFx.NearestOf(_heroes, middle) is { } hero && hero.X > middle.X ? -1 : 1;
        if (Bursts != null)
        {
            _pixels.Clear();
            one.PaintInto(_pixels);
            Bursts.Crumble(_pixels, middle, away);
        }
        var impact = HitLooks.Creatures.GetValueOrDefault(one.Species, Impact.Flesh);
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], middle.X, middle.Y, away, -1, 1, floor: one.Drawn.Y);
        var glims = Stages.Of(Layers.Effects).Glims;
        for (int i = 0; i < 7; i++)
        {
            double a = glims.Between(0, Math.Tau), s = glims.Between(4, 14);
            glims.Add(Puff, middle.X + Math.Cos(a) * 6, middle.Y + Math.Sin(a) * 5, Math.Cos(a) * s, Math.Sin(a) * s - 4, size: glims.Between(3, 5));
        }
    }
}
