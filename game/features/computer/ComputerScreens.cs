using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// What each ship computer's screen shows in the world, by <see cref="Readout"/>: dark with no power; lines of text running up
/// and its rack's lights blinking while powered; the radar's sweep once its antenna is ready. Drawn unlit over its picture,
/// and lighting the room round it in the screen's colour (a <see cref="Glow"/>).
/// </summary>
public partial class ComputerScreens : Node2D
{
    /// <summary>How often the computers are read; the screens run every frame.</summary>
    private const double RefreshMs = 250;
    /// <summary>Frames a second: the text's, the radar's sweep, the rack's lights.</summary>
    private const double TextRate = 2.5, SweepRate = 8, LightRate = 1.2;
    /// <summary>The middle of the screen in the computer's picture, in pixels from its top-left (<c>machine_computer.png</c>).</summary>
    private static readonly Vector2 ScreenMiddle = new(20, 7);
    private static readonly Color TextLight = new Color("7fc4f0") * 0.45f, RadarLight = new Color("6fe08a") * 0.5f;

    private enum Look { Dark, Text, Radar }

    private readonly IGridQueries _grid;
    private readonly IPowerQueries _power;
    private readonly List<(Vector2 Corner, Look Look)> _screens = [];
    private readonly List<Glow> _glows = [];
    private double _sinceRead = RefreshMs, _clock;

    public ComputerScreens(IGridQueries grid, IPowerQueries power)
    {
        (_grid, _power) = (grid, power);
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
        _screens.Clear();
        foreach (var m in _grid.MachinesWith(MachineTrait.Computer))
        {
            var reading = Readout.Of(_grid, _power, m.Id);
            var look = !reading.Powered ? Look.Dark : Readout.Ready(reading, Programs.Radar) ? Look.Radar : Look.Text;
            _screens.Add((new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell, look));
        }
    }

    private void Light()
    {
        while (_glows.Count < _screens.Count)
        {
            var glow = new Glow();
            AddChild(glow);
            _glows.Add(glow);
        }
        for (int i = 0; i < _glows.Count; i++)
        {
            var glow = _glows[i];
            glow.Visible = i < _screens.Count && _screens[i].Look != Look.Dark;
            if (!glow.Visible) continue;
            var (corner, look) = _screens[i];
            glow.Position = corner + ScreenMiddle;
            // A slow breath, so the room's light lives with the screen.
            float breath = 0.85f + 0.15f * Mathf.Sin((float)_clock * 2.2f);
            glow.Colour = (look == Look.Radar ? RadarLight : TextLight) * breath;
        }
    }

    public override void _Draw()
    {
        int text = (int)(_clock * TextRate) % 4, sweep = (int)(_clock * SweepRate) % 8, lights = (int)(_clock * LightRate) % 2;
        foreach (var (corner, look) in _screens)
        {
            if (look == Look.Dark) continue;
            DrawTexture(PixelArt.Picture(look == Look.Radar ? $"computer_radar_{sweep}" : $"computer_screen_{text}"), corner);
            DrawTexture(PixelArt.Picture($"computer_lights_{lights}"), corner);
        }
    }
}
