using System;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>What a <see cref="PlanKey"/>'s face shows: a layer (0110) or a mode of the plan.</summary>
public enum KeyFace { Front, Back, Build, Wiring, Piping }

/// <summary>
/// A key on the build tablet's edge: a layer's on its right (0110), a mode's on its left (plan modes). A wrist key as the
/// sort keys are, the same housing and face, a crate header's square size, mounted on the frame as the bag's is on the
/// display's; its face a schematic of what it picks. The one picked is backlit, as a key that is on; the glyph lights sky
/// under the pointer and white while held.
/// </summary>
public partial class PlanKey : WristKey
{
    private static readonly Color Ink = new("8b98ad"), Hover = new("bae6fd"), Sky = new("38bdf8"), Held = new("7dd3fc");

    private readonly KeyFace _face;
    private readonly Action _pressed;
    private bool _on;

    public PlanKey(KeyFace face, Action pressed) : base(new Vector2(WristLook.KeycapSide, WristLook.KeycapSide))
    {
        _face = face;
        _pressed = pressed;
    }

    /// <summary>Whether what it picks is picked: its face backlit.</summary>
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
        switch (_face)
        {
            case KeyFace.Front: FrontIcon(icon, ink); break;
            case KeyFace.Back: BackIcon(icon, ink); break;
            case KeyFace.Build: BuildIcon(icon, ink); break;
            case KeyFace.Wiring: WiringIcon(icon, ink); break;
            default: PipingIcon(icon, ink); break;
        }
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

    /// <summary>Building: a wall of bricks in three courses, each laid half a brick over the one under it.</summary>
    private void BuildIcon(Rect2 icon, Color ink)
    {
        var o = icon.Position + new Vector2(0, 2);
        for (int row = 0; row < 3; row++)
        {
            float y = o.Y + row * 4, shift = row % 2 == 0 ? 0 : -3;
            for (float x = shift; x < icon.Size.X; x += 7)
            {
                float left = Mathf.Max(x, 0), right = Mathf.Min(x + 6, icon.Size.X);
                DrawRect(new Rect2(o.X + left, y, right - left, 3), ink);
            }
        }
    }

    /// <summary>Wiring: a lightning bolt.</summary>
    private void WiringIcon(Rect2 icon, Color ink)
    {
        var o = icon.Position;
        DrawColoredPolygon(
        [
            o + new Vector2(8, 0), o + new Vector2(3, 8), o + new Vector2(7, 8), o + new Vector2(5, 14),
            o + new Vector2(11, 5), o + new Vector2(7, 5), o + new Vector2(10, 0),
        ], ink);
    }

    /// <summary>Piping: a pipe's elbow, a flange at each end.</summary>
    private void PipingIcon(Rect2 icon, Color ink)
    {
        var o = icon.Position;
        DrawRect(new Rect2(o + new Vector2(2, 3), new Vector2(4, 9)), ink);
        DrawRect(new Rect2(o + new Vector2(2, 8), new Vector2(9, 4)), ink);
        DrawRect(new Rect2(o + new Vector2(1, 1), new Vector2(6, 2)), ink);
        DrawRect(new Rect2(o + new Vector2(11, 7), new Vector2(2, 6)), ink);
    }
}
