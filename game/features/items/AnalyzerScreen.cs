using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The analyzer's screen in its panel, 64 by 40: dark while it is empty or has no power, green lines
/// running down it while it works, a green check once what it holds is analysed.
/// </summary>
public partial class AnalyzerScreen : Control
{
    /// <summary>A line every this many pixels, sliding one gap down in this many seconds.</summary>
    private const float Gap = 6, Period = 0.9f;

    private static readonly StyleBoxFlat Dark = Style.Box(new Color("020409"), Style.Slate800, radius: 3);
    private static readonly StyleBoxFlat Lit = Style.Box(new Color("04130d"), new Color("14532d"), radius: 3);
    private static readonly Color Line = Style.Green400 with { A = 0.55f }, Glow = Style.Green400 with { A = 0.3f };
    /// <summary>The check's corners, from the screen's top-left.</summary>
    private static readonly Vector2[] Check = [new(24, 20), new(30, 26), new(41, 14)];

    private Stage _stage;
    private double _clock;

    public AnalyzerScreen()
    {
        CustomMinimumSize = new Vector2(64, 40);
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>What it shows.</summary>
    public Stage Stage
    {
        get => _stage;
        set
        {
            if (_stage == value) return;
            _stage = value;
            QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        if (_stage != Stage.Working) return;
        _clock = (_clock + delta) % Period;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var all = new Rect2(Vector2.Zero, Size);
        bool lit = _stage is Stage.Working or Stage.Done;
        DrawStyleBox(lit ? Lit : Dark, all);
        if (_stage == Stage.Working)
        {
            float shift = (float)(_clock / Period) * Gap;
            for (float y = Gap - 1 + shift - Gap; y < Size.Y; y += Gap)
                if (y >= 1) DrawRect(new Rect2(1, y, Size.X - 2, 1), Line);
        }
        if (_stage == Stage.Done)
        {
            DrawPolyline(Check, Glow, 7, true);
            DrawPolyline(Check, Style.Green400, 3, true);
        }
    }
}
