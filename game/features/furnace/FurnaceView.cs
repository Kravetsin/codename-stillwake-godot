using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Furnace;

/// <summary>
/// A furnace or a burner generator at work without a word (features/furnace): fire in its mouth, embers
/// streaking up from it on curling paths and ash drifting, the air above its mouth wobbling with heat, and a
/// generator's flywheel spinning; none of it while it is out, for want of fuel, air or room. Its panel says
/// why, on demand. The fire is drawn unlit, for it gives light: as bright as <see cref="BlazeOf"/> says,
/// which breathes toward each picture's brightness (<see cref="Flames"/>); a cracking coal throws sparks
/// with a glint (0075).
/// </summary>
public partial class FurnaceView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The fire's pictures, <c>art/fire_*.png</c>, shown in turn.</summary>
    private const int Frames = 3;
    /// <summary>
    /// Where the fire goes in a machine's picture, in pixels from its top left: the inside of its mouth
    /// (FIRE_AT for the furnace), as wide and high as a picture of the fire; null for a machine with none.
    /// </summary>
    public static Vector2? FireAt(string kind) => kind switch
    {
        FurnaceModule.Smelter => new Vector2(14, 17),
        FurnaceModule.Generator => new Vector2(4, 16),
        _ => null,
    };
    /// <summary>Where embers rise from, from the fire's top left: the top of the fire, give or take <see cref="EmbersSpread"/> either way.</summary>
    private static readonly Vector2 EmbersFrom = new(5, 2);
    private const double EmbersSpread = 3.5;
    /// <summary>Embers a fire at work gives off a second, and ash motes.</summary>
    private const double EmbersPerSecond = 9, AshPerSecond = 7.2;
    /// <summary>Sparks a cracking coal throws, at least and at most; the glint it gives, from the fire's top left.</summary>
    private const int CrackLeast = 4, CrackMost = 7;
    private static readonly Vector2 CrackAt = new(5, -0.5f);
    private static readonly (int, int, int) CrackGlint = (255, 190, 110);
    /// <summary>
    /// The air that shimmers over a fire, from its top left, in pixels: from above the machine down to its
    /// mouth; and how far its rows wobble at the fire's full light.
    /// </summary>
    private static readonly Rect2 ShimmerBox = new(-8, -39, 26, 42);
    private const float ShimmerAmplitude = 0.55f;
    /// <summary>A faint warm haze in the hot air over the mouth, from the fire's top left, pixels across, and opacity at full light.</summary>
    private static readonly Vector2 HazeAt = new(5, -9);
    private const float HazeSize = 26, HazeAlpha = 0.08f;
    /// <summary>
    /// A burner generator's flywheel in its picture (<c>art/burner_wheel_*.png</c>, its pictures turned a
    /// third of a turn between them), in pixels from its top left, and how long each picture shows: a
    /// turn and a half a second.
    /// </summary>
    private static readonly Vector2 WheelAt = new(17, 0);
    private const int WheelFrames = 4;
    private const double WheelStepMs = 55;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    private static readonly CanvasItemMaterial Added = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>
    /// The fire in a furnace or generator at work: its picture, how brightly the picture burns and how brightly
    /// the fire glows, easing toward it; the embers and ash owed, carried over between frames; its shimmer and
    /// haze; a generator's flywheel.
    /// </summary>
    private sealed class Fire(Sprite2D sprite, Shimmer shimmer, Sprite2D haze)
    {
        public Sprite2D Sprite { get; } = sprite;
        public Shimmer Shimmer { get; } = shimmer;
        public Sprite2D Haze { get; } = haze;
        public Sprite2D? Wheel;
        public int Frame;
        public float Blaze = 1, Glow = 1;
        public double Embers, Ash;
    }

    private readonly IGridQueries _grid;
    private readonly FurnaceModule _furnace;
    private readonly Dictionary<int, Fire> _fires = new();
    private readonly HashSet<int> _burning = new();
    private readonly List<int> _out = new();
    private readonly Random _random = new();
    private double _flicker, _spin;
    private int _turn;

    public FurnaceView(IGridQueries grid, FurnaceModule furnace)
    {
        _grid = grid;
        _furnace = furnace;
    }

    /// <summary>The fires shown: what a recording checks.</summary>
    public int Fires => _fires.Count;

    /// <summary>How brightly furnace <paramref name="id"/>'s fire glows now, as a share of its full light: 0 while it is out.</summary>
    public float BlazeOf(int id) => _fires.TryGetValue(id, out var fire) ? fire.Glow : 0;

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    public override void _Process(double delta)
    {
        double elapsedMs = delta * 1000;
        _flicker += elapsedMs;
        bool turn = _flicker >= Flames.StepMs;
        if (turn) _flicker %= Flames.StepMs;
        _spin += elapsedMs;
        if (_spin >= WheelStepMs)
        {
            _turn = (_turn + (int)(_spin / WheelStepMs)) % WheelFrames;
            _spin %= WheelStepMs;
        }
        _burning.Clear();
        foreach (var m in _grid.Machines)
        {
            if (FireAt(m.Kind) is not { } fireAt || !_furnace.At(m.Id).Burning) continue;
            _burning.Add(m.Id);
            if (!_fires.TryGetValue(m.Id, out var fire)) _fires[m.Id] = fire = Light(m);
            if (turn)
            {
                // Never the same picture twice running, so the fire never stands still.
                fire.Frame += 1 + _random.Next(Frames - 1);
                fire.Sprite.Texture = Picture(fire.Frame);
                fire.Blaze = Flames.Blaze(_random.NextDouble());
            }
            fire.Glow = Flames.Breathe(fire.Glow, fire.Blaze, elapsedMs);
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;
            var at = corner + fireAt;
            fire.Sprite.Position = at;
            fire.Shimmer.Box = new Rect2(at + ShimmerBox.Position, ShimmerBox.Size);
            fire.Shimmer.Amplitude = ShimmerAmplitude * (0.7f + 0.3f * fire.Glow);
            fire.Haze.Position = at + HazeAt;
            fire.Haze.Modulate = new Color(1, 110 / 255f, 40 / 255f, HazeAlpha * fire.Glow * (float)Fx.Glow);
            if (fire.Wheel != null)
            {
                fire.Wheel.Position = corner + WheelAt;
                fire.Wheel.Texture = PixelArt.Picture($"burner_wheel_{(_turn + m.Id) % WheelFrames}");
            }
            // The machine's floor: what a cracking coal's sparks bounce off.
            float floor = corner.Y + Content.Machines.Get(m.Kind).Height * Cell;
            Throw(fire, at, floor, elapsedMs, turn && Flames.Cracks(fire.Blaze));
        }
        _out.Clear();
        foreach (var id in _fires.Keys)
            if (!_burning.Contains(id)) _out.Add(id);
        foreach (var id in _out)
        {
            var fire = _fires[id];
            fire.Sprite.QueueFree();
            fire.Shimmer.QueueFree();
            fire.Haze.QueueFree();
            fire.Wheel?.QueueFree();
            _fires.Remove(id);
        }
    }

    /// <summary>A fire lit in <paramref name="m"/>: its picture, its shimmer behind it, its haze, its crackle, a generator's flywheel.</summary>
    private Fire Light(Machine m)
    {
        var sprite = new Sprite2D { Centered = false, ZIndex = Layers.Fire, Material = Unlit, Texture = Picture(m.Id) };
        // Under the fire, so it wobbles the wall and the machine but not the flames or the embers.
        var shimmer = new Shimmer();
        var haze = new Sprite2D
        {
            Texture = PixelArt.Glow,
            ZIndex = Layers.Fire,
            Material = Added,
            Scale = Vector2.One * HazeSize / PixelArt.Glow.GetWidth(),
            TextureFilter = TextureFilterEnum.Linear,
        };
        AddChild(shimmer);
        AddChild(sprite);
        AddChild(haze);
        var fire = new Fire(sprite, shimmer, haze) { Frame = m.Id };
        if (m.Kind == FurnaceModule.Generator)
        {
            // Over the machine's picture, lit as it is: the wheel at rest is drawn in the picture.
            fire.Wheel = new Sprite2D { Centered = false, ZIndex = Layers.Sockets };
            Lit.Set(fire.Wheel);
            AddChild(fire.Wheel);
        }
        // Its crackle goes with it, begun somewhere along the loop so two fires never crackle in step.
        var crackle = Cues.Player2D(S.Fire, S.World);
        sprite.AddChild(crackle);
        crackle.Play((float)(_random.NextDouble() * 19));
        return fire;
    }

    /// <summary>The embers and ash a fire at <paramref name="at"/> owes this frame, and a cracking coal's sparks and glint.</summary>
    private void Throw(Fire fire, Vector2 at, float floor, double elapsedMs, bool cracks)
    {
        var glims = Glims.Shared;
        var from = at + EmbersFrom;
        fire.Embers += EmbersPerSecond * Fx.Count * elapsedMs / 1000;
        for (; fire.Embers >= 1; fire.Embers--)
            glims.Add(Luminous.Ember, from.X + Between(-EmbersSpread, EmbersSpread), from.Y - Between(0, 2), Between(-6, 6), -Between(14, 32));
        fire.Ash += AshPerSecond * Fx.Count * elapsedMs / 1000;
        for (; fire.Ash >= 1; fire.Ash--)
            glims.Add(Luminous.Ash, from.X + Between(-4, 4), from.Y - 3, Between(-3, 3), -Between(4, 9));
        if (!cracks) return;
        int sparks = (int)Math.Round(Between(CrackLeast, CrackMost) * Fx.Count);
        for (int i = 0; i < sparks; i++)
            glims.Add(Luminous.CrackSpark, from.X + Between(-2, 2), from.Y - 2, Between(-28, 28), -Between(55, 95), floor);
        Flares.Shared.Glint(at.X + CrackAt.X, at.Y + CrackAt.Y, 6, CrackGlint);
    }

    private static Texture2D Picture(int frame) => PixelArt.Picture($"fire_{frame % Frames}");

    /// <summary>
    /// Where a machine's fire gives its light, in pixels from its top left; null for a machine with none.
    /// The furnace's from the middle of its bottom row, where the TypeScript game's gave it; a generator's
    /// from low in its mouth.
    /// </summary>
    public static Vector2? LightAt(string kind) => kind switch
    {
        FurnaceModule.Smelter => new Vector2(Cell, Cell * 1.5f),
        FurnaceModule.Generator => FireAt(kind) + new Vector2(5, 7),
        _ => null,
    };
}
