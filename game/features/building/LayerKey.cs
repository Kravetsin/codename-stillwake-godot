using System;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// A layer's key on the build tablet's right edge (0110): a wrist key as the sort keys are, the same housing and face, a
/// crate header's square size, mounted on the frame as the bag's is on the display's; its face a schematic of its layer.
/// The focused layer's is backlit, as a key that is on; the glyph lights sky under the pointer and white while held.
/// </summary>
public partial class LayerKey : WristKey
{
    private static readonly Color Ink = new("8b98ad"), Hover = new("bae6fd"), Sky = new("38bdf8"), Held = new("7dd3fc");

    private readonly Focus _layer;
    private readonly Action _pressed;
    private bool _on;

    public LayerKey(Focus layer, Action pressed) : base(new Vector2(WristLook.KeycapSide, WristLook.KeycapSide))
    {
        _layer = layer;
        _pressed = pressed;
    }

    /// <summary>Whether its layer is the focused one: its face backlit.</summary>
    public bool On
    {
        get => _on;
        set
        {
            if (value == _on) return;
            _on = value;
            QueueRedraw();
        }
    }

    protected override void Pressed() => _pressed();

    protected override void DrawCap(Rect2 face)
    {
        Face(face, lit: _on);
        var (ink, glow, strength) = State switch
        {
            KeyState.Down => (Colors.White, Held, 1f),
            KeyState.Hover => (Hover, Sky, 0.6f),
            _ when _on => (Colors.White, Sky, 0.5f),
            _ => (Ink, Sky, After),
        };
        const float side = 14;
        var icon = new Rect2(Mathf.Round(face.Position.X + (face.Size.X - side) / 2),
            Mathf.Round(face.Position.Y + (face.Size.Y - side) / 2) + (State == KeyState.Down ? 1 : 0), side, side);
        if (strength > 0) DrawRect(icon.Grow(1), glow with { A = 0.12f * strength });
        if (_layer == Focus.Front) FrontIcon(icon, ink);
        else BackIcon(icon, ink);
    }

    /// <summary>The front: a solid bright block before a faint grid.</summary>
    private void FrontIcon(Rect2 icon, Color ink)
    {
        var grid = ink with { A = 0.3f };
        float step = icon.Size.X / 4;
        for (int i = 0; i <= 4; i++)
        {
            float d = Mathf.Round(i * step);
            DrawLine(icon.Position + new Vector2(d, 0), icon.Position + new Vector2(d, icon.Size.Y), grid);
            DrawLine(icon.Position + new Vector2(0, d), icon.Position + new Vector2(icon.Size.X, d), grid);
        }
        var block = new Rect2(icon.Position + new Vector2(4, 4), new Vector2(6, 6));
        DrawRect(block.Grow(1), Render.Style.Slate950);
        DrawRect(block, ink);
    }

    /// <summary>The back: a dim hatched block behind a dotted outline where the front would stand.</summary>
    private void BackIcon(Rect2 icon, Color ink)
    {
        const float side = 9;
        var dim = ink with { A = 0.55f };
        var o = icon.Position;
        DrawRect(new Rect2(o, new Vector2(side, side)), dim, false, 1);
        for (float k = 3; k < 2 * side; k += 3)
            DrawLine(o + new Vector2(Mathf.Min(k, side), Mathf.Max(0, k - side)), o + new Vector2(Mathf.Max(0, k - side), Mathf.Min(k, side)), dim);
        var front = o + new Vector2(5, 5);
        for (float d = 0; d < side; d += 3)
        {
            DrawRect(new Rect2(front + new Vector2(d, 0), new Vector2(2, 1)), ink);
            DrawRect(new Rect2(front + new Vector2(d, side - 1), new Vector2(2, 1)), ink);
            DrawRect(new Rect2(front + new Vector2(0, d), new Vector2(1, 2)), ink);
            DrawRect(new Rect2(front + new Vector2(side - 1, d), new Vector2(1, 2)), ink);
        }
    }
}
