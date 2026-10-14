using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The camera over the ship (render/camera.ts): it centres a world point, zooms in steps with
/// <c>-</c> and <c>=</c> within <see cref="ZoomMin"/>..<see cref="ZoomMax"/>, and answers which cells
/// are on screen and which cell the pointer is over. Until the hero is ported it is moved by hand: the
/// arrow keys and a middle-button drag pan it, the wheel zooms.
/// </summary>
public partial class ShipCamera : Camera2D
{
    /// <summary>Pixels per cell at zoom 1.</summary>
    public const int Cell = 16;

    /// <summary>The zoom a game starts at: the hero about 84 px tall.</summary>
    public const float StartZoom = 2;

    /// <summary>How far the player may zoom, Terraria's way: a little out, a little in. One key press is one step.</summary>
    public const float ZoomMin = 1, ZoomMax = 2.5f, ZoomStepSize = 0.25f;

    /// <summary>How far <see cref="Fit"/> may zoom out to frame a whole world in development.</summary>
    private const float FitMin = 0.1f;

    /// <summary>How fast the arrow keys pan, in screen pixels a second.</summary>
    private const float PanSpeed = 600;

    /// <summary>Whether the dev controls (arrows, wheel, middle drag) move the camera.</summary>
    public bool DevControls { get; set; } = true;

    private bool _dragging;
    // Made once: a string passed each frame would make a StringName each frame for the GC to finalize.
    private static readonly StringName Left = "ui_left", Right = "ui_right", Up = "ui_up", Down = "ui_down";

    public ShipCamera() => SetZoom(StartZoom);

    /// <summary>The zoom one step from <paramref name="zoom"/> toward <paramref name="direction"/> (±1), kept within bounds.</summary>
    public static float ZoomStep(float zoom, int direction) =>
        Mathf.Clamp(zoom + direction * ZoomStepSize, ZoomMin, ZoomMax);

    public float CurrentZoom => Zoom.X;

    public void SetZoom(float zoom) => Zoom = new Vector2(zoom, zoom);

    /// <summary>Centres a world point, in cells.</summary>
    public void Follow(Vector2 cells) => Position = cells * Cell;

    /// <summary>Frames a world rectangle, in cells, with two cells of margin, as far out as needed.</summary>
    public void Fit(Rect2 cells)
    {
        const int margin = 2;
        var screen = GetViewportRect().Size;
        float w = (cells.Size.X + margin * 2) * Cell, h = (cells.Size.Y + margin * 2) * Cell;
        SetZoom(Mathf.Clamp(Mathf.Min(screen.X / w, screen.Y / h), FitMin, ZoomMax));
        Follow(cells.GetCenter());
    }

    /// <summary>The part of the world on screen, in cells.</summary>
    public Rect2 View()
    {
        var size = GetViewportRect().Size / CurrentZoom / Cell;
        return new Rect2(GetScreenCenterPosition() / Cell - size / 2, size);
    }

    /// <summary>The world point under the mouse, in cells.</summary>
    public Vector2 Pointer() => GetGlobalMousePosition() / Cell;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true } key)
        {
            int direction = key.Keycode switch
            {
                Key.Minus or Key.KpSubtract => -1,
                Key.Equal or Key.KpAdd => 1,
                _ => 0,
            };
            if (direction != 0) SetZoom(ZoomStep(CurrentZoom, direction));
        }
        if (!DevControls) return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                SetZoom(ZoomStep(CurrentZoom, 1));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                SetZoom(ZoomStep(CurrentZoom, -1));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } button:
                _dragging = button.Pressed;
                break;
            case InputEventMouseMotion motion when _dragging:
                Position -= motion.Relative / CurrentZoom;
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (!DevControls) return;
        var pan = Input.GetVector(Left, Right, Up, Down);
        Position += pan * PanSpeed * (float)delta / CurrentZoom;
    }
}
