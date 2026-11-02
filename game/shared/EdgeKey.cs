using System;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// A key on the tablet's top edge (<see cref="TabletEdge"/>) that shows a glyph of thin rectangles, a wrist key's face as the sort key's: sky under the pointer, white while
/// held, glowing a moment after; backlit and ringed sky while what it switches is on.
/// </summary>
public partial class EdgeKey : WristKey
{
    private const int U = WristPiece.Scale;
    /// <summary>The glyph's side, in screen pixels.</summary>
    private const float Side = 14;
    private static readonly Color Ink = new("8b98ad"), Afterglow = new("e0f2fe"), Hover = new("bae6fd"), Sky = new("38bdf8"), Held = new("7dd3fc");

    private readonly Rect2[] _glyph;
    private readonly Action _pressed;
    private readonly Func<bool> _on;
    private bool _wasOn;

    /// <param name="glyph">What is on its face, in a square of <see cref="Side"/> pixels.</param>
    /// <param name="pressed">What a press does.</param>
    /// <param name="on">Whether what it switches is on.</param>
    public EdgeKey(Rect2[] glyph, Action pressed, Func<bool> on) : base(new Vector2(WristLook.TabHeight, WristLook.TabHeight))
    {
        (_glyph, _pressed, _on) = (glyph, pressed, on);
    }

    protected override void Pressed() => _pressed();

    public override void _Process(double delta)
    {
        base._Process(delta);
        bool on = _on();
        if (on == _wasOn) return;
        _wasOn = on;
        QueueRedraw();
    }

    protected override void DrawCap(Rect2 face)
    {
        Face(face, lit: _wasOn);
        if (_wasOn)
        {
            DrawRect(face, Sky with { A = 0.14f });
            DrawRect(face.Grow(-U / 2f), Sky with { A = 0.85f }, false, U);
        }
        var (ink, glow, strength) = State switch
        {
            KeyState.Down => (Colors.White, Held, 1f),
            _ when _wasOn => (Colors.White, Sky, 0.6f),
            KeyState.Hover => (Hover, Sky, 0.6f),
            _ => (Ink.Lerp(Afterglow, After), Sky, After),
        };
        var at = face.Position + new Vector2(Mathf.Round((face.Size.X - Side) / 2), Mathf.Round((face.Size.Y - Side) / 2) + (State == KeyState.Down ? 1 : 0));
        if (strength > 0)
        {
            Glyph(at, glow with { A = 0.12f * strength }, 2);
            Glyph(at, glow with { A = 0.3f * strength }, 1);
        }
        Glyph(at, ink, 0);
    }

    private void Glyph(Vector2 at, Color ink, float grow)
    {
        foreach (var part in _glyph) DrawRect(new Rect2(at + part.Position, part.Size).Grow(grow), ink);
    }
}
