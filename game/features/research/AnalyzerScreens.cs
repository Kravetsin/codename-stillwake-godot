using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What each analyzer's screen says without a word: lines running across it while it works, a green
/// check once what it holds is analysed, dark when it is empty or has no power. Drawn unlit over the
/// screen of its picture, for a screen gives its own light.
/// </summary>
public partial class AnalyzerScreens : Node2D
{
    /// <summary>What the analyzers are doing is read this often; the lines run every frame.</summary>
    private const double RefreshMs = 100;
    /// <summary>The screen in the analyzer's picture, in pixels from its top-left (<c>machine_analyzer.png</c>).</summary>
    private static readonly Rect2I Screen = new(7, 5, 18, 7);
    /// <summary>Pixels a line of the running text grows a second.</summary>
    private const double Speed = 24;

    private static readonly Color Dark = new("10161d"), Line = new("9fd8ff"), Check = new("4ade80");
    /// <summary>A check two pixels thick, its pixels from the screen's top-left.</summary>
    private static readonly (int X, int Y)[] CheckPixels =
        [(11, 1), (12, 1), (10, 2), (11, 2), (5, 3), (6, 3), (9, 3), (10, 3), (6, 4), (7, 4), (8, 4), (9, 4), (7, 5), (8, 5)];

    private readonly IGridQueries _grid;
    private readonly IResearchQueries _research;
    private readonly List<(Vector2 Corner, Look Look)> _screens = [];
    private double _sinceRead = RefreshMs, _clock;

    private enum Look { Dark, Working, Done }

    public AnalyzerScreens(IGridQueries grid, IResearchQueries research)
    {
        _grid = grid;
        _research = research;
        ZIndex = Layers.Screens;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    /// <summary>How many screens show lines and how many a check: what a recording checks.</summary>
    public (int Working, int Done) Shown
    {
        get
        {
            int working = 0, done = 0;
            foreach (var (_, look) in _screens)
            {
                if (look == Look.Working) working++;
                if (look == Look.Done) done++;
            }
            return (working, done);
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
        QueueRedraw();
    }

    private void Read()
    {
        _screens.Clear();
        foreach (var m in _grid.Machines)
        {
            if (m.Kind != "analyzer") continue;
            var corner = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell;
            var at = _research.At(m.Id);
            var look = at == null || at.Problem == "research.unpowered" ? Look.Dark
                : at.Problem == "research.nothingNew" ? Look.Done
                : Look.Working;
            _screens.Add((corner, look));
        }
    }

    public override void _Draw()
    {
        foreach (var (corner, look) in _screens)
        {
            var origin = corner + Screen.Position;
            DrawRect(new Rect2(origin, Screen.Size), Dark);
            if (look == Look.Done)
                foreach (var (x, y) in CheckPixels) DrawRect(new Rect2(origin + new Vector2(x, y), Vector2.One), Check);
            if (look != Look.Working) continue;
            // Three lines of text typed across the screen, each starting over when it reaches the edge.
            for (int row = 0; row < 3; row++)
            {
                int width = Screen.Size.X - 2;
                int length = (int)((_clock * Speed + row * 7) % (width + 4));
                if (length > width) continue;
                DrawRect(new Rect2(origin + new Vector2(1, 1 + row * 2), new Vector2(length, 1)), Line);
            }
        }
    }
}
