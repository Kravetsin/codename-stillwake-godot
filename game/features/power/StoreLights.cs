using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// What each capacitor bank says without a word (0101): its gauge of six segments lit from the foot as far as it is full,
/// the top one as bright as it is full within it, green, amber under a quarter as a battery's bar; a spark running up the
/// lit segments while it takes power in, down them while it gives power out, none while it holds. Drawn unlit over its
/// picture (<c>machine_capacitorBank.png</c>), the gauge lighting the room round it a little in its colour (a
/// <see cref="Glow"/>), more the fuller it is, none when empty. Numbers are in the power overlay's tip.
/// </summary>
public partial class StoreLights : Node2D
{
    /// <summary>What the banks hold is read this often; the spark runs every frame.</summary>
    private const double RefreshMs = 100;
    /// <summary>The gauge's segments in the bank's picture: the lowest's top-left, their size and the step up to the next, in pixels.</summary>
    private const int Segments = StoreGauge.Segments;
    private static readonly Vector2I Lowest = new(14, 23), Size = new(4, 2);
    private const int Step = 3;
    private const float LightOf = 0.35f;

    private static readonly Color Green = new(Charges.Green), Amber = new(Charges.Amber), Spark = new("f4fff0");

    private readonly IGridQueries _grid;
    private readonly IPowerQueries _power;
    private readonly List<(Vector2 Corner, StoreLevel Level)> _banks = [];
    private readonly List<Glow> _glows = [];
    private double _sinceRead = RefreshMs, _clock;

    public StoreLights(IGridQueries grid, IPowerQueries power)
    {
        _grid = grid;
        _power = power;
        ZIndex = Layers.Screens;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    /// <summary>The kinds of machine that store a network's energy.</summary>
    private static readonly string[] Kinds = [.. Content.Machines.All.Where(s => s.Store != null).Select(s => s.Kind)];

    public override void _Process(double delta)
    {
        _clock += delta;
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs)
        {
            _sinceRead = 0;
            _banks.Clear();
            foreach (var kind in Kinds)
                foreach (var m in _grid.MachinesOf(kind))
                    if (_power.Store(m.Id) is { } level)
                        _banks.Add((new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell, level));
        }
        Light();
        QueueRedraw();
    }

    /// <summary>A store's colour by how full it is: amber under a quarter, as a battery's bar.</summary>
    public static Color ColourOf(int energy, int capacity) => Charges.BarOf(energy, capacity) == ChargeBar.Low ? Amber : Green;

    private void Light()
    {
        while (_glows.Count < _banks.Count)
        {
            var glow = new Glow();
            AddChild(glow);
            _glows.Add(glow);
        }
        for (int i = 0; i < _glows.Count; i++)
        {
            var glow = _glows[i];
            glow.Visible = i < _banks.Count && _banks[i].Level.Energy > 0;
            if (!glow.Visible) continue;
            var (corner, level) = _banks[i];
            float share = (float)level.Energy / level.Capacity;
            glow.Position = corner + new Vector2(Lowest.X + Size.X / 2f, Lowest.Y + Size.Y - Step * Segments * share / 2);
            glow.Colour = ColourOf(level.Energy, level.Capacity) * (LightOf * (0.3f + 0.7f * share));
        }
    }

    public override void _Draw()
    {
        foreach (var (corner, level) in _banks)
        {
            var colour = ColourOf(level.Energy, level.Capacity);
            for (int s = 0; s < Segments; s++)
            {
                float lit = StoreGauge.Lit(s, level.Energy, level.Capacity);
                if (lit <= 0) continue;
                var at = corner + (Vector2)(Lowest - new Vector2I(0, Step * s));
                var shown = colour.Darkened(0.75f).Lerp(colour, lit).Lerp(Spark, 0.7f * StoreGauge.SparkAt(s, level.Flow, _clock) * lit);
                DrawRect(new Rect2(at, Size), shown);
            }
        }
    }
}
