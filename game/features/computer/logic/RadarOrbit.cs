using System;
using Stillwake.Content;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// Where the radar draws a contact: on the ship's orbit, a circle round the planet's core through the ship, an offset of
/// <see cref="Contacts.Reach"/> lying <see cref="Span"/> of the screen's width from the ship along it, so the farthest stays on screen.
/// </summary>
public static class RadarOrbit
{
    /// <summary>The share of the screen's width a contact at the radar's edge lies from the ship, either side.</summary>
    public const float Span = 0.45f;

    /// <summary>
    /// Where a contact at <paramref name="offset"/> lies on a screen <paramref name="width"/> wide, the ship at
    /// (<paramref name="shipX"/>, <paramref name="shipY"/>) riding round a core <paramref name="radius"/> straight under it.
    /// </summary>
    public static (float X, float Y) At(int offset, float width, float shipX, float shipY, float radius)
    {
        float angle = offset / (float)Contacts.Reach * Span * width / radius;
        float coreY = shipY + radius;
        return (shipX + radius * MathF.Sin(angle), coreY - radius * MathF.Cos(angle));
    }

    /// <summary>How far, in kilometres, a contact at <paramref name="offset"/> lies from the ship, whichever side.</summary>
    public static int Km(int offset) => Math.Abs(offset) * Contacts.KmPerUnit;
}
