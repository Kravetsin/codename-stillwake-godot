namespace Stillwake.Game.Features.Crew;

/// <summary>What an electric salvage tool's sounds do this frame: its run began or ended, and whether its wheel bites.</summary>
public readonly record struct RunCue(bool Starts, bool Stops, bool Bites);

/// <summary>
/// The grinder's and the plasma cutter's sounds over frames (salvage-tools): the run's start and end, which strike and cut off the
/// cutter's jet; and the grinder's bite as it comes on what it grinds, again every <c>biteEveryS</c> while it stays on.
/// </summary>
public struct RunSounds
{
    private bool _ran, _on;
    private double _sinceBiteS;

    /// <summary>One frame of <paramref name="elapsedS"/>: whether the tool <paramref name="runs"/>, and whether it is <paramref name="onPiece"/>.</summary>
    public RunCue Step(bool runs, bool onPiece, double elapsedS, double biteEveryS)
    {
        bool starts = runs && !_ran, stops = !runs && _ran, on = runs && onPiece, bites = false;
        _ran = runs;
        if (on)
        {
            _sinceBiteS += elapsedS;
            if (!_on || _sinceBiteS >= biteEveryS) (bites, _sinceBiteS) = (true, 0);
        }
        _on = on;
        return new(starts, stops, bites);
    }
}
