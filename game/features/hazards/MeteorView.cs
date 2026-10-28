using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Hazards;

/// <summary>
/// A meteor seen coming (2026-10-28-hull-meteors): through its warning a white-hot head flies in from the drift's side with its
/// trail, onto the wall it will strike, lighting what it passes; at the strike, sparks, dust and chips out of the wall's face and
/// a metal break or a clang, by whether the wall went. It reads the sim's meteor once a tick.
/// </summary>
public partial class MeteorView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>How far right of its wall a meteor is first seen, and how far above, in cells: it falls in slant.</summary>
    private const double Reach = 110, Drop = 22;
    private static readonly (int R, int G, int B) Hot = (255, 236, 196), Glow = (255, 170, 90);

    private readonly IGridQueries _grid;
    private readonly HazardsModule _hazards;
    private readonly Func<double> _progress;
    private readonly Func<int> _tick;
    private int _read = -1;
    private Meteor? _coming;
    private readonly Voices _strikes = new(S.World, voices: 2);

    public MeteorView(IGridQueries grid, HazardsModule hazards, Func<double> progress, Func<int> tick)
    {
        _grid = grid;
        _hazards = hazards;
        _progress = progress;
        _tick = tick;
        AddChild(_strikes);
    }

    public override void _Process(double delta)
    {
        if (_tick() != _read)
        {
            _read = _tick();
            var now = _hazards.Incoming;
            if (_coming is { } was && now == null) Strike(was.Cell);
            _coming = now;
        }
        if (_coming is { } meteor) Fly(meteor);
    }

    /// <summary>The wall's face toward the drift, where a meteor at <paramref name="cell"/> lands, in pixels of the world.</summary>
    private (double X, double Y) Face(int cell) => ((cell % _grid.Width + 1) * Cell, (cell / _grid.Width + 0.5) * Cell);

    /// <summary>The head and its trail, the share of the warning still to run: from far up the drift's side down onto the face.</summary>
    private void Fly(Meteor meteor)
    {
        var (x, y) = Face(meteor.Cell);
        int warning = _hazards.Rules.Meteors?.Warning ?? 1;
        double left = Math.Clamp((meteor.Left - _progress()) / warning, 0, 1);
        double hx = x + left * Reach * Cell, hy = y - left * Drop * Cell;
        double tail = 0.18 * Reach * Cell, tx = hx + tail, ty = hy - tail * Drop / Reach;
        // Brighter as it nears: first a far spark, then a streak.
        double near = 1 - left;
        var flares = Stages.Of(Layers.Effects).Flares;
        flares.Trail(hx, hy, tx, ty, Hot, 0.7, (0.35 + 0.65 * near) * Fx.Glow, 1.4);
        flares.Bloom(hx, hy, 10 + 8 * near, Glow, (0.25 + 0.35 * near) * Fx.Glow);
        flares.Hold(this, hx, hy, Glow, 0.5 * near * Fx.Glow, 6, 0.6f, 0.8);
        var glims = Stages.Of(Layers.Effects).Glims;
        if (glims.Between(0, 1) < 0.6)
            glims.Add(Embers, hx + glims.Between(-2, 2), hy + glims.Between(-2, 2), glims.Between(10, 50), glims.Between(-20, 10), size: glims.Between(0.5, 0.9));
    }

    /// <summary>Embers shed along the way, and a broken wall's dust: no gravity out in space.</summary>
    private static readonly Luminous Embers = Luminous.Ember with { Gravity = 0 }, Dust = Luminous.BreakDust with { Gravity = 0 };

    /// <summary>The strike at <paramref name="cell"/>'s face: the wreck's bite thrown back toward the drift, a broken wall's dust too.</summary>
    private void Strike(int cell)
    {
        var (x, y) = Face(cell);
        bool broke = _grid.WallAt(cell) == null;
        var stage = Stages.Of(Layers.Effects);
        stage.Hits.Throw(HitLooks.Of[Impact.Wreck], x, y, 1, 0, 1);
        if (broke)
        {
            var glims = stage.Glims;
            for (int i = 0; i < 18; i++)
            {
                double a = glims.Between(0, Math.Tau), s = glims.Between(6, 30);
                glims.Add(Dust, x - Cell / 2.0 + glims.Between(-4, 4), y + glims.Between(-4, 4),
                    Math.Cos(a) * s, Math.Sin(a) * s, bright: glims.Between(0.6, 0.9), size: glims.Between(0.45, 0.8));
            }
        }
        _strikes.Play(broke ? S.Break : S.Chop, new Vector2((float)x, (float)y));
    }
}
