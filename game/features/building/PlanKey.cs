using System;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>What a <see cref="PlanKey"/>'s face shows: a layer (0110) or a mode of the plan.</summary>
public enum KeyFace { Front, Back, Build, Wiring, Piping }

/// <summary>
/// A key of the build tablet, set in a <see cref="PlanWing"/> on its side: a layer's on its right (0110), a mode's on its
/// left (plan modes). A wrist key's rubber face sunk in a slot cut in the metal; its face a schematic of what it picks, at
/// the art's scale, and the keyboard's key engraved in its corner when it has one. The one picked is backlit, as a key
/// that is on, and ringed sky; the glyph lights pale sky under the pointer and white while held.
/// </summary>
public partial class PlanKey : WristKey
{
    private const int U = TabletLayout.ArtScale;
    /// <summary>The glyph's side in its own pixels, each drawn <see cref="U"/> screen pixels.</summary>
    private const float Side = 14;
    private static readonly Color Ink = new("8b98ad"), Hover = new("bae6fd"), Sky = new("38bdf8"), Held = new("7dd3fc"),
        Slot = new("101318"), Lip = new("5d6678");

    private readonly KeyFace _face;
    private readonly string? _letter;
    private readonly Action _pressed;
    private bool _on;

    /// <param name="letter">The keyboard's key that does the same, engraved in its corner; none for none.</param>
    public PlanKey(KeyFace face, Vector2 size, string? letter, Action pressed) : base(size)
    {
        _face = face;
        _letter = letter;
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

    /// <summary>The slot in the metal, lit on its lower lip, and the face sunk in it.</summary>
    public override void _Draw()
    {
        var box = new Rect2(Vector2.Zero, Size);
        DrawRect(box, Slot);
        DrawRect(new Rect2(0, Size.Y - U, Size.X, U), Lip);
        DrawCap(box.Grow(-U));
    }

    protected override void DrawCap(Rect2 face)
    {
        Face(face, lit: _on);
        // The one picked reads at a glance: its face washed sky and ringed by it.
        if (_on)
        {
            DrawRect(face, Sky with { A = 0.14f });
            DrawRect(face.Grow(-U / 2f), Sky with { A = 0.85f }, false, U);
        }
        var ink = State switch
        {
            KeyState.Down => Colors.White,
            _ when _on => Colors.White,
            KeyState.Hover => Hover,
            _ => Ink,
        };
        int down = State == KeyState.Down ? U : 0;
        if (_letter != null)
            DrawString(Render.Style.Mono, face.Position + new Vector2(2 * U, 2 * U + 9 + down), _letter, fontSize: 10,
                modulate: _on ? Held : Ink with { A = 0.7f });
        var at = new Vector2(Mathf.Round(face.Position.X + (face.Size.X - Side * U) / 2), Mathf.Round(face.Position.Y + (face.Size.Y - Side * U) / 2) + down);
        DrawSetTransform(at, 0, new Vector2(U, U));
        switch (_face)
        {
            case KeyFace.Front: FrontIcon(ink); break;
            case KeyFace.Back: BackIcon(ink); break;
            case KeyFace.Build: BuildIcon(ink); break;
            case KeyFace.Wiring: WiringIcon(ink); break;
            default: PipingIcon(ink); break;
        }
        DrawSetTransform(Vector2.Zero);
    }

    /// <summary>The front: a solid bright block before a faint grid.</summary>
    private void FrontIcon(Color ink)
    {
        var grid = ink with { A = 0.3f };
        float step = Side / 4;
        for (int i = 0; i <= 4; i++)
        {
            float d = Mathf.Min(Mathf.Round(i * step), Side - 1);
            DrawRect(new Rect2(d, 0, 1, Side), grid);
            DrawRect(new Rect2(0, d, Side, 1), grid);
        }
        var block = new Rect2(4, 4, 6, 6);
        DrawRect(block.Grow(1), Render.Style.Slate950);
        DrawRect(block, ink);
    }

    /// <summary>The back: a dim hatched block behind a dotted outline where the front would stand.</summary>
    private void BackIcon(Color ink)
    {
        const float side = 9;
        var dim = ink with { A = 0.55f };
        DrawRect(new Rect2(0, 0, side, side), dim, false, 1);
        for (float k = 3; k < 2 * side; k += 3)
            DrawLine(new Vector2(Mathf.Min(k, side), Mathf.Max(0, k - side)), new Vector2(Mathf.Max(0, k - side), Mathf.Min(k, side)), dim, 1);
        var front = new Vector2(5, 5);
        for (float d = 0; d < side; d += 3)
        {
            DrawRect(new Rect2(front + new Vector2(d, 0), new Vector2(2, 1)), ink);
            DrawRect(new Rect2(front + new Vector2(d, side - 1), new Vector2(2, 1)), ink);
            DrawRect(new Rect2(front + new Vector2(0, d), new Vector2(1, 2)), ink);
            DrawRect(new Rect2(front + new Vector2(side - 1, d), new Vector2(1, 2)), ink);
        }
    }

    /// <summary>Building: a wall of bricks in three courses, each laid half a brick over the one under it.</summary>
    private void BuildIcon(Color ink)
    {
        for (int row = 0; row < 3; row++)
        {
            float y = 2 + row * 4, shift = row % 2 == 0 ? 0 : -3;
            for (float x = shift; x < Side; x += 7)
            {
                float left = Mathf.Max(x, 0), right = Mathf.Min(x + 6, Side);
                DrawRect(new Rect2(left, y, right - left, 3), ink);
            }
        }
    }

    /// <summary>Wiring: a lightning bolt.</summary>
    private void WiringIcon(Color ink) => DrawColoredPolygon(
    [
        new(8, 0), new(3, 8), new(7, 8), new(5, 14), new(11, 5), new(7, 5), new(10, 0),
    ], ink);

    /// <summary>Piping: a pipe's elbow, a flange at each end.</summary>
    private void PipingIcon(Color ink)
    {
        DrawRect(new Rect2(2, 3, 4, 9), ink);
        DrawRect(new Rect2(2, 8, 9, 4), ink);
        DrawRect(new Rect2(1, 1, 6, 2), ink);
        DrawRect(new Rect2(11, 7, 2, 6), ink);
    }
}
