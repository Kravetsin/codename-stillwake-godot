using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Trade;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// Trade stations' capsules going off and coming back (2026-10-30-trade-capsule), behind the hull, so a station stands anywhere with
/// no hatch to it: seen where there is no back wall and past the hull. A thud as one goes, the blip of something done as one is home.
/// </summary>
public partial class CapsuleFlights : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private static readonly Texture2D Capsule = AsciiArt.Capsule();
    private static readonly Color Flame = new("ffb066");

    private readonly IGridQueries _grid;
    private readonly TradeModule _trade;
    private readonly Func<double> _progress;
    private readonly Dictionary<int, bool> _away = new();
    private readonly List<(Vector2 At, CapsulePath.Pose Pose)> _seen = [];
    private readonly Heard2D _off = Cues.Player2D(S.CapsuleOff, S.World, voices: 2), _home = Cues.Player2D(S.ChargeFull, S.World, voices: 2);
    private double _clock;

    public CapsuleFlights(IGridQueries grid, TradeModule trade, Func<double> progress)
    {
        _grid = grid;
        _trade = trade;
        _progress = progress;
        ZIndex = Layers.Capsules;
        // Seen by its own light, out where nothing lights it.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        AddChild(_off);
        AddChild(_home);
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        _seen.Clear();
        foreach (var m in _grid.MachinesWith(MachineTrait.Sells))
        {
            var spec = Machines.Get(m.Kind);
            var middle = new Vector2(m.Cell % _grid.Width + spec.Width / 2f, m.Cell / _grid.Width + spec.Height / 2f);
            var flight = _trade.FlightOf(m.Id);
            Hear(m.Id, flight != null, GlobalTransform * (middle * Cell));
            if (flight != null && CapsulePath.At(flight.Left - _progress(), Market.CapsuleFlight) is { } pose) _seen.Add((middle, pose));
        }
        QueueRedraw();
    }

    /// <summary>A thud or a blip as a station's capsule is seen to go or come back; none for one first seen as it already is.</summary>
    private void Hear(int station, bool away, Vector2 at)
    {
        if (_away.TryGetValue(station, out bool was) && was != away) (away ? _off : _home).PlayAt(at);
        _away[station] = away;
    }

    public override void _Draw()
    {
        foreach (var (middle, pose) in _seen)
        {
            var at = new Vector2((float)(middle.X + pose.X), (float)(middle.Y + pose.Y)) * Cell;
            float scale = (float)pose.Scale;
            // Tilted the way it flies; its flame flickering under it.
            DrawSetTransform(at, Mathf.Atan2(-(float)CapsulePath.Astern, (float)CapsulePath.Rise), new Vector2(scale, scale));
            var size = Capsule.GetSize();
            float flicker = 2.2f + 0.8f * Mathf.Sin((float)_clock * 40);
            DrawCircle(new Vector2(0, size.Y / 2 + flicker), flicker, Flame with { A = (float)pose.Alpha });
            DrawTexture(Capsule, -size / 2, Colors.White with { A = (float)pose.Alpha });
        }
        DrawSetTransform(Vector2.Zero);
    }
}
