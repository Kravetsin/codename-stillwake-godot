using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// A queued station's screen in its panel, 64 by 30 (the mockup's <c>.fscreen</c>): green lines running
/// across it while it works or waits full, READY while nothing is queued, dark while a unit waits for power.
/// The pod needs no power, so its screen is never dark.
/// </summary>
public partial class FabricatorScreen : Control
{
    /// <summary>A line every this many pixels, sliding one gap right in this many seconds.</summary>
    private const float Gap = 6, Period = 0.7f;

    private static readonly StyleBoxFlat Working = Style.Box(new Color("04130d"), new Color("14532d"), radius: 3);
    private static readonly StyleBoxFlat Idle = Style.Box(new Color("04130d"), Style.Slate800, radius: 3);
    private static readonly Color Line = Style.Green400 with { A = 0.6f }, ReadyText = new("22c55e");

    private Doing _doing;
    private double _clock;

    public FabricatorScreen()
    {
        CustomMinimumSize = new Vector2(64, 30);
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>What it shows.</summary>
    public Doing Doing
    {
        get => _doing;
        set
        {
            if (_doing == value) return;
            _doing = value;
            QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        if (_doing is Doing.Ready or Doing.Waiting or Doing.NoPower) return;
        _clock = (_clock + delta) % Period;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var all = new Rect2(Vector2.Zero, Size);
        if (_doing == Doing.NoPower)
        {
            DrawStyleBox(Idle, all);
            return;
        }
        if (_doing is Doing.Ready or Doing.Waiting)
        {
            DrawStyleBox(Idle, all);
            string ready = T("fabricator.screenReady");
            var font = Style.Mono;
            var size = font.GetStringSize(ready, HorizontalAlignment.Left, -1, 9);
            DrawString(font, new Vector2((Size.X - size.X) / 2, (Size.Y + size.Y) / 2 - 2), ready, HorizontalAlignment.Left, -1, 9, ReadyText);
            return;
        }
        DrawStyleBox(Working, all);
        float shift = (float)(_clock / Period) * Gap;
        for (float x = Gap - 1 + shift - Gap; x < Size.X; x += Gap)
            if (x >= 1) DrawRect(new Rect2(x, 1, 1, Size.Y - 2), Line);
    }
}
