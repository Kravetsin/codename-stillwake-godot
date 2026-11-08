namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Whether an aimed item's arm is up (aim-on-use): while it is used, the left button held or the item at work, and
/// <see cref="LingerMs"/> after, so a shot let go at the release leaves along the aim and quick clicks keep the arm raised.
/// Otherwise the item hangs and only the head follows the pointer.
/// </summary>
public struct ArmUp
{
    /// <summary>How long the arm stays up after the last use, in ms.</summary>
    public const double LingerMs = 450;

    /// <summary>How long the arm takes to come up to the aim, or go down from it, in ms; shots and hits wait till it is up.</summary>
    public const double RaiseMs = 90;

    private double? _sinceUseMs;

    /// <summary>Whether the arm is up <paramref name="elapsedMs"/> after the last frame, the item <paramref name="used"/> now or not.</summary>
    public bool Step(bool used, double elapsedMs)
    {
        if (used) _sinceUseMs = 0;
        else if (_sinceUseMs is double since) _sinceUseMs = since + elapsedMs;
        return _sinceUseMs is double ms && ms <= LingerMs;
    }
}
