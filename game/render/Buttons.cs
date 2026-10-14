using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>A mouse button pressed or let go, where in the world, in cells, and whether Shift was held then.</summary>
public readonly record struct ButtonEvent(bool Down, (double X, double Y) At, bool Shift);

/// <summary>
/// The mouse buttons as the world reads them (render/camera.ts's <c>take*</c>): presses and releases
/// since the last take, in order, at their world points. Read from the buttons held after each event,
/// so a press or release of one button while another is held still counts. Only what began with a
/// press on the world is the world's: a drag begun on a panel crosses it unread.
/// </summary>
public sealed class Buttons
{
    /// <summary>The bits of the buttons held, as Godot's <c>MouseButtonMask</c> and the browser's <c>buttons</c>.</summary>
    public const int Left = 1, Right = 2, Middle = 4;

    private List<ButtonEvent> _left = [], _right = [];
    private List<(double X, double Y)> _middle = [];
    private bool _leftDown, _rightDown, _middleDown, _ours;

    /// <summary>
    /// One pointer event: <paramref name="pressedOnWorld"/> when it is a press that reached the world
    /// (not a panel), <paramref name="held"/> the buttons held after it.
    /// </summary>
    public void OnPointer(bool pressedOnWorld, int held, (double X, double Y) at, bool shift)
    {
        if (pressedOnWorld) _ours = true;
        if (!_ours) return;
        bool left = (held & Left) != 0;
        if (left != _leftDown)
        {
            _leftDown = left;
            _left.Add(new ButtonEvent(left, at, shift));
        }
        bool right = (held & Right) != 0;
        if (right != _rightDown)
        {
            _rightDown = right;
            _right.Add(new ButtonEvent(right, at, shift));
        }
        bool middle = (held & Middle) != 0;
        if (middle && !_middleDown) _middle.Add(at);
        _middleDown = middle;
        if (held == 0) _ours = false;
    }

    /// <summary>Whether the left button is held now, pressed on the world.</summary>
    public bool HoldsLeft => _leftDown;

    /// <summary>Left-button presses and releases since the last call, in order.</summary>
    public IReadOnlyList<ButtonEvent> TakeLeft()
    {
        var taken = _left;
        _left = [];
        return taken;
    }

    /// <summary>Right-button presses and releases since the last call, as the left's.</summary>
    public IReadOnlyList<ButtonEvent> TakeRight()
    {
        var taken = _right;
        _right = [];
        return taken;
    }

    /// <summary>Middle-button presses since the last call, in order, at their world points.</summary>
    public IReadOnlyList<(double X, double Y)> TakeMiddle()
    {
        var taken = _middle;
        _middle = [];
        return taken;
    }
}
