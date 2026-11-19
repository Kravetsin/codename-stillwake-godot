namespace Stillwake.Game;

/// <summary>
/// The pictures on the item cards (the item card mockup): how large a worn item's is drawn, the light drone's motes and a
/// battery's glow. A tool's picture is fitted by its own rule (<c>ToolPictureFit</c>).
/// </summary>
public static class CardLook
{
    /// <summary>A worn item's icon, and the light drone's, a small one, in screen pixels a pixel of it.</summary>
    public const int WornScale = 3, DroneScale = 5;

    /// <summary>The light drone's motes drifting through its beam on its card: how many, and their speeds, screen pixels a second.</summary>
    public const int Motes = 16;
    public const float MoteSlowest = 2, MoteFastest = 7;

    /// <summary>A battery's glow on its card at full charge, and how far it pulses either way, a share of that.</summary>
    public const float ChargeGlow = 0.2f, ChargePulse = 0.05f;
}
