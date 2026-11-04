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
    /// <summary>Its frames of flame a second, and the top middle of the flame's picture, from the capsule's middle (the subject's bell).</summary>
    private const double FlameFps = 15;
    private static readonly Vector2 Nozzle = new(0, 10.9f);
    private const float KickedFlame = 1.4f;
    private static readonly Texture2D Capsule = PixelArt.Picture("capsule");
    private static readonly Texture2D[] Flames = [PixelArt.Picture("capsule_flame_0"), PixelArt.Picture("capsule_flame_1"), PixelArt.Picture("capsule_flame_2")];

    private readonly IGridQueries _grid;
    private readonly TradeModule _trade;
    private readonly Func<double> _progress;
    private readonly Dictionary<int, bool> _away = new();
    private readonly List<(Vector2 At, CapsulePath.Pose Pose, bool Kicks)> _seen = [];
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
            double left = flight == null ? 0 : flight.Left - _progress();
            if (flight != null && CapsulePath.At(left, Market.CapsuleFlight) is { } pose) _seen.Add((middle, pose, CapsulePath.Kicks(left, Market.CapsuleFlight)));
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
        var flame = Flames[(int)(_clock * FlameFps) % Flames.Length];
        foreach (var (middle, pose, kicks) in _seen)
        {
            var at = new Vector2((float)(middle.X + pose.X), (float)(middle.Y + pose.Y)) * Cell;
            float scale = (float)pose.Scale;
            var shown = Colors.White with { A = (float)pose.Alpha };
            // Tilted the way it flies; its flame hung under it, longer as it lifts off and touches down.
            DrawSetTransform(at, Mathf.Atan2(-(float)CapsulePath.Astern, (float)CapsulePath.Rise), new Vector2(scale, scale));
            var size = flame.GetSize();
            DrawTextureRect(flame, new Rect2(Nozzle.X - size.X / 2, Nozzle.Y, size.X, size.Y * (kicks ? KickedFlame : 1)), false, shown);
            DrawTexture(Capsule, -Capsule.GetSize() / 2, shown);
        }
        DrawSetTransform(Vector2.Zero);
    }
}
