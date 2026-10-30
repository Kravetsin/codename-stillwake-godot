using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Trade;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What each station's screen says without a word: on a tech station a cursor blinking, waiting to be used; on a trade station
/// lines typed across it while it holds what sells, dark while it holds nothing that does. Drawn unlit over the screen of its
/// picture, and lighting the room round it a little in what it shows (a <see cref="Glow"/>).
/// </summary>
public partial class StationScreens : Node2D
{
    /// <summary>What the stations are doing is read this often; the lines run every frame.</summary>
    private const double RefreshMs = 100;
    /// <summary>The screen in a station's picture, in pixels from its top-left (<c>machine_techStation.png</c>, <c>machine_tradeStation.png</c>).</summary>
    private static readonly Rect2I Screen = new(7, 5, 18, 7);
    /// <summary>Pixels a line of the running text grows a second.</summary>
    private const double Speed = 24;
    /// <summary>How often the waiting cursor blinks, a second.</summary>
    private const double Blinks = 1.6;
    /// <summary>How much of its colour the screen's light gives: waiting, working.</summary>
    private const float WaitingLight = 0.18f, WorkingLight = 0.55f;

    private static readonly Color Dark = new("10161d"), Line = new("9fd8ff"), Dim = new("3a6a8a");

    private readonly IGridQueries _grid;
    private readonly TradeModule _trade;
    private readonly List<(Vector2 Corner, Look Look)> _screens = [];
    private readonly List<Glow> _glows = [];
    private double _sinceRead = RefreshMs, _clock;

    private enum Look { Dark, Waiting, Working }

    public StationScreens(IGridQueries grid, TradeModule trade)
    {
        _grid = grid;
        _trade = trade;
        ZIndex = Layers.Screens;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    /// <summary>How many screens show lines: what a recording checks.</summary>
    public int Working
    {
        get
        {
            int working = 0;
            foreach (var (_, look) in _screens)
                if (look == Look.Working) working++;
            return working;
        }
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
        foreach (var m in _grid.MachinesWith(MachineTrait.Tech)) _screens.Add((CornerOf(m), Look.Waiting));
        foreach (var m in _grid.MachinesWith(MachineTrait.Sells)) _screens.Add((CornerOf(m), _trade.SellingFrom(m.Id) != null ? Look.Working : Look.Dark));
    }

    private Vector2 CornerOf(Machine m) => new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell;

    /// <summary>Each screen's light, its colour as much as the screen shows of it now.</summary>
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
            glow.Position = corner + (Vector2)Screen.Position + (Vector2)Screen.Size / 2;
            glow.Colour = look == Look.Waiting ? Line * (WaitingLight * (CursorOn() ? 1.3f : 0.8f))
                // The lines typed so far: the screen brighter the more of it is lit.
                : Line * (WorkingLight * (0.7f + 0.3f * Typed()));
        }
    }

    private bool CursorOn() => _clock * Blinks % 1 < 0.5;

    /// <summary>How long a typed line is now, in pixels; past the screen's width it waits to start over.</summary>
    private int LineLength(int row) => (int)((_clock * Speed + row * 7) % (Screen.Size.X - 2 + 4));

    private float Typed()
    {
        int width = Screen.Size.X - 2, sum = 0;
        // A line past the edge is the pause before it starts over: nothing shows.
        for (int row = 0; row < 3; row++) sum += LineLength(row) > width ? 0 : LineLength(row);
        return sum / (3f * width);
    }

    public override void _Draw()
    {
        foreach (var (corner, look) in _screens)
        {
            var origin = corner + Screen.Position;
            DrawRect(new Rect2(origin, Screen.Size), Dark);
            switch (look)
            {
                case Look.Waiting:
                    // A prompt mark and a cursor blinking after it.
                    DrawRect(new Rect2(origin + new Vector2(1, 1), new Vector2(1, 1)), Dim);
                    if (CursorOn()) DrawRect(new Rect2(origin + new Vector2(3, 1), new Vector2(2, 1)), Line);
                    break;
                case Look.Working:
                    // Three lines of text typed across the screen, each starting over when it reaches the edge.
                    for (int row = 0; row < 3; row++)
                    {
                        int length = LineLength(row);
                        if (length > Screen.Size.X - 2) continue;
                        DrawRect(new Rect2(origin + new Vector2(1, 1 + row * 2), new Vector2(length, 1)), Line);
                    }
                    break;
            }
        }
    }
}
