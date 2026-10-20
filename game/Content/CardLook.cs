namespace Stillwake.Game;

/// <summary>
/// The pictures on the item cards (the item card mockup): how large a tool's held picture and a worn item's are drawn,
/// how large the effects round a tool are, and how a drill's card runs it — spun up, running, wound down as the hand's
/// does (<see cref="HeldLook"/>), at rest, over and over while the card shows.
/// </summary>
public static class CardLook
{
    /// <summary>A tool's held picture: screen pixels a pixel of it, whole so it stays crisp.</summary>
    public const int ToolScale = 6;

    /// <summary>A worn item's icon, and the helmet the helmet lamp is shown on, in screen pixels a pixel of them.</summary>
    public const int WornScale = 3, HelmetScale = 4;

    /// <summary>
    /// The effects round a tool: screen pixels a pixel of the world. Smaller than <see cref="ToolScale"/>, so sparks and
    /// arcs keep their size in the game against the picture's, and stay in its window.
    /// </summary>
    public const float EffectScale = 2;

    /// <summary>How long a drill's card runs it, then rests it once wound down, in milliseconds.</summary>
    public const double RunMs = 2500, RestMs = 500;

    /// <summary>The helmet lamp's motes drifting through its beam on its card: how many, and their speeds, screen pixels a second.</summary>
    public const int Motes = 16;
    public const float MoteSlowest = 2, MoteFastest = 7;

    /// <summary>A battery's glow on its card at full charge, and how far it pulses either way, a share of that.</summary>
    public const float ChargeGlow = 0.2f, ChargePulse = 0.05f;
}
