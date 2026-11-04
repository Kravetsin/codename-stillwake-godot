namespace Stillwake.Game.Features.Logistics;

/// <summary>How a salvage drone's net is drawn: open, half shut as it takes a load, shut round it.</summary>
public enum GripPose { Open, Mid, Shut }

/// <summary>
/// A salvage drone's net (salvage-drones), apart from Godot: open while it holds nothing; when a load appears, half shut for
/// <see cref="SalvageLook.GripMidS"/>, then shut; open again once the load is gone. One met already holding a load is shut.
/// </summary>
public sealed class Grip(bool held)
{
    private bool _held = held;
    private double _mid;

    public GripPose Pose { get; private set; } = held ? GripPose.Shut : GripPose.Open;

    /// <summary><paramref name="dt"/> seconds on, holding a load or not.</summary>
    public void Step(double dt, bool held)
    {
        if (held && !_held) _mid = SalvageLook.GripMidS;
        _held = held;
        if (!held) Pose = GripPose.Open;
        else if (_mid > 0)
        {
            _mid -= dt;
            Pose = GripPose.Mid;
        }
        else Pose = GripPose.Shut;
    }
}
