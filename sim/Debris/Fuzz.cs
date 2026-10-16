namespace Stillwake.Sim.Debris;

public sealed partial class DebrisModule
{
    /// <summary>
    /// A grab or a hit, one in two each, at a piece that is out most of the time — a wreck or a small
    /// piece, whichever is drawn — else one already gone or never spawned. Most are refused, as few fuzzed
    /// heroes stand in space; those that are not take the piece, or chop at the wreck till it breaks.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var pieces = State.Pieces;
        bool hit = rng.Below(2) == 0;
        int piece = pieces.Count > 0 && rng.Below(4) != 0
            ? pieces[(int)rng.Below((uint)pieces.Count)].Id
            : (int)rng.Below((uint)State.NextId + 2);
        return hit ? new Hit(piece) { From = from } : new Grab(piece) { From = from };
    }
}
