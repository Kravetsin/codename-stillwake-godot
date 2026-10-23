using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// The arc benches (0099, <see cref="BenchLook"/>): over each bench's picture, which the grid lays, the tool on it as the hand
/// draws it, its modules on it (<see cref="HeldParts"/>, <see cref="HeldArt"/>), clamped where it says, and the vise's jaws laid
/// over it; the shard's cracks alight, drawn unlit, breathing, its glow round it and its violet-white light on the room, now
/// and then a small arc off its tip. As a module goes onto the tool (the sim's stack, so a friend's fit shows too) arcs strike
/// from the shard into the tool's first mount for a moment, its glow brighter and a soft flash, toned down so the bench is
/// still seen. In pixels of the world.
/// </summary>
public partial class BenchView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    private static readonly CanvasItemMaterial Added = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>One bench's sprites, what it showed on its slot last, and its burst's time left, in ms.</summary>
    private sealed class Shown
    {
        public required Sprite2D Tool, Front, Alight, Glow, FitGlow;
        public Stack? Was;
        public bool Seen;
        public double Burst, NextStrike;
    }

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly Dictionary<int, Shown> _shown = new();
    private readonly HashSet<int> _here = new();
    private readonly List<int> _gone = new();
    private readonly List<LightSource> _lights = [];
    private readonly Random _random = new();
    private double _clock;

    public BenchView(IGridQueries grid, IItemsQueries items) => (_grid, _items) = (grid, items);

    /// <summary>The shards' light this frame, for the light feature (<c>LightView.Benches</c>).</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    /// <summary>The benches whose fit's burst plays now: what a recording checks.</summary>
    public int Bursting { get; private set; }

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        _clock += ms;
        _lights.Clear();
        _here.Clear();
        int bursting = 0;
        foreach (var m in _grid.MachinesOf(BenchLook.Kind))
        {
            _here.Add(m.Id);
            if (!_shown.TryGetValue(m.Id, out var s)) _shown[m.Id] = s = Made();
            // Its picture rises over its footprint, the vise over the bench (0099).
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width - ShipTiles.RiseOf(m.Kind)) * Cell;
            var slots = _items.Crate(m.Id);
            var tool = slots.Count > 0 ? slots[0] : null;
            var look = HeldLook.Of(tool?.Item);
            if (s.Seen && tool != null && s.Was?.Item == tool.Item && s.Was.Module == null && tool.Module != null) Burst(s, corner);
            (s.Was, s.Seen) = (tool, true);
            if (tool != null && look != null)
            {
                var parts = HeldParts.Of(look, ToolStats.ModulesOf(tool));
                var (x, y) = BenchLook.ToolAt(look);
                s.Tool.Texture = HeldArt.Of(look.Picture, parts.Layers);
                s.Tool.Position = corner + new Vector2(x, y);
            }
            s.Tool.Visible = tool != null && look != null;
            foreach (var sprite in (Sprite2D[])[s.Front, s.Alight]) sprite.Position = corner;
            s.Glow.Position = s.FitGlow.Position = corner - Vector2.One * BenchLook.GlowMargin;
            // The shard breathes, slowly, each bench in a step of its own.
            double breath = 0.5 + 0.5 * Math.Sin(_clock / 900 + m.Id * 1.7 + 0.6 * Math.Sin(_clock / 2300));
            float shines = (float)(BenchLook.BreathLeast + (BenchLook.BreathMost - BenchLook.BreathLeast) * breath);
            s.Alight.Modulate = new Color(1, 1, 1, shines);
            s.Glow.Modulate = new Color(1, 1, 1, shines);
            double burst = 0;
            if (s.Burst > 0)
            {
                bursting++;
                s.Burst = Math.Max(0, s.Burst - ms);
                burst = s.Burst / BenchLook.BurstMs;
                s.NextStrike -= ms;
                if (s.NextStrike <= 0)
                {
                    s.NextStrike = BenchLook.BurstEveryMs * (0.7 + 0.6 * _random.NextDouble());
                    Strike(corner, Target(look, tool));
                }
            }
            s.FitGlow.Visible = burst > 0;
            s.FitGlow.Modulate = new Color(1, 1, 1, (float)(BenchLook.FitGlowMost * burst));
            var idle = BenchLook.Idle;
            if (_random.NextDouble() < Crackle.Chance(idle, 0) * delta * Crackle.StepsPerSecond) Idle(corner);
            var (lx, ly) = BenchLook.LightAt;
            _lights.Add(new LightSource(corner.X + lx, corner.Y + ly, BenchLook.Light, BenchLook.Strength * shines * (float)(1 + BenchLook.BurstLight * burst),
                BenchLook.Reach, RelicLooks.LightKeep, 1, RelicLooks.LightSize));
        }
        Bursting = bursting;
        _gone.Clear();
        foreach (var id in _shown.Keys)
            if (!_here.Contains(id)) _gone.Add(id);
        foreach (var id in _gone)
        {
            var s = _shown[id];
            foreach (var sprite in (Sprite2D[])[s.Tool, s.Front, s.Alight, s.Glow, s.FitGlow]) sprite.QueueFree();
            _shown.Remove(id);
        }
    }

    /// <summary>A bench's sprites, back to front: the tool and the jaws lit as the machine is, the shard alight and its glows unlit.</summary>
    private Shown Made()
    {
        Sprite2D Sprite(string picture, int z, Material? material)
        {
            var sprite = new Sprite2D { Texture = picture.Length > 0 ? PixelArt.Picture(picture) : null, Centered = false, ZIndex = z };
            if (material != null) sprite.Material = material;
            else Lit.Set(sprite);
            AddChild(sprite);
            return sprite;
        }
        return new Shown
        {
            Tool = Sprite("", Layers.Machines, null),
            Front = Sprite(BenchLook.Front, Layers.Machines, null),
            Alight = Sprite(BenchLook.Alight, Layers.Screens, Unlit),
            Glow = Sprite(BenchLook.Glow, Layers.Screens, Added),
            FitGlow = Sprite(BenchLook.FitGlow, Layers.Screens, Added),
        };
    }

    /// <summary>A module gone onto the tool: the burst begins, its first arcs at once, and a soft flash at the shard.</summary>
    private void Burst(Shown s, Vector2 corner)
    {
        (s.Burst, s.NextStrike) = (BenchLook.BurstMs, 0);
        var (lx, ly) = BenchLook.LightAt;
        Flares.Shared.Flash(corner.X + lx, corner.Y + ly, RelicLooks.Hot, RelicLooks.FlashStrength * BenchLook.FlashStrength * RelicLooks.SmallFlash,
            BenchLook.Reach + 4, RelicLooks.LightKeep, RelicLooks.FlashDecay * 1.5);
    }

    /// <summary>Where the burst's arcs strike, in the bench's picture: the middle of the tool's first mount's box, else where the page drew the drill's coil.</summary>
    private static (float X, float Y) Target(HeldLook? look, Stack? tool)
    {
        if (look?.Mounts is not { Count: > 0 } mounts || tool == null) return BenchLook.Strikes;
        var (x, y) = BenchLook.ToolAt(look);
        var box = mounts[0].Box;
        return ((float)(x + (box.X0 + box.X1) / 2), (float)(y + (box.Y0 + box.Y1) / 2));
    }

    /// <summary>One strike of the burst: an arc from each of the shard's points into the target, crisp and violet-white.</summary>
    private void Strike(Vector2 corner, (float X, float Y) target)
    {
        foreach (var (sx, sy) in BenchLook.Sources)
        {
            var a = corner + new Vector2(sx, sy);
            var b = corner + new Vector2(target.X + (float)(_random.NextDouble() - 0.5) * 2, target.Y + (float)(_random.NextDouble() - 0.5));
            double bow = (_random.NextDouble() - 0.5) * 0.5;
            Bolts.Shared.Add(() => (a.X, a.Y, b.X, b.Y, bow), _random.Next(3, 6), 0.8, 0.6, branchy: _random.NextDouble() < 0.3, tint: BoltTint.Violet);
        }
    }

    /// <summary>A small arc off the shard's tip into the air, as a relic's at rest (0096).</summary>
    private void Idle(Vector2 corner)
    {
        var e = BenchLook.Idle;
        double angle = -Math.PI * (0.1 + 0.8 * _random.NextDouble()), reach = e.Reach * (0.6 + 0.4 * _random.NextDouble());
        var a = corner + new Vector2(e.X, e.Y);
        var b = a + new Vector2((float)(Math.Cos(angle) * reach), (float)(Math.Sin(angle) * reach));
        Crackle.Strike(_random, e, a, b, BoltTint.Violet);
    }
}
