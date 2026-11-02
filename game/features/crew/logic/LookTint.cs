namespace Stillwake.Game.Features.Crew;

/// <summary>
/// How a hi-res part takes a look's skin and hair (2026-10-28-looks-are-printed): smooth paint has no exact ramps to swap as the
/// pixels do (<see cref="SuitArt"/>). Skin is scaled, colour by colour, from its paint to the ramp chosen: the head, and the skin pictures under clothes. Hair takes the colour chosen at the tone it is painted, for its shades lean purple and would scale
/// to indigo; the brown it was drawn in stays as drawn.
/// </summary>
public readonly record struct LookTint(float R, float G, float B, LookTint.Paints Kind)
{
    public enum Paints
    {
        Hair,
        Skin,
    }

    /// <summary>The tone hair is painted in (the subject's <c>HAIR</c> base through the finish): what stands for the ramp's own colour.</summary>
    private const float HairTone = 0.33f;

    /// <summary>Skin's paint through the finish, the subject's <c>SK</c> base: lighter than any ramp's own.</summary>
    private const int SkinPaint = 0xe2c09f;

    /// <summary>How <paramref name="part"/> is tinted; none for a part that takes neither, or hair in the brown it is drawn in.</summary>
    public static LookTint? Of(string part, int skin, int hair)
    {
        if (part.StartsWith("hair_", System.StringComparison.Ordinal) || part.StartsWith("beard_", System.StringComparison.Ordinal))
            return hair == HeroLooks.HairDrawn ? null : new LookTint(Channel(HeroLooks.Hairs[hair][1], 16), Channel(HeroLooks.Hairs[hair][1], 8), Channel(HeroLooks.Hairs[hair][1], 0), Paints.Hair);
        if (part != "head" && !part.EndsWith("_skin", System.StringComparison.Ordinal)) return null;
        int to = HeroLooks.Skins[skin][1];
        return new LookTint(Channel(to, 16) / Channel(SkinPaint, 16), Channel(to, 8) / Channel(SkinPaint, 8), Channel(to, 0) / Channel(SkinPaint, 0), Paints.Skin);
    }

    /// <summary>A painted colour as this tint leaves it.</summary>
    public (float R, float G, float B) Apply(float r, float g, float b)
    {
        if (Kind == Paints.Hair)
        {
            float tone = (0.3f * r + 0.59f * g + 0.11f * b) / HairTone;
            return (R * tone, G * tone, B * tone);
        }
        return (r * R, g * G, b * B);
    }

    private static float Channel(int colour, int shift) => (colour >> shift & 0xff) / 255f;
}
