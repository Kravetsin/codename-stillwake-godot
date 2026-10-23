using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>A mouse button pressed or let go, and where in the world, in cells.</summary>
public readonly record struct ButtonEvent(bool Down, (double X, double Y) At);

/// <summary>
/// The mouse buttons as the world reads them (render/camera.ts's <c>take*</c>): the left and right buttons' presses
/// and releases since the last take, in order, at their world points, and whether each is held. The left button is
/// the front layer, the right the back (0107). Read from the buttons held after each event, so a press or release of
/// one button while another is held still counts. Only what began with a press on the world is the world's: a drag
/// begun on a panel crosses it unread.
/// </summary>
public sealed class Buttons
{
    /// <summary>The bits of the buttons held, as Godot's <c>MouseButtonMask</c> and the browser's <c>buttons</c>.</summary>
    public const int Left = 1, Right = 2;

    private List<ButtonEvent> _left = [], _right = [];
    private bool _leftDown, _rightDown, _ours, _clicked, _rightClicked;

    /// <summary>
    /// One pointer event: <paramref name="pressedOnWorld"/> when it is a press that reached the world
    /// (not a panel), <paramref name="held"/> the buttons held after it.
    /// </summary>
    public void OnPointer(bool pressedOnWorld, int held, (double X, double Y) at)
    {
        if (pressedOnWorld) _ours = true;
        if (!_ours) return;
        bool left = (held & Left) != 0;
        if (left != _leftDown)
        {
            _leftDown = left;
            _left.Add(new ButtonEvent(left, at));
            if (left) _clicked = true;
        }
        bool right = (held & Right) != 0;
        if (right != _rightDown)
        {
            _rightDown = right;
            _right.Add(new ButtonEvent(right, at));
            if (right) _rightClicked = true;
        }
        if ((held & (Left | Right)) == 0) _ours = false;
    }

    /// <summary>Whether the left button is held now, pressed on the world.</summary>
    public bool HoldsLeft => _leftDown;

    /// <summary>Whether the right button is held now, pressed on the world: what the hands do, they do to the back layer.</summary>
    public bool HoldsRight => _rightDown;

    /// <summary>
    /// Whether the left button was pressed on the world since the last call: the hero's hands read it
    /// (crew/keys.ts's <c>takeClick</c>) apart from the presses <see cref="TakeLeft"/> hands the card.
    /// </summary>
    public bool TakeClick()
    {
        bool clicked = _clicked;
        _clicked = false;
        return clicked;
    }

    /// <summary>Whether the right button was pressed on the world since the last call: a canister in hand is breathed (0047), an axe swung at the back.</summary>
    public bool TakeRightClick()
    {
        bool clicked = _rightClicked;
        _rightClicked = false;
        return clicked;
    }

    /// <summary>Left-button presses and releases since the last call, in order.</summary>
    public IReadOnlyList<ButtonEvent> TakeLeft()
    {
        var taken = _left;
        _left = [];
        return taken;
    }

    /// <summary>Right-button presses and releases since the last call, as the left's: the card's back layer.</summary>
    public IReadOnlyList<ButtonEvent> TakeRight()
    {
        var taken = _right;
        _right = [];
        return taken;
    }
}

/// <summary>Which layer the buttons work (0107): the left the front, the right the back, everywhere.</summary>
public static class Layering
{
    /// <summary>
    /// Whether what the hands do with <paramref name="left"/> and <paramref name="right"/> held goes to the back layer: the
    /// right alone. Both held, the front keeps them.
    /// </summary>
    public static bool Back(bool left, bool right) => right && !left;
}
