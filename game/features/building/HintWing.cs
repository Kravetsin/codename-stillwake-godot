using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The plan's hint, as picked (build-hints, the wing's column): a narrow phosphor screen bolted to the tablet's right side under
/// the layers' keys, down to the screen's foot, a line of <see cref="Hints"/> a row, wrapped and indented when long. The line
/// to build with flashes once as it comes; one short of its builder is red.
/// </summary>
public partial class HintWing : Control
{
    /// <summary>Its width and the least height it is drawn at, in logical pixels: grown by the interface's size.</summary>
    public const float Width = 150, Least = 121;
    private const float Frame = 4, ScreenTop = 8, Pad = 4, First = 10, Between = 5, Indent = 10, FlashFor = 0.7f;
    private static readonly Color Back = new("171d25"), Metal = new("4d586d"), Lit = new("6c788c"), Shade = new("3c4554"), Bolt = new("8a95a8"),
        BoltHole = new("2a313c"), Mount = new("25303d"), Glass = new("05100f"), Scan = new(0, 0, 0, 0.22f), Band = new(0.61f, 0.71f, 0.78f, 0.18f);
    private static readonly GradientTexture2D Vignette = new()
    {
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(1.2f, 0.5f),
        Gradient = new Gradient { Colors = [new Color(0.47f, 0.67f, 0.75f, 0.06f), new Color(0, 0, 0, 0.25f)], Offsets = [0, 1] },
    };

    private IReadOnlyList<KeyHint> _lines = [];
    private string _shown = "";
    private List<(List<HintPlaced> HintPlaced, HintTone Tone)> _laid = [];
    private double _time, _flash;
    private string? _flashed;

    /// <summary>How much larger than at the normal interface size it is drawn.</summary>
    public float Grown { get; set; } = 1;

    public HintWing() => MouseFilter = MouseFilterEnum.Ignore;

    /// <summary><paramref name="lines"/> shown from the next frame: laid again only when they change.</summary>
    public void Show(IReadOnlyList<KeyHint> lines)
    {
        string id = string.Join("|", lines.Select(l => l.Id));
        if (id == _shown) return;
        _shown = id;
        _lines = lines;
        // The line to build with flashes as it comes, and again as it turns red.
        var main = lines.FirstOrDefault(l => l.Says is "hint.build" or "hint.noBuilder");
        if (main?.Id != _flashed && main != null) _flash = FlashFor;
        _flashed = main?.Id;
        _laid = lines.Select(l => (KeyCaps.Lay(l, Width - 2 * Frame - 2 * Pad, Indent), ToneOf(l))).ToList();
    }

    private static HintTone ToneOf(KeyHint line) => line.Short ? HintTone.Short : line.Says == "hint.build" ? HintTone.Main : HintTone.Plain;

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _time += delta;
        _flash = System.Math.Max(0, _flash - delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * Grown);
        float w = Width, h = Size.Y / Grown;
        DrawRect(new Rect2(0, 0, w, h), Back);
        DrawRect(new Rect2(1, 1, w - 2, h - 1), Metal);
        DrawRect(new Rect2(1, 1, w - 2, 1), Lit);
        DrawRect(new Rect2(1, 1, 1, h - 1), Shade);
        foreach (float x in (float[])[4, w - 5])
        {
            DrawRect(new Rect2(x - 1, 3, 3, 3), BoltHole);
            DrawRect(new Rect2(x, 4, 1, 1), Bolt);
        }
        // Two brackets up to the layers' keys above.
        DrawRect(new Rect2(22, -5, 10, 6), Mount);
        DrawRect(new Rect2(36, -5, 10, 6), Mount);
        var screen = new Rect2(Frame, ScreenTop, w - 2 * Frame, h - ScreenTop);
        DrawRect(screen, Glass);
        DrawTextureRect(Vignette, screen, false);
        float y = screen.Position.Y + First, flash = (float)(_flash / FlashFor);
        foreach (var (placed, tone) in _laid)
        {
            int rows = KeyCaps.RowsOf(placed);
            float lit = tone == HintTone.Plain ? 0 : flash;
            if (lit > 0) DrawRect(new Rect2(screen.Position.X, y - 9, screen.Size.X, rows * KeyCaps.Row + 5), new Color(Band, Band.A * lit));
            KeyCaps.Draw(this, placed, new Vector2(screen.Position.X + Pad, y), tone, _time, lit);
            y += Between + rows * KeyCaps.Row;
        }
        for (float row = screen.Position.Y; row < screen.End.Y; row += 2) DrawRect(new Rect2(screen.Position.X, row, screen.Size.X, 1), Scan);
    }
}
