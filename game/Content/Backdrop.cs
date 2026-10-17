namespace Stillwake.Game;

/// <summary>
/// What fills space between the stars and the ship (render/README.md), drawn by the client only: far
/// wrecks, then nearer dust. Each lies on a plane of its own that follows <c>Parallax</c> of the
/// camera's movement and drifts left <c>Drift</c> of its pixels a second, a pixel of the plane
/// <c>zoom</c> pixels of the screen, so it moves at that share of the world at every zoom; it is
/// scattered over square tiles of the plane <c>Tile</c> pixels wide, <c>Mean</c> things a tile on
/// average.
/// </summary>
public static class Backdrop
{
    public const float WreckParallax = 0.3f, WreckDrift = 5f, WreckMean = 1f;
    public const int WreckTile = 560;

    /// <summary>A far wreck's size: pixels of its plane for a pixel of the debris pictures it is made of.</summary>
    public const float WreckScaleMin = 0.45f, WreckScaleMax = 0.85f;

    /// <summary>The most pieces of debris a wreck gathers round its core, a wreck's own picture.</summary>
    public const int WreckPiecesMost = 5;

    /// <summary>
    /// The colour a far wreck's lightest pixel takes: <see cref="WreckNear"/> of the way from space's own
    /// colour to <see cref="WreckTint"/> for the biggest, <see cref="WreckFar"/> for the smallest, its
    /// darkest pixels <see cref="WreckLift"/> of that. Grey, never lit, and drawn whole, so where pieces
    /// overlap they do not darken and the stars behind are hidden.
    /// </summary>
    public static readonly (float R, float G, float B) WreckTint = (0.2f, 0.23f, 0.3f);
    public const float WreckNear = 0.7f, WreckFar = 0.4f, WreckLift = 0.45f;

    public const float DustParallax = 0.6f, DustDrift = 11f, DustMean = 8f;
    public const int DustTile = 256;

    /// <summary>The share of specks two screen pixels wide rather than one.</summary>
    public const float DustBig = 0.2f;

    /// <summary>A speck's colour, and how much of it shows: from the faintest to the brightest.</summary>
    public static readonly (float R, float G, float B) DustColour = (0.78f, 0.8f, 0.86f);
    public const float DustAlphaMin = 0.2f, DustAlphaMax = 0.6f;
}
