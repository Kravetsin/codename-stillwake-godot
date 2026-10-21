using System;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// How the arc makers' lamp hung aboard looks (0091): the lantern of its icon, hung by its handle from the top of its cell. Its
/// body, cage and cold glass always, the grid's tile lit by the world; while it shines its glass alight over it, drawn as the icon
/// draws it with no light or glow over it, and its glow round it, both as bright as it shines; and at full power the homes'
/// lantern's arc crackling across its glass.
/// </summary>
public static class ArcLampLooks
{
    public const string Kind = "arcLamp";

    /// <summary>Its pictures (<c>tools/homes-art/export.js</c>, from its icon): the body; its glass alight while lit; the glow, a cell's margin round it.</summary>
    public const string Body = "machine_arcLamp", Alight = "machine_arcLamp_lit", Glow = "machine_arcLamp_glow";

    /// <summary>How opaque each of its pictures is drawn, back to front: <see cref="Body"/>, <see cref="Alight"/> over it, <see cref="Glow"/> added.</summary>
    public readonly record struct Look(float Body, float Alight, float Glow);

    /// <summary>
    /// What is drawn of it, shining at <paramref name="shines"/> of its full light: the body whole whatever its power, the glass alight
    /// and its glow as bright as it shines, none of either while it is off, unpowered or dark in a flicker.
    /// </summary>
    public static Look Drawn(float shines)
    {
        float lit = Math.Clamp(shines, 0, 1);
        return new(1, lit, lit);
    }

    /// <summary>Whether its arc crackles, given its <paramref name="share"/> of the power it draws: only steady at full, as a home's lit lamp.</summary>
    public static bool Crackles(float share) => share >= 1;

    /// <summary>Where its glass's middle is in its picture, in pixels: the icon's lantern, its glass six pixels wide and seven tall.</summary>
    public static readonly (float X, float Y) Glass = (8, 7.5f);

    /// <summary>The homes' lantern's arc (<see cref="HomesArt"/>), moved from its glass onto this one's: from its picture's corner.</summary>
    public static readonly Emitter Arc = Moved(HomesArt.Of["lantern"]);

    private static Emitter Moved(HomesArt.Picture lantern)
    {
        var glass = lantern.Lights[0];
        float dx = Glass.X - glass.X, dy = Glass.Y - glass.Y;
        var arc = lantern.Emitters.First(e => e.Kind == Emits.Arc);
        return arc with { X = arc.X + dx, Y = arc.Y + dy, X2 = arc.X2 + dx, Y2 = arc.Y2 + dy };
    }
}
