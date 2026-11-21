using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// A small hologram beside the build window, the plan's keys set in it (<see cref="PlanKey"/>): the window's glass and buzzing
/// frame (holo windows), a pixel of its spacing <see cref="TabletLayout.ArtScale"/> of the screen's.
/// </summary>
public partial class PlanWing : Control
{
    private const int U = TabletLayout.ArtScale;
    /// <summary>Between its edge and its keys, and between two keys, in screen pixels.</summary>
    public const int Pad = 3 * U, Gap = 2 * U;

    private static readonly Color Ground = new Color(HoloLook.Drone.Ground) with { A = HoloLook.GroundAlpha };

    public PlanWing()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Resized += QueueRedraw;
        AddChild(new BuzzFrame());
    }

    public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), Ground);
}
