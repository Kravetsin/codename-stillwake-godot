using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The plan's hint, as picked (build-hints, the wing's column): a narrow hologram beside the build window under the layers' keys,
/// its glass and buzzing frame (holo windows), down to the window's foot, a line of <see cref="Hints"/> a row, wrapped and indented when long. The line
/// to build with flashes once as it comes; one short of its builder is red.
/// </summary>
public partial class HintWing : Control
{
    /// <summary>Its width in logical pixels: grown by the interface's size.</summary>
    public const float Width = 150;
    private const float Frame = 4, ScreenTop = 8, Pad = 4, First = 10, Between = 5, Indent = 10, FlashFor = 0.7f;
    private static readonly Color Ground = new Color(HoloLook.Drone.Ground) with { A = HoloLook.GroundAlpha }, Band = new(0.61f, 0.71f, 0.78f, 0.18f);

    private IReadOnlyList<KeyHint> _lines = [];
    private string _shown = "";
    private List<(List<HintPlaced> HintPlaced, HintTone Tone)> _laid = [];
    private double _time, _flash;
    private string? _flashed;

    /// <summary>How much larger than at the normal interface size it is drawn.</summary>
    public float Grown { get; set; } = 1;

    public HintWing()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(new BuzzFrame());
    }

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
        DrawRect(new Rect2(0, 0, w, h), Ground);
        var screen = new Rect2(Frame, ScreenTop, w - 2 * Frame, h - ScreenTop);
        float y = screen.Position.Y + First, flash = (float)(_flash / FlashFor);
        foreach (var (placed, tone) in _laid)
        {
            int rows = KeyCaps.RowsOf(placed);
            float lit = tone == HintTone.Plain ? 0 : flash;
            if (lit > 0) DrawRect(new Rect2(screen.Position.X, y - 9, screen.Size.X, rows * KeyCaps.Row + 5), new Color(Band, Band.A * lit));
            KeyCaps.Draw(this, placed, new Vector2(screen.Position.X + Pad, y), tone, _time, lit);
            y += Between + rows * KeyCaps.Row;
        }
    }
}
