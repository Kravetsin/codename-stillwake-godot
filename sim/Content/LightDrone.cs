namespace Stillwake.Content;

/// <summary>
/// The light drone (light-drone), worn in the helmet's mount slot (2026-10-27-suits-are-worn): a small drone floating by the
/// wearer's head, its beam where the pointer points, seen by friends, for the dark of a planet's caves, drawn by the client
/// alone. Free: it draws no power, so a hero lost underground is never left in the dark. Crafted at the workbench.
/// </summary>
public static class LightDrone
{
    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the light drone.</summary>
    public static readonly string Item = "lightDrone";

    /// <summary>
    /// The node that opens the scanner mode (2026-11-07-the-scanner, light-drone): bought once on the black market, known crew-wide;
    /// in that mode the drone's light is out and a wreck's hit points show under the pointer.
    /// </summary>
    public static readonly string ScannerMode = "scanner";
}
