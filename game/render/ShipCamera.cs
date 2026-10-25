using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The camera over the ship (render/camera.ts): it centres a world point, zooms in steps with
/// <c>-</c> and <c>=</c> or Ctrl and the wheel from the farthest, <see cref="Farthest"/> on any screen, to twice as near, and answers which cells
/// are on screen and which cell the pointer is over. Until the hero is ported it is moved by hand: the
/// arrow keys and a middle-button drag pan it, the wheel zooms.
/// </summary>
public partial class ShipCamera : Camera2D
{
    /// <summary>Pixels per cell at zoom 1: 8 (0104), a map's character two by two of them.</summary>
    public const int Cell = Stillwake.Game.Camera.Cell;

    /// <summary>
    /// The most of the world a screen shows, in cells, at the farthest zoom, where a game starts: the same
    /// for every player whatever their screen, as Terraria keeps it; a bigger screen draws it larger. Within
    /// the debris' <c>Debris.View</c> both ways, so no piece appears or vanishes in sight (0052).
    /// </summary>
    public static readonly Vector2 Farthest = new(192, 108);

    /// <summary>How near the player may zoom, as a multiple of the farthest, and one key press's step.</summary>
    public const float LevelMax = 2, LevelStep = 0.2f;

    /// <summary>How far <see cref="Fit"/> may zoom out to frame a whole world in development.</summary>
    private const float FitMin = 0.1f;

    /// <summary>How fast the arrow keys pan, in screen pixels a second.</summary>
    private const float PanSpeed = 600;

    /// <summary>Whether the dev controls (arrows, wheel, middle drag) move the camera.</summary>
    public bool DevControls { get; set; } = true;

    private bool _dragging;
    private float _level = 1;
    // A zoom set outright, by a dev view or Fit, rather than the player's level.
    private float? _fixed;
    // Made once: a string passed each frame would make a StringName each frame for the GC to finalize.
    private static readonly StringName Left = "ui_left", Right = "ui_right", Up = "ui_up", Down = "ui_down";

    /// <summary>The farthest zoom on a screen <paramref name="screen"/> pixels big: what shows at most <see cref="Farthest"/>.</summary>
    public static float FarthestZoom(Vector2 screen) => Mathf.Max(screen.X / (Farthest.X * Cell), screen.Y / (Farthest.Y * Cell));

    public float CurrentZoom => Zoom.X;

    /// <summary>Zooms to exactly <paramref name="zoom"/>, for dev views; the player's next step goes back to their level.</summary>
    public void SetZoom(float zoom)
    {
        _fixed = zoom;
        Zoom = new Vector2(zoom, zoom);
    }

    public override void _Ready()
    {
        Apply();
        GetViewport().SizeChanged += Apply;
    }

    private void Step(int direction)
    {
        _fixed = null;
        _level = Mathf.Clamp(_level + direction * LevelStep, 1, LevelMax);
        Apply();
    }

    private void Apply()
    {
        float zoom = _fixed ?? FarthestZoom(GetViewportRect().Size) * _level;
        Zoom = new Vector2(zoom, zoom);
    }

    /// <summary>Centres a world point, in cells.</summary>
    public void Follow(Vector2 cells) => Position = cells * Cell;

    /// <summary>Frames a world rectangle, in cells, with four cells of margin, as far out as needed.</summary>
    public void Fit(Rect2 cells)
    {
        const int margin = 4;
        var screen = GetViewportRect().Size;
        float w = (cells.Size.X + margin * 2) * Cell, h = (cells.Size.Y + margin * 2) * Cell;
        SetZoom(Mathf.Clamp(Mathf.Min(screen.X / w, screen.Y / h), FitMin, FarthestZoom(screen) * LevelMax));
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
            if (direction != 0) Step(direction);
        }
        // Ctrl and the wheel zoom in play too; the hotbar leaves the wheel to it while Ctrl is held.
        if (e is InputEventMouseButton { Pressed: true, CtrlPressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel)
        {
            Step(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!DevControls) return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                Step(1);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                Step(-1);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } button:
                _dragging = button.Pressed;
                break;
            case InputEventMouseMotion motion when _dragging:
                Position -= motion.Relative / CurrentZoom;
                break;
        }
    }

    /// <summary>
    /// A hit's weight (0075): the view knocked along <paramref name="away"/> by a pixel of the world and back, over
    /// <see cref="NudgeSteps"/>' 60 Hz frames, the other way each, smaller after the first two.
    /// </summary>
    public void Nudge(Vector2 away)
    {
        _nudgeMs = 0;
        _nudgeWay = Mathf.Abs(away.X) >= Mathf.Abs(away.Y) ? new Vector2(Mathf.Sign(away.X), 0) : new Vector2(0, Mathf.Sign(away.Y));
        if (_nudgeWay == Vector2.Zero) _nudgeWay = Vector2.Right;
    }

    /// <summary>How far the view is knocked on each 60 Hz frame of a nudge, in pixels of the world, along its way and back.</summary>
    private static readonly float[] NudgeSteps = [0.75f, -0.75f, 0.4f, -0.4f];
    private double _nudgeMs = double.MaxValue;
    private Vector2 _nudgeWay;

    public override void _Process(double delta)
    {
        if (_nudgeMs < NudgeSteps.Length * 1000.0 / 60)
        {
            Offset = _nudgeWay * NudgeSteps[(int)(_nudgeMs * 60 / 1000)];
            _nudgeMs += delta * 1000;
        }
        else Offset = Vector2.Zero;
        if (!DevControls) return;
        var pan = Input.GetVector(Left, Right, Up, Down);
        Position += pan * PanSpeed * (float)delta / CurrentZoom;
    }
}
