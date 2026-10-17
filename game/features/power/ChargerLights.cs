using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// What each charging station says without a word (0053): its light pulsing green while it charges,
/// steady once its battery is full on a network that makes power, dark empty or unpowered; a battery in
/// its bay with its charge rising up it. Drawn unlit over its picture (<c>machine_charger.png</c>), its
/// light lighting the room round it a little in the same green (a <see cref="Glow"/>), none when dark.
/// </summary>
public partial class ChargerLights : Node2D
{
    /// <summary>What the stations are doing is read this often; the pulse runs every frame.</summary>
    private const double RefreshMs = 100;
    /// <summary>The light and the bay in the station's picture, in pixels from its top-left.</summary>
    private static readonly Rect2I Lens = new(5, 7, 6, 2), Bay = new(5, 12, 6, 12);
    /// <summary>How often the light pulses a second while it charges, and how much of its colour its light gives.</summary>
    private const double Pulses = 0.8;
    private const float ChargingLight = 0.6f, FullLight = 0.45f;

    private static readonly Color Green = new(Charges.Green), Dim = new("2c4a2a"), Shell = new("7a8593"), Empty = new("1d2128");

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IPowerQueries _power;
    private readonly List<(Vector2 Corner, Look Look, float Share, ChargeBar? Bar)> _stations = [];
    private readonly List<Glow> _glows = [];
    private double _sinceRead = RefreshMs, _clock;

    private enum Look { Dark, Charging, Full }

    public ChargerLights(IGridQueries grid, IItemsQueries items, IPowerQueries power)
    {
        _grid = grid;
        _items = items;
        _power = power;
        ZIndex = Layers.Screens;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs)
        {
            _sinceRead = 0;
            Read();
        }
        Light();
        QueueRedraw();
    }

    private void Read()
    {
        _stations.Clear();
        foreach (var kind in Kinds)
            foreach (var m in _grid.MachinesOf(kind))
            {
                var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell;
                var held = _items.Crate(m.Id) is { Count: > 0 } slots ? slots[0] : null;
                float share = held == null ? -1 : Charges.Share(held);
                var look = _power.Charges(m.Id) ? Look.Charging
                    : held != null && share >= 1 && Powered(m) ? Look.Full
                    : Look.Dark;
                _stations.Add((corner, look, share, held == null ? null : Charges.BarOf(held)));
            }
    }

    /// <summary>The kinds of machine that charge what they hold.</summary>
    private static readonly string[] Kinds = [.. Content.Machines.All.Where(s => s.Charges).Select(s => s.Kind)];

    /// <summary>Whether a network with something making power reaches the station: a full one draws nothing, so its share says nothing.</summary>
    private bool Powered(Machine m) => _power.NodeAt(m.Cell) is int node && _power.NetworkOf(node) is { Supply: > 0 };

    /// <summary>How bright the light is now: pulsing while it charges, steady when full.</summary>
    private float Brightness(Look look) => look switch
    {
        Look.Charging => 0.7f + 0.3f * (float)Math.Cos(_clock * Pulses * 2 * Math.PI),
        Look.Full => 1,
        _ => 0,
    };

    private void Light()
    {
        while (_glows.Count < _stations.Count)
        {
            var glow = new Glow();
            AddChild(glow);
            _glows.Add(glow);
        }
        for (int i = 0; i < _glows.Count; i++)
        {
            var glow = _glows[i];
            glow.Visible = i < _stations.Count && _stations[i].Look != Look.Dark;
            if (!glow.Visible) continue;
            var (corner, look, _, _) = _stations[i];
            glow.Position = corner + (Vector2)Lens.Position + (Vector2)Lens.Size / 2;
            glow.Colour = Green * ((look == Look.Charging ? ChargingLight : FullLight) * (0.6f + 0.4f * Brightness(look)));
        }
    }

    public override void _Draw()
    {
        foreach (var (corner, look, share, bar) in _stations)
        {
            var lens = new Rect2(corner + Lens.Position, Lens.Size);
            DrawRect(lens, look == Look.Dark ? Empty : Dim.Lerp(Green, Brightness(look)));
            if (share < 0) continue;
            // The battery standing in the bay, its charge rising from the foot.
            var shell = new Rect2(corner + Bay.Position + new Vector2(1, 1), Bay.Size - new Vector2(2, 1));
            DrawRect(shell, Shell);
            var inside = shell.Grow(-1);
            DrawRect(inside, Empty);
            float filled = Mathf.Round(inside.Size.Y * share);
            if (filled > 0)
                DrawRect(new Rect2(inside.Position + new Vector2(0, inside.Size.Y - filled), new Vector2(inside.Size.X, filled)), bar == ChargeBar.Low ? new Color(Charges.Amber) : Green);
            DrawRect(new Rect2(shell.Position + new Vector2(2, -1), new Vector2(shell.Size.X - 4, 1)), Shell);
        }
    }
}
