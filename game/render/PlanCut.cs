namespace Stillwake.Game.Render;

/// <summary>
/// Where a plan being built stops being a plan (0063): on one side of <see cref="Line"/>, in pixels of the world,
/// it is drawn as the real thing, on the other still as its ghost. What stands turns real from the bottom up; a
/// floor or a conduit, thin, from the left.
/// </summary>
/// <param name="Line">A row of pixels for what turns real bottom-up, real from it down; a column for what does so from the left, real left of it.</param>
public readonly record struct PlanCut(int Line, bool FromLeft)
{
    /// <summary>
    /// The cut of a plan over <paramref name="box"/> (x, y, w, h, in pixels) with <paramref name="done"/> of its
    /// <paramref name="all"/> ticks of work done: that share of the box, rounded down to a pixel, so it moves a row
    /// or a column of the art at a time.
    /// </summary>
    public static PlanCut Of((int X, int Y, int W, int H) box, bool fromLeft, int done, int all) =>
        fromLeft ? new(box.X + box.W * done / all, true) : new(box.Y + box.H - box.H * done / all, false);

    /// <summary>Whether a placement of <paramref name="kind"/> on <paramref name="layer"/> turns real from the left, as floors do.</summary>
    public static bool FromLeftFor(string layer, string kind) => kind == "floor";

    /// <summary>
    /// <paramref name="rect"/> (x, y, w, h) split at the cut: the part drawn real and the part still a plan, either
    /// of them empty (no width or height) when the cut misses the rectangle.
    /// </summary>
    public ((int X, int Y, int W, int H) Real, (int X, int Y, int W, int H) Plan) Split((int X, int Y, int W, int H) rect)
    {
        if (FromLeft)
        {
            int x = System.Math.Clamp(Line, rect.X, rect.X + rect.W);
            return ((rect.X, rect.Y, x - rect.X, rect.H), (x, rect.Y, rect.X + rect.W - x, rect.H));
        }
        int y = System.Math.Clamp(Line, rect.Y, rect.Y + rect.H);
        return ((rect.X, y, rect.W, rect.Y + rect.H - y), (rect.X, rect.Y, rect.W, y - rect.Y));
    }
}
