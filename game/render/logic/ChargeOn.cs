namespace Stillwake.Game.Render;

/// <summary>
/// Which charge a struck blade in hand carries, and where on its picture (spear-effects, skins): a fitted arc module's, in its
/// crackle box, wins; else the hero's effect skin's, round its edge (<see cref="HeldLook.EdgeBox"/>); else none.
/// </summary>
public static class ChargeOn
{
    /// <summary>The charge on <paramref name="parts"/> with <paramref name="skin"/> the hero's effect skin's look, if any; none for none.</summary>
    public static (ChargeLook Look, (double X0, double Y0, double X1, double Y1) Box)? Of(HeldParts parts, ChargeLook? skin) =>
        parts.Crackles.Count > 0 ? (ChargeLook.Arc, parts.Crackles[0])
        : skin != null ? (skin, parts.Look.EdgeBox())
        : null;
}
