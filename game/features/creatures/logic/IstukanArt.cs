namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// What the Istukan («Истукан», candidate 4 of the guard page), the spire's guard, is drawn from: its pose and where that puts its
/// parts, for its rig (<see cref="IstukanRig"/>), and its lights. Facing right, y down, its origin the middle of its feet. Plain C#, tested.
/// </summary>
public static class IstukanArt
{
    /// <summary>The makers' light: a flight's streak, the core in its chest.</summary>
    public static readonly TickArt.Rgb Arc = new(112, 198, 255), Violet = new(180, 147, 255);

    /// <summary>
    /// What it is doing, the page's <c>P</c>, each 0..1 and mixed in this order: folded as a column, crouched, flown, stuck, dead. Its
    /// lamp's colour, none for dark glass, and brightness; how far its seams' light has risen; how far dimmed it is; a clock that
    /// shivers its arms while it lies stuck; whether it stands on a floor that hides what falls below its feet.
    /// </summary>
    public readonly record struct Pose(double Fold, double Crouch, double Fly, double Stuck, double Dead, TickArt.Rgb? Lamp, double LampAmp, double Seam,
        double Dim = 0, double Clock = 0, bool OnFloor = true);

    /// <summary>Where its parts are this frame, from the middle of its feet: its lamp's pupil, the violet core in its chest.</summary>
    public readonly record struct Anchors((double X, double Y) Eye, (double X, double Y) Core);
}
