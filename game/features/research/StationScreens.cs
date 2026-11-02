using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Trade;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What a trade station's screen says without a word: lines typed across it while it holds what sells, dark while it holds
/// nothing that does; while its capsule is away, a flash as it goes and then a bar running down to its return. Drawn unlit over the screen of its picture, and lighting the room round it a little (a <see cref="Glow"/>).
/// </summary>
public partial class StationScreens : Node2D
{
    /// <summary>What the stations are doing is read this often; the lines run every frame.</summary>
    private const double RefreshMs = 100;
    /// <summary>The screen in a station's picture, in pixels from its top-left (<c>machine_tradeStation.png</c>).</summary>
    private static readonly Rect2I Screen = new(7, 5, 18, 7);
    /// <summary>Pixels a line of the running text grows a second.</summary>
    private const double Speed = 24;
    /// <summary>How much of its colour the screen's light gives while working.</summary>
    private const float WorkingLight = 0.55f;

    /// <summary>How long a launch whites the screen, in ticks.</summary>
    private const int FlashTicks = 15;

    private static readonly Color Dark = new("10161d"), Line = new("9fd8ff"), Away = new("f0b44c");

    private readonly IGridQueries _grid;
    private readonly TradeModule _trade;
    private readonly List<(Vector2 Corner, bool Working, int? Left)> _screens = [];
    private readonly List<Glow> _glows = [];
    private double _sinceRead = RefreshMs, _clock;

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
            foreach (var (_, on, _) in _screens)
                if (on) working++;
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
        foreach (var m in _grid.MachinesWith(MachineTrait.Sells)) _screens.Add((CornerOf(m), _trade.Worth(m.Id) > 0 && _trade.FlightOf(m.Id) == null, _trade.FlightOf(m.Id)?.Left));
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
            glow.Visible = i < _screens.Count && _screens[i].Working;
            if (!glow.Visible) continue;
            glow.Position = _screens[i].Corner + (Vector2)Screen.Position + (Vector2)Screen.Size / 2;
            // The lines typed so far: the screen brighter the more of it is lit.
            glow.Colour = Line * (WorkingLight * (0.7f + 0.3f * Typed()));
        }
    }

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
        foreach (var (corner, working, left) in _screens)
        {
            var origin = corner + Screen.Position;
            DrawRect(new Rect2(origin, Screen.Size), Dark);
            if (left is int ticks)
            {
                int gone = Market.CapsuleFlight - ticks;
                if (gone < FlashTicks) DrawRect(new Rect2(origin, Screen.Size), Colors.White with { A = 1f - (float)gone / FlashTicks });
                DrawRect(new Rect2(origin + new Vector2(1, 3), new Vector2((Screen.Size.X - 2f) * ticks / Market.CapsuleFlight, 1)), Away);
                continue;
            }
            if (!working) continue;
            // Three lines of text typed across the screen, each starting over when it reaches the edge.
            for (int row = 0; row < 3; row++)
            {
                int length = LineLength(row);
                if (length > Screen.Size.X - 2) continue;
                DrawRect(new Rect2(origin + new Vector2(1, 1 + row * 2), new Vector2(length, 1)), Line);
            }
        }
    }
}
