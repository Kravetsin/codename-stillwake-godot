namespace Stillwake.Sim.Debris;

public sealed partial class DebrisModule
{
    /// <summary>
    /// Grabs a piece that is out most of the time, else one already gone or never spawned. Most grabs are
    /// refused, as few fuzzed heroes stand in space; those that are not take the piece.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var pieces = State.Pieces;
        int piece = pieces.Count > 0 && rng.Below(4) != 0
            ? pieces[(int)rng.Below((uint)pieces.Count)].Id
            : (int)rng.Below((uint)State.NextId + 2);
        return new Grab(piece) { From = from };
    }
}
