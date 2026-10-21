using System;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// How the arc makers' lamp hung aboard looks (0090), as their ceiling lamp does in the homes: its body, mount and dark globe,
/// always, the grid's tile lit by the world; while it shines its globe alight over it and the globe's glow added, both as bright
/// as it shines; and at full power the ceiling lamp's arc crackling across its globe.
/// </summary>
public static class ArcLampLooks
{
    public const string Kind = "arcLamp";

    /// <summary>Its pictures (<c>tools/homes-art/export.js</c>): the body; what of it is alight while lit; the glow, a cell's margin round it.</summary>
    public const string Body = "machine_arcLamp", Alight = "machine_arcLamp_lit", Glow = "machine_arcLamp_glow";

    /// <summary>How opaque each of its pictures is drawn, back to front: <see cref="Body"/>, <see cref="Alight"/> over it, <see cref="Glow"/> added.</summary>
    public readonly record struct Look(float Body, float Alight, float Glow);

    /// <summary>
    /// What is drawn of it, shining at <paramref name="shines"/> of its full light: the body whole whatever its power, the globe alight
    /// and its glow as bright as it shines, none of either while it is off, unpowered or dark in a flicker.
    /// </summary>
    public static Look Drawn(float shines)
    {
        float lit = Math.Clamp(shines, 0, 1);
        return new(1, lit, lit);
    }

    /// <summary>Whether its arc crackles, given its <paramref name="share"/> of the power it draws: only steady at full, as a home's lit lamp.</summary>
    public static bool Crackles(float share) => share >= 1;

    /// <summary>Where its globe's middle is in its picture, in pixels: the ceiling lamp's globe moved into one cell.</summary>
    public static readonly (float X, float Y) Globe = (8.5f, 10.5f);

    /// <summary>The ceiling lamp's arc (<see cref="HomesArt"/>), moved from its globe onto this one: from its picture's corner.</summary>
    public static readonly Emitter Arc = Moved(HomesArt.Of["ceilLamp"]);

    private static Emitter Moved(HomesArt.Picture ceilLamp)
    {
        var globe = ceilLamp.Lights[0];
        float dx = Globe.X - globe.X, dy = Globe.Y - globe.Y;
        var arc = ceilLamp.Emitters.First(e => e.Kind == Emits.Arc);
        return arc with { X = arc.X + dx, Y = arc.Y + dy, X2 = arc.X2 + dx, Y2 = arc.Y2 + dy };
    }
}
