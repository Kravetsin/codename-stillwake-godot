using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// Every creature's death, alike (2026-10-29-kill-feel): held white a moment, it bursts into its own pixels, which fly out
/// from its middle and away from the striker, carrying its speed, fall and heap on the ground of the surface drawn, and fade
/// (<see cref="PixelDust"/>, drawn by <see cref="PixelsLayer"/>).
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
    /// Bursts <paramref name="pixels"/> (a world pixel each, its colour) out of <paramref name="middle"/>: at most
    /// <see cref="KillLooks.PopPixels"/> of them, taken evenly; <paramref name="away"/> the striker's other side, 0 for none;
    /// <paramref name="velocity"/> the body's, in pixels a second.
    /// </summary>
    public void Burst(Dictionary<(int X, int Y), (byte R, byte G, byte B)> pixels, Vector2 middle, int away, Vector2 velocity)
    {
        if (_ground.Grid == null || pixels.Count == 0) return;
        int every = (pixels.Count + KillLooks.PopPixels - 1) / KillLooks.PopPixels, k = 0;
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
                Vx = outward.X * Between(KillLooks.OutLeast, KillLooks.OutMost) + side * Between(KillLooks.AwayLeast, KillLooks.AwayMost) + velocity.X,
                Vy = outward.Y * Between(KillLooks.OutLeast, KillLooks.OutMost) - Between(KillLooks.UpLeast, KillLooks.UpMost) + velocity.Y,
                Lasts = Between(KillLooks.LastsLeast, KillLooks.LastsMost),
                Colour = colour,
            }, _ground);
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
