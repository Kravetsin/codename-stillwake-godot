using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>A mouse button pressed or let go, and where in the world, in cells.</summary>
public readonly record struct ButtonEvent(bool Down, (double X, double Y) At);

/// <summary>
/// The mouse buttons as the world reads them (render/camera.ts's <c>take*</c>): the left and middle
/// buttons' presses and releases since the last take, in order, at their world points, and whether the
/// right one is held (0030: it works plans while held). Read from the buttons held after each event, so
/// a press or release of one button while another is held still counts. Only what began with a press on
/// the world is the world's: a drag begun on a panel crosses it unread.
/// </summary>
public sealed class Buttons
{
    /// <summary>The bits of the buttons held, as Godot's <c>MouseButtonMask</c> and the browser's <c>buttons</c>.</summary>
    public const int Left = 1, Right = 2, Middle = 4;

    private List<ButtonEvent> _left = [], _middle = [];
    private bool _leftDown, _rightDown, _middleDown, _ours, _clicked;

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
        _rightDown = (held & Right) != 0;
        bool middle = (held & Middle) != 0;
        if (middle != _middleDown)
        {
            _middleDown = middle;
            _middle.Add(new ButtonEvent(middle, at));
        }
        if (held == 0) _ours = false;
    }

    /// <summary>Whether the left button is held now, pressed on the world.</summary>
    public bool HoldsLeft => _leftDown;

    /// <summary>Whether the right button is held now, pressed on the world: the hero works the plans under the pointer.</summary>
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

    /// <summary>Left-button presses and releases since the last call, in order.</summary>
    public IReadOnlyList<ButtonEvent> TakeLeft()
    {
        var taken = _left;
        _left = [];
        return taken;
    }

    /// <summary>Middle-button presses and releases since the last call, as the left's: what removes.</summary>
    public IReadOnlyList<ButtonEvent> TakeMiddle()
    {
        var taken = _middle;
        _middle = [];
        return taken;
    }
}
