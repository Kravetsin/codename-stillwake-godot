using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The dark inside a derelict (derelict-looks, <see cref="VesselLights"/>): its red lamps breathing, its one dying lamp flickering,
/// its consoles' screens glowing, some glitching, a red dot over each shut door; each a halo or a face added here, unlit, and its
/// light through <see cref="LightView"/>, which leaves the vessel's own lamps to this (<see cref="Claims"/>). A lamp or console
/// shows only while its machine stands, a dot while its door is shut. No Godot light (0029). Phases run on this client's clock.
/// </summary>
public partial class VesselLamps : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    private readonly IGridQueries _grid;
    private readonly VesselLights _vessel;
    /// <summary>Each lamp's grid cell and where it shines from, in the world's pixels; whether it stands, as of the grid's last version.</summary>
    private readonly (int Cell, Vector2 At)[] _lamps;
    private readonly (int Cell, Vector2 Corner)[] _consoles;
    private readonly (Vector2 Dot, Vector2 Left, Vector2 Right)[] _doors;
    private readonly bool[] _lampStands, _consoleStands, _doorShut;
    private readonly HashSet<int> _lampCells;
    private readonly List<LightSource> _lights = [];
    private int _version = -1;
    private double _timeMs;

    public VesselLamps(IGridQueries grid, VesselLights vessel)
    {
        (_grid, _vessel) = (grid, vessel);
        ZIndex = Layers.Screens;
        Material = Additive;
        int w = grid.Width, m = Content.Vessels.Margin;
        var offset = new Vector2(m, m) * Cell;
        _lamps = new (int, Vector2)[vessel.Lamps.Count];
        for (int i = 0; i < _lamps.Length; i++)
        {
            var lamp = vessel.Lamps[i];
            var spec = Content.Machines.Get(lamp.Kind);
            var corner = offset + new Vector2(lamp.X, lamp.Y) * Cell;
            var at = L.Lamps[lamp.Kind].At is { } from ? corner + new Vector2(from.X, from.Y) : corner + new Vector2(spec.Width, spec.Height) * Cell / 2f;
            _lamps[i] = (VesselLights.CellOf(lamp.X, lamp.Y, w), at);
        }
        _lampCells = vessel.LampCells(w);
        _consoles = new (int, Vector2)[vessel.Consoles.Count];
        for (int i = 0; i < _consoles.Length; i++)
        {
            var c = vessel.Consoles[i];
            _consoles[i] = (VesselLights.CellOf(c.X, c.Y, w), offset + new Vector2(c.X, c.Y) * Cell);
        }
        _doors = new (Vector2, Vector2, Vector2)[vessel.Doors.Count];
        for (int i = 0; i < _doors.Length; i++)
        {
            // Over the doorway's two columns, by its top; its light cast from just outside each side, for a shut door stops light.
            var d = vessel.Doors[i];
            var top = offset + new Vector2(d.X, d.Y) * Cell;
            _doors[i] = (top + new Vector2(Cell, L.DoorDotDown), top + new Vector2(-1, L.DoorDotDown), top + new Vector2(2 * Cell + 1, L.DoorDotDown));
        }
        (_lampStands, _consoleStands, _doorShut) = (new bool[_lamps.Length], new bool[_consoles.Length], new bool[_doors.Length]);
    }

    /// <summary>Whether <paramref name="lamp"/> is one of the vessel's own, lit here, not by <see cref="LightView"/>.</summary>
    public bool Claims(Machine lamp) => VesselLights.Claims(lamp, _lampCells);

    public IReadOnlyList<LightSource> Lights => _lights;

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        if (_grid.Version != _version) Follow();
        _lights.Clear();
        for (int i = 0; i < _lamps.Length; i++)
        {
            var lamp = _vessel.Lamps[i];
            if (!_lampStands[i] || lamp.State == LampState.Dead) continue;
            var look = L.Lamps[VesselLights.Emergency(lamp.State) ? "emergencyLamp" : lamp.Kind];
            float shines = VesselLights.Shines(lamp.State, _timeMs, lamp.PhaseMs);
            var at = _lamps[i].At;
            _lights.Add(new LightSource(at.X, at.Y, look.Colour, shines * look.Strength, look.Reach, look.Keep, L.LampMost, L.LampSize));
        }
        var screen = L.DerelictScreen;
        for (int i = 0; i < _consoles.Length; i++)
        {
            var c = _vessel.Consoles[i];
            if (!_consoleStands[i] || !c.Lit) continue;
            float shines = VesselLights.Screen(c.Glitching, _timeMs, c.PhaseMs);
            foreach (var (x, y, w, h, _) in L.ConsoleScreens[c.Kind])
            {
                var at = _consoles[i].Corner + new Vector2(x + w / 2, y + h / 2);
                _lights.Add(new LightSource(at.X, at.Y, screen.Colour, shines * screen.Strength, screen.Reach, screen.Keep, 1));
            }
        }
        var dot = L.DoorDot;
        for (int i = 0; i < _doors.Length; i++)
        {
            if (!_doorShut[i]) continue;
            foreach (var at in (ReadOnlySpan<Vector2>)[_doors[i].Left, _doors[i].Right])
                _lights.Add(new LightSource(at.X, at.Y, dot.Colour, dot.Strength, dot.Reach, dot.Keep, 1));
        }
        QueueRedraw();
    }

    /// <summary>What still stands and which doors are shut, read again only when the grid changed.</summary>
    private void Follow()
    {
        _version = _grid.Version;
        for (int i = 0; i < _lamps.Length; i++) _lampStands[i] = VesselLights.Stands(_grid, _vessel.Lamps[i].Kind, _lamps[i].Cell);
        for (int i = 0; i < _consoles.Length; i++) _consoleStands[i] = VesselLights.Stands(_grid, _vessel.Consoles[i].Kind, _consoles[i].Cell);
        for (int i = 0; i < _doors.Length; i++) _doorShut[i] = VesselLights.Shut(_grid, _vessel.Doors[i]);
    }

    public override void _Draw()
    {
        var glow = PixelArt.Glow;
        float halo = LightView.GlowCells * Cell;
        for (int i = 0; i < _lamps.Length; i++)
        {
            var lamp = _vessel.Lamps[i];
            if (!_lampStands[i] || lamp.State == LampState.Dead) continue;
            float shines = VesselLights.Shines(lamp.State, _timeMs, lamp.PhaseMs);
            var colour = new Color(L.Lamps[VesselLights.Emergency(lamp.State) ? "emergencyLamp" : lamp.Kind].Halo);
            DrawTextureRect(glow, new Rect2(_lamps[i].At - Vector2.One * halo / 2, Vector2.One * halo), false, colour with { A = shines * LightView.GlowAlpha });
        }
        var screen = new Color(L.DerelictScreen.Halo);
        for (int i = 0; i < _consoles.Length; i++)
        {
            var c = _vessel.Consoles[i];
            if (!_consoleStands[i] || !c.Lit) continue;
            float shines = VesselLights.Screen(c.Glitching, _timeMs, c.PhaseMs);
            foreach (var (x, y, w, h, round) in L.ConsoleScreens[c.Kind])
            {
                var face = new Rect2(_consoles[i].Corner + new Vector2(x, y), new Vector2(w, h));
                var tint = screen with { A = shines * L.ScreenFace };
                if (round) DrawCircle(face.GetCenter(), Math.Min(w, h) / 2, tint);
                else DrawRect(face, tint);
                // Its glow round it, half again as wide.
                var around = face.Grow(Math.Max(w, h) * 0.5f);
                DrawTextureRect(glow, around, false, screen with { A = shines * LightView.GlowAlpha * 0.6f });
            }
        }
        var dot = new Color(L.DoorDot.Halo);
        for (int i = 0; i < _doors.Length; i++)
        {
            if (!_doorShut[i]) continue;
            var at = _doors[i].Dot;
            DrawRect(new Rect2(at - Vector2.One * L.DoorDotSize / 2, Vector2.One * L.DoorDotSize), dot);
            DrawTextureRect(glow, new Rect2(at - Vector2.One * 3, Vector2.One * 6), false, dot with { A = 0.5f });
        }
    }

    /// <summary>Added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial Additive = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };
}
