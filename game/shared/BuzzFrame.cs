using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The buzzing frame round a hologram's glass, its parent control (holo windows): a machine window's (<see cref="HoloFrame"/>), in the
/// drone's light, added over what is under it; drawn again every frame it shows.
/// </summary>
public partial class BuzzFrame : Node2D
{
    private static readonly Color Light = new(HoloLook.Drone.Light);

    /// <summary>How strongly it shows: 0 not at all.</summary>
    public float Alpha { get; set; } = 1;

    public BuzzFrame() => Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

    public override void _Process(double delta)
    {
        if (IsVisibleInTree()) QueueRedraw();
    }

    public override void _Draw()
    {
        if (GetParent() is not Control glass || Alpha <= 0 || glass.Size.X < 2 || glass.Size.Y < 2) return;
        HoloFrame.Buzz(this, new HoloRect(0.5f, 0.5f, glass.Size.X - 1, glass.Size.Y - 1), Light, Time.GetTicksMsec() / 1000.0, Alpha);
    }
}
