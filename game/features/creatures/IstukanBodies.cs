using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Grid;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The creatures drawn as the Istukan (the warden, its page's candidate 4): each its sprite drawn from its pose when that changes
/// (<see cref="IstukanArt"/>, <see cref="IstukanMotion"/>), lit as the world, its lamp and seams over it unlit; its lamp lighting the
/// hall in its colour, a thin arc of light behind its flight, dust where it comes to rest; struck a white flash and fine sparks. Gone
/// with its relic taken or its post felled it falls as blocks, lies, and crumbles (<see cref="IstukanFate"/>); gone with both standing
/// it calmed, with no hero in the arena, and simply is no longer there. Heard on the World bus (<see cref="S.Istukans"/>).
/// </summary>
public partial class IstukanBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    /// <summary>The height over its feet it turns about, in pixels: its middle.</summary>
    private const float Middle = 20;

    /// <summary>How fast the fallen drop to the floor under them, in pixels a second a second.</summary>
    private const float Drop = 700;

    private sealed partial class Shown : PosedSprite
    {
        public readonly IstukanArt Art = new();
        private readonly IstukanRig _rig = new();
        private readonly InkRig? _ink;

        public Shown() : base(IstukanArt.Width, IstukanArt.Height, IstukanArt.OriginX, IstukanArt.OriginY, Middle)
        {
            if (!HiRes.On) return;
            _ink = new InkRig(CreatureArt.InkIstukan.Id, CreatureArt.InkIstukan.Parts) { Position = new Vector2(0, Middle) };
            AddChild(_ink);
        }

        public IstukanMotion Motion = null!;
        public IstukanArt.Anchors Anchors;
        public IstukanArt.Pose? Painted;
        public S.IstukanCues Cues = null!;
        public Vector2 From, To, Drawn;
        public int Tick = -1, Damage, Post, Relic;
        public double OwnAt = double.NegativeInfinity, ShotAt = double.NegativeInfinity;
        public float Floor, DropSpeed;
        public string Species = "";
        /// <summary>The walls its post holds on to: any of them dug out fells it.</summary>
        public readonly List<int> Holds = [];

        /// <summary>Its sprite in its motion's pose, painted only if that changed, at its place, turned and facing as it flies.</summary>
        public void Show(Vector2 feet)
        {
            if (_ink != null)
            {
                Anchors = _rig.Place(Motion.Pose, Motion.Clock);
                _ink.Show(_rig.Slots, Motion.Flash, Motion.Pose.Dim);
            }
            else if (Painted != Motion.Pose)
            {
                Painted = Motion.Pose;
                Anchors = Art.Draw(Motion.Pose);
                Paint(Art.Albedo, Art.Silhouette, Art.Emission);
            }
            Place(feet, Motion.Shake, Motion.Angle, Motion.Face, Motion.Flash);
        }

        /// <summary>Where a point of its sprite (<see cref="IstukanArt.Anchors"/>) is in the world this frame.</summary>
        public Vector2 World((double X, double Y) at) => Transform * new Vector2((float)at.X, (float)at.Y + Middle);

        /// <summary>Its pixels in the world as they stand now, for a burst.</summary>
        public void PaintInto(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into)
        {
            if (_ink != null) _ink.Pixels(into, Transform);
            else PaintInto(into, Art.Albedo);
        }
    }

    private readonly Dictionary<int, Shown> _shown = new();
    private readonly Stack<Shown> _pool = new();
    private readonly List<Shown> _falling = [];
    private readonly HashSet<int> _seen = [], _alive = [];
    private readonly List<int> _gone = [];
    private readonly Voices _world = new(S.World, voices: 4);
    private readonly Random _random = new();
    private readonly Dictionary<(int X, int Y), (byte R, byte G, byte B)> _pixels = [];
    private double _clockMs;
    private IReadOnlyList<(float X, float Y)> _heroes = [];

    /// <summary>What a fallen one crumbles into, shared with the other creatures.</summary>
    public PixelBursts? Bursts { get; set; }

    public override void _Ready() => AddChild(_world);

    /// <summary>
    /// A frame of the creatures in <paramref name="posted"/> (those drawn as the Istukan), tick <paramref name="tick"/> drawn
    /// <paramref name="progress"/> of the way to the next, watching the heroes drawn at <paramref name="heroes"/> (their chests);
    /// what their lamps light goes into <paramref name="lights"/>; <paramref name="shotByOwn"/> those this player's own shots hit.
    /// </summary>
    public void Frame(CreaturesModule creatures, IBuildingQueries built, IGridQueries grid, IReadOnlyList<Creature> posted, int tick, double progress,
        double elapsedMs, IReadOnlyList<(float X, float Y)> heroes, List<LightSource> lights, IReadOnlySet<int> shotByOwn)
    {
        _clockMs += elapsedMs;
        foreach (int id in shotByOwn)
            if (_shown.TryGetValue(id, out var shot)) shot.ShotAt = _clockMs;
        _heroes = heroes;
        double dt = Math.Max(0, elapsedMs / 1000);
        _seen.Clear();
        foreach (var c in posted)
        {
            _seen.Add(c.Id);
            if (!_shown.TryGetValue(c.Id, out var one)) _shown[c.Id] = one = Take(c, built, grid, tick);
            Follow(one, c, tick);
            var feet = one.From.Lerp(one.To, (float)Math.Clamp(progress, 0, 1)) / Unit * Cell;
            if (c.Damage > one.Damage)
            {
                // A friend's strike, or one of this player's own already shown as it was sent.
                if (_clockMs - one.OwnAt > CreatureLooks.OwnMs) Struck(one, Spec.Weapons.Axe.Item, own: _clockMs - one.ShotAt < CreatureLooks.OwnMs);
                one.Damage = c.Damage;
            }
            var moments = one.Motion.Step(SeenOf(one, c, progress), dt);
            var was = one.Drawn;
            one.Drawn = feet;
            one.Show(feet);
            Moments(one, moments);
            if (c.Act is { Phase: ActPhase.Active }) Streak(one, was, feet);
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
            else Gone(one, built, grid);
        }
        Falling(dt, lights);
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
        foreach (var one in _falling) Put(one);
        _shown.Clear();
        _falling.Clear();
    }

    private Shown Take(Creature c, IBuildingQueries built, IGridQueries grid, int tick)
    {
        var one = _pool.Count > 0 ? _pool.Pop() : Made();
        var at = new Vector2(c.X, c.Y);
        var ward = Spec.Creatures.Get(c.Species).Brain[c.Drive] as Spec.Creatures.Ward;
        one.Motion = new IstukanMotion(_random.NextDouble, (ward?.Wake ?? 0) / (double)World.TicksPerSecond);
        (one.From, one.To, one.Tick, one.Damage, one.Species, one.OwnAt, one.ShotAt) = (at, at, tick, c.Damage, c.Species, double.NegativeInfinity, double.NegativeInfinity);
        one.Drawn = at / Unit * Cell;
        one.Post = CreaturesModule.PostOf(c) ?? -1;
        one.Holds.Clear();
        if (built.PostAt(one.Post) is { } post) one.Holds.AddRange(built.HoldsOf(post));
        one.Relic = CreaturesModule.RelicOf(c) ?? -1;
        one.Cues = S.Istukans[c.Species];
        // Into its hall till it has a hero to face.
        one.Motion.Face = one.Relic % grid.Width * Unit >= c.X ? 1 : -1;
        (one.Painted, one.DropSpeed, one.Modulate, one.Visible) = (null, 0, Colors.White, true);
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

    /// <summary>What the sim says of <paramref name="c"/> as its motion reads it, <paramref name="progress"/> of the way to the next tick.</summary>
    private IstukanMotion.Seen SeenOf(Shown one, Creature c, double progress)
    {
        var spec = Spec.Creatures.Get(c.Species);
        var act = c.Act;
        bool winding = act is { Phase: ActPhase.WindUp }, pouncing = act is { Phase: ActPhase.Active };
        double windUpS = winding && spec.Moves[act!.Move] is Spec.Creatures.Pounce pounce ? (pounce.WindUp - act.Left + Math.Clamp(progress, 0, 1)) / World.TicksPerSecond : 0;
        double perTick = Cell / (double)Unit * World.TicksPerSecond;
        var middle = one.Drawn + new Vector2(0, -Middle);
        (double, double)? hero = CreatureFx.NearestOf(_heroes, middle) is { } h ? (h.X - middle.X, h.Y - middle.Y) : null;
        return new IstukanMotion.Seen(CreaturesModule.Asleep(c), winding, windUpS, pouncing, CreaturesModule.Stuck(c), c.VX * perTick, c.VY * perTick, one.From == one.To && c.VX == 0 && c.VY == 0, hero);
    }

    /// <summary>A blow on <paramref name="one"/>: it flashes and shakes, fine sparks thrown toward the hero who struck, heard where it lands.</summary>
    private void Struck(Shown one, string tool, bool own)
    {
        one.Motion.Struck();
        var middle = one.Drawn + new Vector2(0, -Middle);
        var hero = CreatureFx.NearestOf(_heroes, middle) ?? middle + new Vector2(one.Motion.Face * 20, -10);
        var to = hero - middle;
        float d = Math.Max(to.Length(), 1);
        var at = middle + new Vector2(to.X / d * 9, to.Y / d * 12);
        CreatureFx.Sparks(Stages.Of(Layers.Effects), at, Math.Atan2(to.Y, to.X), 1);
        _world.Play(own ? S.Felt(one.Cues.Hit) : one.Cues.Hit, at, 1, GroundSounds.HitDbOf(tool));
    }

    /// <summary>Its moments seen and heard.</summary>
    private void Moments(Shown one, IstukanMotion.Moment m)
    {
        if (m == IstukanMotion.Moment.None) return;
        var eye = one.World(one.Anchors.Eye);
        var flares = Stages.Of(Layers.Creatures).Flares;
        if (m.HasFlag(IstukanMotion.Moment.Wake)) _world.Play(one.Cues.Wake, eye);
        if (m.HasFlag(IstukanMotion.Moment.Alert))
        {
            flares.Flash(eye.X, eye.Y, IstukanMotion.Yellow.Bytes, 0.6 * Fx.Glow, 8, CreatureLooks.LightKeep, 0.8);
            flares.Glint(eye.X, eye.Y, 4, (255, 250, 220));
            _world.Play(one.Cues.Alert, eye);
        }
        if (m.HasFlag(IstukanMotion.Moment.WindUp)) _world.Play(one.Cues.WindUp, eye);
        if (m.HasFlag(IstukanMotion.Moment.Pounce)) _world.Play(one.Cues.Pounce, eye);
        if (m.HasFlag(IstukanMotion.Moment.Land))
        {
            Dust(one.Drawn);
            _world.Play(one.Cues.Land, one.Drawn);
        }
    }

    /// <summary>Where it comes to rest: pale streaks along the floor, dust and a veil.</summary>
    private static void Dust(Vector2 feet)
    {
        var glims = Stages.Of(Layers.Creatures).Glims;
        for (int i = 0; i < 9; i++)
        {
            float x = feet.X + (float)glims.Between(-10, 10);
            glims.Add(Luminous.LandStreak, x, feet.Y - 0.5, (x < feet.X ? -1 : 1) * glims.Between(50, 120), -glims.Between(0, 15));
            glims.Add(Luminous.HitDust, x, feet.Y - 1, glims.Between(-16, 16), glims.Between(-18, -2));
        }
        for (int i = 0; i < 3; i++) glims.Add(Luminous.DustVeil, feet.X + glims.Between(-8, 8), feet.Y - 1, glims.Between(-10, 10), glims.Between(-6, -1));
    }

    /// <summary>The line of its throw: a thin streak of the makers' light behind it.</summary>
    private static void Streak(Shown one, Vector2 was, Vector2 now)
    {
        var (from, to) = (was + new Vector2(0, -Middle), now + new Vector2(0, -Middle));
        Stages.Of(Layers.Creatures).Flares.Trail(from.X, from.Y, to.X, to.Y, IstukanArt.Arc.Bytes, 0.8, 0.5, 0.3);
    }

    /// <summary>What glows of it this frame: its lamp's glow and light, the core in its chest.</summary>
    private static void Glows(Shown one, List<LightSource> lights)
    {
        var pose = one.Motion.Pose;
        var flares = Stages.Of(Layers.Creatures).Flares;
        var core = one.World(one.Anchors.Core);
        if (pose.Seam > 0.12) flares.Bloom(core.X, core.Y, 8, IstukanArt.Violet.Bytes, 0.2 * pose.Seam);
        if (pose.Lamp is not { } lamp || pose.LampAmp <= 0) return;
        var eye = one.World(one.Anchors.Eye);
        double amp = Math.Clamp(pose.LampAmp, 0, 1.7);
        var whiter = TickArt.Rgb.Mix(lamp, TickArt.White, 0.3);
        flares.Bloom(eye.X, eye.Y, 2 * (4 + 2.5 * amp + (one.Motion.Hot ? 2 : 0)), lamp.Bytes, 0.45 * amp);
        flares.Bloom(eye.X, eye.Y, 3.2, whiter.Bytes, 0.7 * amp);
        lights.Add(new LightSource(eye.X, eye.Y, ((float)lamp.R / 255, (float)lamp.G / 255, (float)lamp.B / 255),
            CreatureLooks.TickEyeLight * (float)amp * (one.Motion.Hot ? CreatureLooks.TickActingLight : 1), CreatureLooks.LightReach, CreatureLooks.LightKeep, 1));
    }

    /// <summary>
    /// <paramref name="one"/> gone from the world where it was drawn: by how, killed or shut down it falls where it was and is heard so;
    /// calmed it is put away, no hero in its arena to see it.
    /// </summary>
    private void Gone(Shown one, IBuildingQueries built, IGridQueries grid)
    {
        bool taken = false;
        foreach (var relic in built.Relics)
            if (relic.Cell == one.Relic) taken = relic.Taken;
        var fate = IstukanFate.Of(taken, built.PostAt(one.Post) != null, one.Holds.TrueForAll(cell => grid.WallAt(cell) != null));
        if (fate == IstukanFate.Fate.Calmed)
        {
            Put(one);
            return;
        }
        var middle = one.Drawn + new Vector2(0, -Middle);
        if (fate == IstukanFate.Fate.Killed)
        {
            CreatureFx.Sparks(Stages.Of(Layers.Effects), middle, -Math.PI / 2, 1.4);
            Stages.Of(Layers.Effects).Flares.Flash(middle.X, middle.Y, (255, 214, 150), 1.2 * Fx.Glow, 12, CreatureLooks.LightKeep, 0.86);
        }
        bool own = _clockMs - one.OwnAt < CreatureLooks.OwnMs * 2 || _clockMs - one.ShotAt < CreatureLooks.OwnMs;
        var cue = fate == IstukanFate.Fate.Killed ? one.Cues.Killed : one.Cues.ShutDown;
        _world.Play(own ? S.Felt(cue) : cue, middle);
        one.Motion.Fall();
        one.Floor = FloorUnder(one.Drawn, Spec.Creatures.Get(one.Species).Body.Width * Cell / 2, grid);
        _falling.Add(one);
    }

    /// <summary>The top of the first row at or under <paramref name="feet"/> solid under any column of a body <paramref name="half"/> pixels either way: what one killed in flight drops to, as a walker would.</summary>
    private static float FloorUnder(Vector2 feet, float half, IGridQueries grid)
    {
        int left = Math.Max(0, (int)Math.Floor((feet.X - half + 0.5f) / Cell)), right = Math.Min(grid.Width - 1, (int)Math.Floor((feet.X + half - 0.5f) / Cell));
        for (int row = (int)Math.Floor((feet.Y - 1) / Cell); row < grid.Height; row++)
            for (int column = left; column <= right; column++)
                if (grid.Solid(row * grid.Width + column)) return row * Cell;
        return grid.Height * Cell;
    }

    /// <summary>The fallen dropping to their floor, stuttering out, lying as blocks; each crumbling as its time is up, back to the pool.</summary>
    private void Falling(double dt, List<LightSource> lights)
    {
        for (int i = _falling.Count - 1; i >= 0; i--)
        {
            var one = _falling[i];
            bool landed = one.Drawn.Y >= one.Floor;
            if (!landed)
            {
                one.DropSpeed += Drop * (float)dt;
                one.Drawn = new Vector2(one.Drawn.X, Math.Min(one.Floor, one.Drawn.Y + one.DropSpeed * (float)dt));
                if (one.Drawn.Y >= one.Floor) Dust(one.Drawn);
            }
            one.Motion.Step(new IstukanMotion.Seen(false, false, 0, false, false, 0, 0, landed, null), dt);
            one.Show(one.Drawn);
            Glows(one, lights);
            if (one.Motion.Falling < IstukanMotion.FallS + IstukanMotion.LieS) continue;
            Crumble(one);
            _falling.RemoveAt(i);
            Put(one);
        }
    }

    private void Crumble(Shown one)
    {
        if (Bursts == null) return;
        _pixels.Clear();
        one.PaintInto(_pixels);
        Bursts.Crumble(_pixels, one.Drawn, 0);
    }
}
