namespace Stillwake.Game.Shared;

/// <summary>Four distances in a piece's art, in art pixels: from each edge of its texture inwards.</summary>
public readonly record struct Bezel(int Left, int Top, int Right, int Bottom);

/// <summary>
/// One piece of the wrist computer's metal (0074): a 9-slice drawn at two screen pixels a pixel round a window, its
/// corners kept whole and its edges tiled, so a frame of any size is as crisp as another. <see cref="Window"/> is how
/// far the window lies inside the texture: the bezel and, outside it, <see cref="WristPieces.Pad"/> for the bumpers.
/// </summary>
/// <param name="Texture">The 9-slice's texture.</param>
/// <param name="Patch">The 9-slice's margins: its corners, taken whole.</param>
/// <param name="Window">Where the window lies inside the texture.</param>
/// <param name="Led">The status lamp's middle, from the texture's right edge and down from its top; none for a piece without one that glows.</param>
public sealed record WristPiece(string Texture, Bezel Patch, Bezel Window, (int FromRight, int Down)? Led = null)
{
    /// <summary>Screen pixels to an art pixel, as the build tablet's.</summary>
    public const int Scale = 2;

    /// <summary>
    /// The art round a window <paramref name="width"/> by <paramref name="height"/> screen pixels: its top-left in screen
    /// pixels from the window's, and its size in art pixels, an odd side rounded up so no bezel covers the window.
    /// </summary>
    public (int X, int Y, int Width, int Height) Around(int width, int height) =>
        (-Window.Left * Scale, -Window.Top * Scale,
            (width + Scale - 1) / Scale + Window.Left + Window.Right, (height + Scale - 1) / Scale + Window.Top + Window.Bottom);

    /// <summary>The screen row where the outline under a window whose foot is at <paramref name="foot"/> begins.</summary>
    public int OutlineBelow(int foot) => foot + Scale * (Window.Bottom - WristPieces.Pad - 1);

    /// <summary>The screen row where the outline over a window whose top is at <paramref name="top"/> begins.</summary>
    public int OutlineAbove(int top) => top - Scale * (Window.Top - WristPieces.Pad);
}

/// <summary>
/// The wrist computer's pieces (0074), exported from the concept's drawing code (inventory-look variant B) in the build
/// tablet's metal: the cuff round the hotbar and the gauges' tabs under it, always shown; the forearm display round
/// the bag, the open machine's module under it, and the equipment's unit.
/// </summary>
public static class WristPieces
{
    /// <summary>The transparent art pixel round every piece's shape, where the rubber bumpers stand out.</summary>
    public const int Pad = 1;

    /// <summary>Round the hotbar: a thin rim, two small bumpers on top, a dim status light.</summary>
    public static readonly WristPiece Cuff = new("res://art/wrist_cuff.png", new(7, 7, 12, 4), new(4, 4, 4, 4));

    /// <summary>A gauge's tab under the cuff, its top outline the cuff's bottom one; the suit's and the energy bar.</summary>
    public static readonly WristPiece Gauge = new("res://art/wrist_gauge.png", new(4, 3, 4, 4), new(4, 3, 4, 4));

    /// <summary>Round the bag, nothing open under it: bumpers, a warning stripe, screws, signal pips and a lamp.</summary>
    public static readonly WristPiece Display = new("res://art/wrist_display.png", new(54, 9, 34, 9), new(5, 6, 5, 5), (16, 4));

    /// <summary>Round the bag with a machine open under it: its bottom edge square, where the machine's module meets it.</summary>
    public static readonly WristPiece DisplayDocked = Display with { Texture = "res://art/wrist_display_docked.png", Patch = new(54, 9, 34, 4), Window = new(5, 6, 5, 4) };

    /// <summary>Round the open machine under the bag (the items' and the Fabricator's): square on top, bumpers below.</summary>
    public static readonly WristPiece Dock = new("res://art/wrist_dock.png", new(13, 3, 13, 9), new(5, 3, 5, 5));

    /// <summary>Round the equipment at the right edge.</summary>
    public static readonly WristPiece Body = new("res://art/wrist_body.png", new(13, 9, 19, 9), new(5, 6, 5, 5), (16, 4));
}
