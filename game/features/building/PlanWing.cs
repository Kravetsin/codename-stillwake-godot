using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// A wing of the build tablet's metal bolted to one of its sides, its inner edge under the frame: the plan's keys are set
/// in it (<see cref="PlanKey"/>). Drawn as the frame's art is, a pixel of it <see cref="TabletLayout.ArtScale"/> of the
/// screen's, its outer corners cut and lit from above; behind the tablet, so the frame's corners clamp it.
/// </summary>
public partial class PlanWing : Control
{
    private const int U = TabletLayout.ArtScale;
    /// <summary>How far its inner edge reaches under the frame.</summary>
    public const int Tuck = 3 * U;
    /// <summary>Between its edge and its keys, and between two keys, in screen pixels.</summary>
    public const int Pad = 3 * U, Gap = 2 * U;

    private static readonly Color Out = new("15181e"), Light = new("6a7385"), Side = new("5d6678"), Metal = new("4a5263"),
        Dark = new("3a404d"), Scratch = new("262b35");

    private readonly bool _left;

    /// <param name="left">Whether it is bolted to the tablet's left side, its inner edge on its right.</param>
    public PlanWing(bool left)
    {
        _left = left;
        MouseFilter = MouseFilterEnum.Stop;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        float w = Size.X, h = Size.Y;
        // The outer side's corners are cut; the inner side runs on under the frame.
        float cutL = _left ? U : 0, cutR = _left ? 0 : U;
        DrawRect(new Rect2(cutL, 0, w - cutL - cutR, h), Out);
        DrawRect(new Rect2(0, U, w, h - 2 * U), Out);
        float x = _left ? U : 0, inner = w - U;
        DrawRect(new Rect2(x, U, inner, h - 2 * U), Metal);
        DrawRect(new Rect2(x, U, inner, U), Light);
        DrawRect(new Rect2(x, h - 2 * U, inner, U), Dark);
        DrawRect(new Rect2(_left ? U : w - 2 * U, 2 * U, U, h - 4 * U), _left ? Side : Dark);
        // A scuff down the metal, as the frame's sides have.
        DrawRect(new Rect2(_left ? 2 * U : w - 3 * U, Mathf.Round(h * 0.3f), U, Mathf.Round(h * 0.25f)), Scratch);
    }
}
