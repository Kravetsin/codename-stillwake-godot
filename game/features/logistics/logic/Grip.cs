namespace Stillwake.Game.Features.Logistics;

/// <summary>How a salvage drone's net is drawn: open, half shut as it takes a load, shut round it.</summary>
public enum GripPose { Open, Mid, Shut }

/// <summary>
/// A salvage drone's net (salvage-drones, salvage-trips), apart from Godot: open while it holds nothing; as each catch comes
/// into it, half shut for <see cref="SalvageLook.GripMidS"/>, then shut; open again once the load is gone. One met already
/// holding a load is shut.
/// </summary>
public sealed class Grip(int held)
{
    private int _held = held;
    private double _mid;

    public GripPose Pose { get; private set; } = held > 0 ? GripPose.Shut : GripPose.Open;

    /// <summary><paramref name="dt"/> seconds on, holding <paramref name="held"/> catches.</summary>
    public void Step(double dt, int held)
    {
        if (held > _held) _mid = SalvageLook.GripMidS;
        _held = held;
        if (held == 0) Pose = GripPose.Open;
        else if (_mid > 0)
        {
            _mid -= dt;
            Pose = GripPose.Mid;
        }
        else Pose = GripPose.Shut;
    }
}
