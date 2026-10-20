namespace Stillwake.Content;

/// <summary>
/// The helmet lamp (0077), worn on the head: a soft warm light round the wearer's head, seen by friends, for the dark of a
/// planet's caves, drawn by the client alone. Free: it draws no power, so a hero lost underground is never left in the
/// dark. Crafted at the lifepod.
/// </summary>
public static class HelmetLamp
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the helmet lamp.</summary>
    public static readonly string Item = "helmetLamp";
}
