using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// A meteor seen coming (2026-10-28-hull-meteors, 2026-10-28-meteor-blast): through its warning a rock, bigger the stronger it
/// is, flies in from up the drift's side onto the wall it will strike, its front white-hot, trailing light, embers and smoke; at
/// the strike, sparks and a shock ring at the wall, and out of every wall it broke chunks and dust flung on along its way, with a
/// metal break or a clang by whether any went. It reads the sim's meteor once a tick.
/// </summary>
public partial class MeteorView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>How far right of its wall a meteor is first seen, and how far above, in cells: it falls in slant.</summary>
    private const double Reach = 60, Drop = 18;
    private static readonly (int R, int G, int B) Hot = (255, 236, 196), Glow = (255, 170, 90);
    private static readonly Color Rock = new(0.36f, 0.31f, 0.28f), Front = new(1f, 0.82f, 0.55f);
    /// <summary>Embers shed along the way, smoke, what a broken wall throws, its chunks hot from the blow, and a held rock's shards: no gravity out in space.</summary>
    private static readonly Luminous Embers = Luminous.Ember with { Gravity = 0 }, Smoke = Luminous.Vapour with { Gravity = 0 },
        Dust = Luminous.BreakDust with { Gravity = 0 },
        Chunk = Luminous.Chip with { Gravity = 0, Size = 2.2, Matter = false, Ramp = [(255, 222, 170), (230, 130, 70), (120, 100, 100)], LifeMs = (900, 1500) },
        Shard = Chunk with { Ramp = [(255, 236, 200), (210, 120, 70), (92, 79, 71)], LifeMs = (600, 1100) };

    private readonly IGridQueries _grid;
    private readonly HazardsModule _hazards;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
    private int _read = -1;
    private Meteor? _coming;
    /// <summary>The walls and back walls round the meteor coming, as the last tick left them: those gone at its strike are what it broke.</summary>
    private readonly List<(int Cell, bool Back)> _round = [];
    private (double X, double Y, double Radius)? _head;
    private readonly Voices _strikes = new(S.World, voices: 2);

    public MeteorView(IGridQueries grid, HazardsModule hazards, Func<double> progress, Func<int> tick)
    {
        _grid = grid;
        _hazards = hazards;
        _progress = progress;
        _tick = tick;
        ZIndex = Layers.Effects;
        // A hot rock: seen by its own glow, in the dark too.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        AddChild(_strikes);
    }

    public override void _Process(double delta)
    {
        if (_tick() != _read)
        {
            _read = _tick();
            var now = _hazards.Incoming;
            if (_coming is { } was && now == null) Strike(was);
            _coming = now;
            if (now != null) Look(now);
        }
        _head = _coming is { } meteor ? Fly(meteor) : null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_head is not var (x, y, r)) return;
        var at = new Vector2((float)x, (float)y);
        // Its front, toward where it goes, white-hot; the rest of the rock behind it.
        var ahead = Way.Normalized() * (float)(r * 0.35);
        DrawCircle(at + ahead, (float)(r * 0.9), Front);
        DrawCircle(at - ahead * 0.6f, (float)r, Rock);
        DrawCircle(at + ahead * 1.4f, (float)(r * 0.45), Colors.White);
    }

    /// <summary>The way a meteor flies, in pixels: from up the drift's side down to its wall.</summary>
    private static Vector2 Way => new(-(float)Reach, (float)Drop);

    /// <summary>The wall's face toward the drift, where a meteor at <paramref name="cell"/> lands, in pixels of the world.</summary>
    private (double X, double Y) Face(int cell) => ((cell % _grid.Width + 1) * Cell, (cell / _grid.Width + 0.5) * Cell);

    /// <summary>The walls and back walls within reach of <paramref name="meteor"/>'s blast, as they stand this tick.</summary>
    private void Look(Meteor meteor)
    {
        _round.Clear();
        int width = _grid.Width, cx = meteor.Cell % width, cy = meteor.Cell / width;
        int most = _hazards.Shower is { } shower ? HazardsModule.Farthest(meteor.Power, shower) : 0;
        for (int y = Math.Max(0, cy - most); y <= Math.Min(_grid.Height - 1, cy + most); y++)
            for (int x = Math.Max(0, cx - most); x <= Math.Min(width - 1, cx + most); x++)
            {
                int cell = y * width + x;
                if (_grid.WallAt(cell) != null) _round.Add((cell, false));
                if (_grid.BackAt(cell) != null) _round.Add((cell, true));
            }
    }

    /// <summary>The rock's head and size, the share of the warning still to run; its light, trail, embers and smoke stamped.</summary>
    private (double X, double Y, double Radius) Fly(Meteor meteor)
    {
        var (x, y) = Face(meteor.Cell);
        int warning = _hazards.Shower?.Warning ?? 1;
        double left = Math.Clamp((meteor.Left - _progress()) / warning, 0, 1), near = 1 - left;
        double hx = x + left * Reach * Cell, hy = y - left * Drop * Cell, radius = 1.5 + 0.6 * meteor.Power;
        double tail = (6 + 2 * meteor.Power) * Cell;
        var flares = Stages.Of(Layers.Effects).Flares;
        flares.Trail(hx, hy, hx + tail, hy - tail * Drop / Reach, Hot, 0.7, (0.5 + 0.5 * near) * Fx.Glow, radius * 0.9);
        flares.Bloom(hx, hy, radius * 5, Glow, (0.35 + 0.35 * near) * Fx.Glow);
        flares.Hold(this, hx, hy, Glow, (0.3 + 0.5 * near) * Fx.Glow, 8, 0.6f, 0.8);
        var glims = Stages.Of(Layers.Effects).Glims;
        // Shed backward, slower than it flies: speed in pixels a second, the way back along its path.
        double vx = Reach * Cell * 20 / warning, vy = -Drop * Cell * 20 / warning;
        for (int i = 0; i < 2; i++)
            glims.Add(Embers, hx + glims.Between(-radius, radius), hy + glims.Between(-radius, radius),
                -vx * glims.Between(-0.05, 0.15), -vy * glims.Between(-0.05, 0.15), size: glims.Between(0.5, 1));
        if (glims.Between(0, 1) < 0.5)
            glims.Add(Smoke, hx + glims.Between(-radius, radius), hy + glims.Between(-radius, radius), glims.Between(-4, 4), glims.Between(-4, 4),
                bright: 0.5, size: glims.Between(1.5, 2.5) * radius / 3);
        return (hx, hy, radius);
    }

    /// <summary>
    /// The strike at <paramref name="meteor"/>'s wall: the wreck's bite thrown back toward the drift and a shock ring as wide as what
    /// broke; out of each wall and back wall gone, chunks and dust flung on the way it flew.
    /// </summary>
    private void Strike(Meteor meteor)
    {
        var (x, y) = Face(meteor.Cell);
        var stage = Stages.Of(Layers.Effects);
        var glims = stage.Glims;
        stage.Hits.Throw(HitLooks.Of[Impact.Wreck], x, y, 1, 0, 1);
        int broke = 0;
        double farthest = 0;
        var way = Way.Normalized();
        foreach (var (cell, isBack) in _round)
        {
            if ((isBack ? _grid.BackAt(cell) : _grid.WallAt(cell)) != null) continue;
            broke++;
            double cx = (cell % _grid.Width + 0.5) * Cell, cy = (cell / _grid.Width + 0.5) * Cell;
            farthest = Math.Max(farthest, Math.Sqrt((cx - x) * (cx - x) + (cy - y) * (cy - y)));
            // A back wall gone throws a chunk where a wall throws three.
            for (int i = 0; i < (isBack ? 1 : 3); i++)
            {
                double s = glims.Between(40, 140), a = Math.Atan2(way.Y, way.X) + glims.Between(-0.9, 0.9);
                glims.Add(Chunk, cx + glims.Between(-3, 3), cy + glims.Between(-3, 3), Math.Cos(a) * s, Math.Sin(a) * s, size: glims.Between(1.4, 2.6));
            }
            for (int i = 0; i < 3; i++)
            {
                double a = glims.Between(0, Math.Tau), s = glims.Between(6, 30);
                glims.Add(Dust, cx + glims.Between(-4, 4), cy + glims.Between(-4, 4), Math.Cos(a) * s, Math.Sin(a) * s,
                    bright: glims.Between(0.6, 0.9), size: glims.Between(0.5, 0.9));
            }
        }
        if (broke > 0)
        {
            stage.Flares.Shock(x, y, 4, farthest + 2 * Cell, 400, (255, 220, 170), 0.6 * Fx.Glow, 0.6);
            stage.Flares.Flash(x, y, Glow, 0.9 * Fx.Glow, 10, 0.6f, 0.9);
        }
        else
        {
            // The wall held: the rock shatters on it, its pieces glowing as they cool, flung back out the way it came.
            double back = Math.Atan2(-way.Y, -way.X);
            for (int i = 0; i < 6 + 2 * meteor.Power; i++)
            {
                double s = glims.Between(30, 120), a = back + glims.Between(-1.2, 1.2);
                glims.Add(Shard, x + glims.Between(-2, 2), y + glims.Between(-2, 2), Math.Cos(a) * s, Math.Sin(a) * s, size: glims.Between(1, 2));
            }
            stage.Flares.Glint(x, y, 12, (255, 236, 200));
            stage.Flares.Shock(x, y, 2, 3 * Cell, 250, (255, 220, 170), 0.4 * Fx.Glow, 0.5);
            stage.Flares.Flash(x, y, Glow, 0.6 * Fx.Glow, 6, 0.6f, 0.85);
        }
        _strikes.Play(broke > 0 ? S.Break : S.Chop, new Vector2((float)x, (float)y));
        _round.Clear();
    }
}
