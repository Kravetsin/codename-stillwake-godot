namespace Stillwake.Game;

/// <summary>
/// The pictures on the item cards (the item card mockup): how large a worn item's is drawn, the helmet lamp's motes and a
/// battery's glow. A tool's card has no picture: its icon in the slot shows it (2026-10-27-tool-icons).
/// </summary>
public static class CardLook
{
    /// <summary>A worn item's icon, and the helmet the helmet lamp is shown on, in screen pixels a pixel of them.</summary>
    public const int WornScale = 3, HelmetScale = 4;

    /// <summary>The helmet lamp's motes drifting through its beam on its card: how many, and their speeds, screen pixels a second.</summary>
    public const int Motes = 16;
    public const float MoteSlowest = 2, MoteFastest = 7;

    /// <summary>A battery's glow on its card at full charge, and how far it pulses either way, a share of that.</summary>
    public const float ChargeGlow = 0.2f, ChargePulse = 0.05f;
}
