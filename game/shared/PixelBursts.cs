using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>
/// Every death, alike (2026-10-29-kills-crumble): a killed creature, or a hero's body emptied (2026-10-29-body-bag), stands as
/// its own pixels and crumbles from the top down, each breaking off a little out and away from the striker, falling and heaping
/// on the ground of the surface drawn, and fading (<see cref="PixelDust"/>, drawn by <see cref="PixelsLayer"/>).
/// </summary>
public partial class PixelBursts : Node2D
{
    private const float Cell = ShipCamera.Cell;

    private readonly PixelDust _dust = new(KillLooks.MostPixels);
    private readonly PixelsLayer _layer = new();
    private readonly Random _random = new();
    private readonly Ground _ground = new();

    /// <summary>The pixels flying or lying now: for the frame log and scripted runs.</summary>
    public int Count => _dust.Motes.Count;

    public override void _Ready() => AddChild(_layer);

    /// <summary>The surface drawn: what its pixels fall on and float in.</summary>
    public void On(IGridQueries grid, IRoomsQueries rooms, Spec.Surfaces.Kind rules)
    {
        if (grid != _ground.Grid) _dust.Clear();
        (_ground.Grid, _ground.Rooms, _ground.Rules) = (grid, rooms, rules);
    }

    /// <summary>Moves the pixels <paramref name="elapsedMs"/> on and draws them.</summary>
    public void Frame(double elapsedMs)
    {
        if (_ground.Grid != null) _dust.Step(Math.Max(0, elapsedMs / 1000), _ground);
        _layer.Show(_dust);
    }

    /// <summary>
    /// Crumbles <paramref name="pixels"/> (a world pixel each, its colour), a body drawn about <paramref name="middle"/>: each hangs
    /// where it is, the top breaking off first (<see cref="PixelDust.WaitOf"/>), at most <see cref="KillLooks.BodyPixels"/> of
    /// them, taken evenly; <paramref name="away"/> the striker's other side, 0 for none.
    /// </summary>
    public void Crumble(Dictionary<(int X, int Y), (byte R, byte G, byte B)> pixels, Vector2 middle, int away)
    {
        if (_ground.Grid == null || pixels.Count == 0) return;
        var (top, bottom) = (int.MaxValue, int.MinValue);
        foreach (var (x, y) in pixels.Keys) (top, bottom) = (Math.Min(top, y), Math.Max(bottom, y));
        int every = (pixels.Count + KillLooks.BodyPixels - 1) / KillLooks.BodyPixels, k = 0;
        foreach (var ((x, y), colour) in pixels)
        {
            if (k++ % every != 0) continue;
            var at = new Vector2(x + 0.5f, y + 0.5f);
            var outward = (at - middle).Normalized();
            float side = away != 0 ? away : Between(-1, 1);
            _dust.Add(new PixelDust.Mote
            {
                X = at.X,
                Y = at.Y,
                Vx = outward.X * Between(KillLooks.OutLeast, KillLooks.OutMost) + side * Between(KillLooks.AwayLeast, KillLooks.AwayMost),
                Vy = outward.Y * Between(KillLooks.OutLeast, KillLooks.OutMost) - Between(KillLooks.UpLeast, KillLooks.UpMost),
                Wait = PixelDust.WaitOf(y, top, bottom, Between(0, 1)),
                Lasts = Between(KillLooks.LastsLeast, KillLooks.LastsMost),
                Colour = colour,
            }, _ground);
        }
    }

    /// <summary>
    /// Adds to <paramref name="into"/> every world pixel a picture <paramref name="w"/> by <paramref name="h"/> (RGBA
    /// <paramref name="data"/>) covers drawn through <paramref name="toWorld"/>, the colour of the picture's pixel under its middle:
    /// sampled from the world's side, so a picture drawn larger or turned leaves no gaps.
    /// </summary>
    public static void Paint(Dictionary<(int X, int Y), (byte R, byte G, byte B)> into, Transform2D toWorld, int w, int h, byte[] data)
    {
        var back = toWorld.AffineInverse();
        Vector2[] corners = [toWorld * Vector2.Zero, toWorld * new Vector2(w, 0), toWorld * new Vector2(0, h), toWorld * new Vector2(w, h)];
        var (left, top, right, bottom) = (float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        foreach (var c in corners) (left, top, right, bottom) = (MathF.Min(left, c.X), MathF.Min(top, c.Y), MathF.Max(right, c.X), MathF.Max(bottom, c.Y));
        for (int y = (int)MathF.Floor(top); y < (int)MathF.Ceiling(bottom); y++)
            for (int x = (int)MathF.Floor(left); x < (int)MathF.Ceiling(right); x++)
            {
                var p = back * new Vector2(x + 0.5f, y + 0.5f);
                int px = (int)MathF.Floor(p.X), py = (int)MathF.Floor(p.Y);
                if (px < 0 || py < 0 || px >= w || py >= h) continue;
                int o = (py * w + px) * 4;
                if (data[o + 3] == 0) continue;
                into[(x, y)] = (data[o], data[o + 1], data[o + 2]);
            }
    }

    /// <summary>Every pixel gone: another surface, or a load.</summary>
    public void Clear() => _dust.Clear();

    private float Between(float least, float most) => least + (float)_random.NextDouble() * (most - least);

    /// <summary>The surface's cells as the pixels meet them: solid where a stack on the floor stops, afloat in space.</summary>
    private sealed class Ground : PixelDust.IGround
    {
        public IGridQueries? Grid;
        public IRoomsQueries? Rooms;
        public Spec.Surfaces.Kind? Rules;

        private int? CellOf(int x, int y)
        {
            int cx = (int)MathF.Floor(x / Cell), cy = (int)MathF.Floor(y / Cell);
            return cx < 0 || cy < 0 || cx >= Grid!.Width || cy >= Grid.Height ? null : cy * Grid.Width + cx;
        }

        public bool Solid(int x, int y) => CellOf(x, y) is int cell && Spec.Structures.Solid(Grid!.StructureAt(cell));

        public bool Afloat(int x, int y) => CellOf(x, y) is int cell && Rooms!.Afloat(cell);

        // A stack's fall, in thousandths of a cell a tick each tick, as pixels a second each second, made heavier.
        public float Gravity => Rules!.Fall(Spec.Floor.Gravity) * Cell / 1000f * Sim.World.TicksPerSecond * Sim.World.TicksPerSecond * KillLooks.Weight;
    }
}
