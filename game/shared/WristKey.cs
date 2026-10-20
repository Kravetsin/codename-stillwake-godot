using System;
using Godot;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>How a wrist key stands: at rest, under the pointer, or held down.</summary>
public enum KeyState { Idle, Hover, Down }

/// <summary>
/// A hardware key of the wrist computer (0081): a housing of its scuffed metal at two screen pixels a pixel, lit from
/// above, round a rubber face that sinks while held. What is on the face is the key's own: the items' sort keys, the
/// logistics feature's drone key. It acts as it is pressed; its glyph glows a moment after it is let go.
/// </summary>
public abstract partial class WristKey : Control
{
    private const int U = WristPiece.Scale;
    private static readonly Color Out = new("0e1014"), Rubber = new("1f2229"), RubberTop = new("30343d"), RubberLow = new("17191e"),
        RubberHover = new("262b35"), Light = new("6a7385"), Side = new("5d6678"), Metal = new("4a5263"), Dark = new("3a404d"), Sunk = new("101318"),
        LitFace = new("0d2636"), LitDown = new("0a1d29"), LitTop = new("164158");

    private double _sinceUp = Look.KeyAfterS;

    protected WristKey(Vector2 size)
    {
        CustomMinimumSize = size;
        Size = size;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += () => Stand(State == KeyState.Down ? KeyState.Down : KeyState.Hover);
        MouseExited += () => Stand(KeyState.Idle);
        GuiInput += e =>
        {
            if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button) return;
            AcceptEvent();
            if (button.Pressed)
            {
                Stand(KeyState.Down);
                Pressed();
            }
            else if (State == KeyState.Down)
            {
                _sinceUp = 0;
                Stand(new Rect2(Vector2.Zero, Size).HasPoint(button.Position) ? KeyState.Hover : KeyState.Idle);
            }
        };
    }

    /// <summary>How it stands now.</summary>
    protected KeyState State { get; private set; }

    /// <summary>The glow its glyph keeps after it is let go: 1 then, 0 after <see cref="Look.KeyAfterS"/>.</summary>
    protected float After => (float)Math.Max(0, 1 - _sinceUp / Look.KeyAfterS);

    /// <summary>What a press does.</summary>
    protected abstract void Pressed();

    /// <summary>Draws what is on the face, inside <paramref name="face"/>; <see cref="Face"/> draws the rubber.</summary>
    protected abstract void DrawCap(Rect2 face);

    private void Stand(KeyState state)
    {
        State = state;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_sinceUp >= Look.KeyAfterS) return;
        _sinceUp += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var box = new Rect2(Vector2.Zero, Size);
        Housing(box);
        DrawCap(box.Grow(-2 * U));
    }

    /// <summary>The metal round the face: an outline with its corners cut, a bevel lit from above.</summary>
    private void Housing(Rect2 r)
    {
        float x = r.Position.X, y = r.Position.Y, w = r.Size.X, h = r.Size.Y;
        DrawRect(new Rect2(x + U, y, w - 2 * U, h), Out);
        DrawRect(new Rect2(x, y + U, w, h - 2 * U), Out);
        DrawRect(new Rect2(x + U, y + U, w - 2 * U, h - 2 * U), Metal);
        DrawRect(new Rect2(x + U, y + U, w - 2 * U, U), Light);
        DrawRect(new Rect2(x + U, y + 2 * U, U, h - 4 * U), Side);
        DrawRect(new Rect2(x + U, y + h - 2 * U, w - 2 * U, U), Dark);
        DrawRect(new Rect2(x + w - 2 * U, y + 2 * U, U, h - 4 * U), Dark);
    }

    /// <summary>The rubber face, sunk while held; <paramref name="lit"/> backlit in the screen's blue, as the drone key while drones serve.</summary>
    protected void Face(Rect2 f, bool lit = false)
    {
        bool down = State == KeyState.Down, hover = State == KeyState.Hover;
        DrawRect(f.Grow(1), Out);
        DrawRect(f, lit ? (down ? LitDown : LitFace) : down ? RubberLow : hover ? RubberHover : Rubber);
        DrawRect(f with { Size = new Vector2(f.Size.X, U) }, down ? Sunk : lit ? LitTop : hover ? Dark : RubberTop);
        DrawRect(new Rect2(f.Position.X, f.End.Y - U, f.Size.X, U), down ? RubberHover : RubberLow);
    }
}
