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
/// A furnace or a burner generator at work without a word (features/furnace): fire in its mouth and
/// embers rising from it while it burns, and a generator's flywheel spinning; none of it while it is
/// out, for want of fuel, air or room. Its panel says why, on demand. The fire is drawn unlit, for it
/// gives light: as bright as <see cref="BlazeOf"/> says, which jumps with its pictures (<see cref="Flames"/>).
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
    private const float EmbersSpread = 3;
    /// <summary>Embers a furnace at work gives off a second.</summary>
    private const double EmbersPerSecond = 7;
    /// <summary>
    /// A burner generator's flywheel in its picture (<c>art/burner_wheel_*.png</c>, its pictures turned a
    /// third of a turn between them), in pixels from its top left, and how long each picture shows: a
    /// turn and a half a second.
    /// </summary>
    private static readonly Vector2 WheelAt = new(17, 0);
    private const int WheelFrames = 4;
    private const double WheelStepMs = 55;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly FurnaceModule _furnace;
    /// <summary>
    /// The fire in each furnace or generator at work, its picture, how brightly it burns in it, and the
    /// embers owed to it, carried over between frames, by machine id; a generator's flywheel with it.
    /// </summary>
    private readonly Dictionary<int, (Sprite2D Sprite, int Frame, float Blaze, double Owed, Sprite2D? Wheel)> _fires = new();
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

    /// <summary>How brightly furnace <paramref name="id"/>'s fire burns now, as a share of its full light: 0 while it is out.</summary>
    public float BlazeOf(int id) => _fires.TryGetValue(id, out var fire) ? fire.Blaze : 0;

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
            if (!_fires.TryGetValue(m.Id, out var fire))
            {
                fire = (new Sprite2D { Centered = false, ZIndex = Layers.Fire, Material = Unlit, Texture = Picture(m.Id) }, m.Id, 1, 0, null);
                AddChild(fire.Sprite);
                if (m.Kind == FurnaceModule.Generator)
                {
                    // Over the machine's picture, lit as it is: the wheel at rest is drawn in the picture.
                    fire.Wheel = new Sprite2D { Centered = false, ZIndex = Layers.Sockets };
                    Lit.Set(fire.Wheel);
                    AddChild(fire.Wheel);
                }
                // Its crackle goes with it, begun somewhere along the loop so two fires never crackle in step.
                var crackle = Cues.Player2D(S.Fire, S.World);
                fire.Sprite.AddChild(crackle);
                crackle.Play((float)(_random.NextDouble() * 19));
            }
            if (turn)
            {
                // Never the same picture twice running, so the fire never stands still.
                fire.Frame += 1 + _random.Next(Frames - 1);
                fire.Sprite.Texture = Picture(fire.Frame);
                fire.Blaze = Flames.Blaze(_random.NextDouble());
            }
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;
            fire.Sprite.Position = corner + fireAt;
            if (fire.Wheel != null)
            {
                fire.Wheel.Position = corner + WheelAt;
                fire.Wheel.Texture = PixelArt.Picture($"burner_wheel_{(_turn + m.Id) % WheelFrames}");
            }
            double due = fire.Owed + EmbersPerSecond * elapsedMs / 1000;
            int count = (int)Math.Floor(due);
            fire.Owed = due - count;
            if (turn && Flames.Cracks(fire.Blaze)) count += Flames.CrackEmbers;
            var from = corner + fireAt + EmbersFrom;
            for (int i = 0; i < count; i++) Particles.Shared.Emit(Effect.Ember, from.X + (_random.NextDouble() * 2 - 1) * EmbersSpread, from.Y, 1);
            _fires[m.Id] = fire;
        }
        _out.Clear();
        foreach (var id in _fires.Keys)
            if (!_burning.Contains(id)) _out.Add(id);
        foreach (var id in _out)
        {
            _fires[id].Sprite.QueueFree();
            _fires[id].Wheel?.QueueFree();
            _fires.Remove(id);
        }
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
