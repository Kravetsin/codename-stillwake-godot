using System.Collections.Generic;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The line beside the pointer (build-hints, <see cref="Hints.PointerOf"/>): one press and its words on a sliver of the wing's
/// screen in its metal, below and left of the pointer, clear of the cost tip on its right.
/// </summary>
public partial class HintChip : Control
{
    private const float Frame = 2, Pad = 4, Below = 14, Left = 10;
    private static readonly Color Metal = new("4d586d"), Glass = new("060c11");
    private List<HintPlaced> _placed = [];
    private string _shown = "";
    private double _time;

    /// <summary>How much larger than at the normal interface size it is drawn.</summary>
    public float Grown { get; set; } = 1;

    public HintChip() => MouseFilter = MouseFilterEnum.Ignore;

    /// <summary><paramref name="line"/> shown by the pointer at <paramref name="pointer"/> on the screen; none hides it.</summary>
    public void Show(KeyHint? line, Vector2 pointer)
    {
        Visible = line != null;
        if (line == null) return;
        if (line.Id != _shown)
        {
            _shown = line.Id;
            _placed = KeyCaps.Lay(line, float.MaxValue, 0);
        }
        float width = (_placed.Count == 0 ? 0 : _placed[^1].X + _placed[^1].Piece.Width) + 2 * (Frame + Pad);
        Size = new Vector2(width, KeyCaps.CapHeight + 2 * Frame + 2) * Grown;
        Position = new Vector2(Mathf.Round(pointer.X - Left * Grown - Size.X), Mathf.Round(pointer.Y + Below * Grown));
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * Grown);
        var size = Size / Grown;
        DrawRect(new Rect2(Vector2.Zero, size), Metal);
        DrawRect(new Rect2(Frame, Frame, size.X - 2 * Frame, size.Y - 2 * Frame), Glass);
        KeyCaps.Draw(this, _placed, new Vector2(Frame + Pad, size.Y / 2), HintTone.Plain, _time);
    }
}
