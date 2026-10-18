using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The sort button (0008): a small square with three bars, longest at the top, and an arrow down beside them, drawn
/// in code; lighter under the pointer. A click sorts; what it does is said in the one tooltip, set by its panel.
/// </summary>
public partial class SortButton : Control
{
    private static readonly Vector2 Side = new(22, 18);
    private static readonly StyleBoxFlat Idle = Style.Box(Style.Slate800, Style.Slate600, radius: 4);
    private static readonly StyleBoxFlat Lit = Style.Box(Style.Slate700, Style.Slate500, radius: 4);

    private readonly Action _pressed;
    private bool _hovered;

    public SortButton(Action pressed)
    {
        _pressed = pressed;
        CustomMinimumSize = Side;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += () => Hover(true);
        MouseExited += () => Hover(false);
        GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            AcceptEvent();
            _pressed();
        };
    }

    private void Hover(bool on)
    {
        _hovered = on;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawStyleBox(_hovered ? Lit : Idle, new Rect2(Vector2.Zero, Size));
        var ink = _hovered ? Style.Slate100 : Style.Slate300;
        // Three bars, each shorter: what a sort makes of a jumble.
        float left = 5, top = 5;
        for (int i = 0; i < 3; i++) DrawRect(new Rect2(left, top + 3 * i, 8 - 2 * i, 2), ink);
        // An arrow down at their right, from the first bar to below the last.
        float x = Size.X - 6;
        DrawRect(new Rect2(x - 0.5f, top, 1.5f, 7), ink);
        DrawColoredPolygon([new Vector2(x - 3, top + 6), new Vector2(x + 4, top + 6), new Vector2(x + 0.25f, top + 10)], ink);
    }
}
