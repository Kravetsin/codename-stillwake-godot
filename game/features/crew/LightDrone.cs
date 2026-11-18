using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone (light-drone, a try): a stand-in orb floating by the hero's head in place of the helmet's lamp and
/// scanner, its lens turned where it shines. Drawn unlit, in the world's frame; its light is the head lamp's (<c>Sources.HeadLamp</c>).
/// </summary>
public partial class LightDrone : Node2D
{
    private static readonly Color Ink = new("15130f"), Lamp = new("ffd98a"), Scan = new("7fe0c8"), Core = new("fffbe8");
    private readonly DroneFloat _float = new();
    private Vector2 _toward = Vector2.Right;

    public LightDrone()
    {
        TopLevel = true;
        ZIndex = 1;
        Visible = false;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    /// <summary>Whether it scans rather than shines: the scanner worn, not the lamp.</summary>
    public bool Scanning { get; set; }

    /// <summary>Floats it after the neck at <paramref name="neck"/>, its lens turned <paramref name="toward"/>, a unit vector.</summary>
    public void Follow(Vector2 neck, int facing, Vector2 toward, double elapsedMs, double timeMs)
    {
        var (x, y) = _float.Follow((neck.X, neck.Y), facing, elapsedMs, timeMs);
        GlobalPosition = new Vector2((float)x, (float)y);
        _toward = toward;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var colour = Scanning ? Scan : Lamp;
        DrawCircle(Vector2.Zero, 2.6f, Ink);
        DrawCircle(Vector2.Zero, 1.9f, colour.Darkened(0.35f));
        DrawCircle(_toward * 0.8f, 1.1f, colour);
        DrawCircle(_toward * 1.1f, 0.5f, Core);
    }
}
