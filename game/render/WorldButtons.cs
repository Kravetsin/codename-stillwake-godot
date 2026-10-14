using System;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The mouse over the world (render/camera.ts's pointer and buttons): feeds <see cref="Buttons"/> from
/// Godot's input, a press only when no panel took it, and says where the pointer is in cells. A
/// scripted run sets <see cref="Script"/> to point and press instead of the mouse.
/// </summary>
public partial class WorldButtons : Node2D
{
    public Buttons Buttons { get; } = new();

    /// <summary>Where a script points, in cells, in place of the mouse; null to read the mouse.</summary>
    public Func<(double X, double Y)?>? Script { get; set; }

    /// <summary>The world point under the pointer, in cells; null while the mouse is off the window.</summary>
    public (double X, double Y)? Pointer()
    {
        if (Script != null) return Script();
        var viewport = GetViewport();
        if (!viewport.GetVisibleRect().HasPoint(viewport.GetMousePosition())) return null;
        var at = GetGlobalMousePosition() / ShipCamera.Cell;
        return (at.X, at.Y);
    }

    private (double X, double Y) WorldOf(Vector2 screen)
    {
        var at = GetCanvasTransform().AffineInverse() * screen / ShipCamera.Cell;
        return (at.X, at.Y);
    }

    private static bool Ours(MouseButton b) => b is MouseButton.Left or MouseButton.Right or MouseButton.Middle;

    // Releases and moves are read before any panel, so a drag begun on the world and let go over a panel still ends.
    public override void _Input(InputEvent e)
    {
        if (Script != null) return;
        if (e is InputEventMouseButton { Pressed: false } up && Ours(up.ButtonIndex))
            Buttons.OnPointer(false, (int)up.ButtonMask, WorldOf(up.Position), up.ShiftPressed);
        else if (e is InputEventMouseMotion move)
            Buttons.OnPointer(false, (int)move.ButtonMask, WorldOf(move.Position), move.ShiftPressed);
    }

    // A press counts only when no panel took it.
    public override void _UnhandledInput(InputEvent e)
    {
        if (Script != null) return;
        if (e is InputEventMouseButton { Pressed: true } down && Ours(down.ButtonIndex))
            Buttons.OnPointer(true, (int)down.ButtonMask, WorldOf(down.Position), down.ShiftPressed);
    }
}
