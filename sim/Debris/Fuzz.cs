namespace Stillwake.Sim.Debris;

public sealed partial class DebrisModule
{
    /// <summary>
    /// A grab, a hit or a reel, one in three each, at a piece that is out most of the time — a wreck or a
    /// small piece, whichever is drawn — else one already gone or never spawned. Most are refused, as few
    /// fuzzed heroes stand in space; those that are not take the piece, chop at the wreck till it breaks,
    /// or reel it to a point anywhere from a grid's size before the grid to one past it, so the clamp works.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var pieces = State.Pieces;
        uint kind = rng.Below(3);
        int piece = pieces.Count > 0 && rng.Below(4) != 0
            ? pieces[(int)rng.Below((uint)pieces.Count)].Id
            : (int)rng.Below((uint)State.NextId + 2);
        if (kind == 0) return new Hit(piece) { From = from };
        if (kind == 1) return new Grab(piece) { From = from };
        int width = _grid.Width * Unit, height = _grid.Height * Unit;
        int toX = (int)rng.Below((ulong)(3 * width)) - width, toY = (int)rng.Below((ulong)(3 * height)) - height;
        return new Reel(piece, toX, toY) { From = from };
    }
}
