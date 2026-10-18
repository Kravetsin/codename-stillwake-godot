using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What each analyzer's screen says without a word: on a powered network with nothing in it, or only what
/// there is nothing to learn from, a cursor blinking, waiting for something to analyse; lines running across it while it works; a green check
/// once what it holds is analysed; dark with no power. Drawn unlit over the screen of its picture, and
/// lighting the room round it a little in what it shows (a <see cref="Glow"/>), dark when it is.
/// </summary>
public partial class AnalyzerScreens : Node2D
{
    /// <summary>What the analyzers are doing is read this often; the lines run every frame.</summary>
    private const double RefreshMs = 100;
    /// <summary>The screen in the analyzer's picture, in pixels from its top-left (<c>machine_analyzer.png</c>).</summary>
    private static readonly Rect2I Screen = new(7, 5, 18, 7);
    /// <summary>Pixels a line of the running text grows a second.</summary>
    private const double Speed = 24;
    /// <summary>How often the waiting cursor blinks, a second.</summary>
    private const double Blinks = 1.6;
    /// <summary>How much of its colour the screen's light gives: waiting, working, done.</summary>
    private const float WaitingLight = 0.18f, WorkingLight = 0.55f, DoneLight = 0.5f;

    private static readonly Color Dark = new("10161d"), Line = new("9fd8ff"), Check = new("4ade80"), Dim = new("3a6a8a");
    /// <summary>A check two pixels thick, its pixels from the screen's top-left.</summary>
    private static readonly (int X, int Y)[] CheckPixels =
        [(11, 1), (12, 1), (10, 2), (11, 2), (5, 3), (6, 3), (9, 3), (10, 3), (6, 4), (7, 4), (8, 4), (9, 4), (7, 5), (8, 5)];

    private readonly IGridQueries _grid;
    private readonly IResearchQueries _research;
    private readonly IPowerQueries _power;
    private readonly List<(Vector2 Corner, Look Look)> _screens = [];
    private readonly List<Glow> _glows = [];
    private double _sinceRead = RefreshMs, _clock;

    private enum Look { Dark, Waiting, Working, Done }

    public AnalyzerScreens(IGridQueries grid, IResearchQueries research, IPowerQueries power)
    {
        _grid = grid;
        _research = research;
        _power = power;
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
        Light();
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
            // Holding what is in no cost, it waits as an empty one does: there is nothing to analyse.
            var look = at == null || at.Problem == "research.nothingToLearn" ? (Powered(m) ? Look.Waiting : Look.Dark)
                : at.Problem == "research.unpowered" ? Look.Dark
                : at.Problem == "research.nothingNew" ? Look.Done
                : Look.Working;
            _screens.Add((corner, look));
        }
    }

    /// <summary>Whether a network with something making power reaches the analyzer: an empty one draws nothing, so its share says nothing.</summary>
    private bool Powered(Machine m) =>
        _power.NodeAt(m.Cell) is int node && _power.NetworkOf(node) is { Supply: > 0 };

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
            glow.Colour = look switch
            {
                Look.Waiting => Line * (WaitingLight * (CursorOn() ? 1.3f : 0.8f)),
                // The lines typed so far: the screen brighter the more of it is lit.
                Look.Working => Line * (WorkingLight * (0.7f + 0.3f * Typed())),
                _ => Check * DoneLight,
            };
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
                    // A prompt mark and a cursor blinking after it: waiting for something to analyse.
                    DrawRect(new Rect2(origin + new Vector2(1, 1), new Vector2(1, 1)), Dim);
                    if (CursorOn()) DrawRect(new Rect2(origin + new Vector2(3, 1), new Vector2(2, 1)), Line);
                    break;
                case Look.Done:
                    foreach (var (x, y) in CheckPixels) DrawRect(new Rect2(origin + new Vector2(x, y), Vector2.One), Check);
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
