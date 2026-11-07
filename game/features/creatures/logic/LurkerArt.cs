namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// What a cave lurker (2026-11-07-cave-hostiles) is drawn from: its pose for its rig (<see cref="LurkerRig"/>) and the colours it shines in.
/// Facing right, y down, its origin the middle of its feet. Plain C#, tested.
/// </summary>
public static class LurkerArt
{
    /// <summary>The pick page's colours: the garden's teal and the violet it flares to, a snail's amber, a hurt shell's cracks, an eye.</summary>
    public static readonly TickArt.Rgb Teal = new(86, 230, 205), Violet = new(180, 118, 255), Amber = new(255, 166, 62), Crack = new(255, 200, 120),
        Eye = new(255, 248, 232);

    /// <summary>
    /// What it is doing, the page's state, each 0..1 unless said: the capback hunkered still, alert, crouched, flown, sagging, squashed
    /// landing, flinched, its lean (radians) and its caps' shudder, its legs' walk and their stride (turns); the snail drawn into its shell,
    /// its eye stalks laid back, its plug shut, its shell's spin and rock (radians). How brightly it glows and flares, how lit its eye
    /// is, how its shell's cracks shine; the clock its sways keep, in milliseconds.
    /// </summary>
    public readonly record struct Pose(double Hunk = 0, double Alert = 0, double Crouch = 0, double Fly = 0, double Sag = 0, double Squash = 0, double Flinch = 0,
        double Lean = 0, double Shudder = 0, double Walk = 0, double Gait = 0, double Retract = 0, double Stalk = 0, double Lid = 0, double Spin = 0,
        double Rock = 0, double Glow = 1, double Flare = 0, double EyeLit = 0, double Crack = 0, double Clock = 0);

    /// <summary>A light it gives this frame, in its own pixels: its place, reach in pixels, colour and strength.</summary>
    public readonly record struct Light(double X, double Y, double R, TickArt.Rgb Colour, double K);

    /// <summary>The colour a light mask shines in.</summary>
    public static TickArt.Rgb Of(CreatureArt.LurkerShine shine) => shine switch
    {
        CreatureArt.LurkerShine.Teal => Teal,
        CreatureArt.LurkerShine.Violet => Violet,
        CreatureArt.LurkerShine.Crack => Crack,
        CreatureArt.LurkerShine.Eye => Eye,
        _ => Amber,
    };
}
