namespace Stillwake.Game;

/// <summary>
/// How an item held in the front hand looks (0040), in pixels of its picture (<see cref="Picture"/>, +x
/// along the handle toward the head, +y down): the point the fist closes on (<see cref="Grip"/>), the
/// radians the handle leaves the fist at, turned back from the forearm (<see cref="Hold"/>), and the two
/// ends of its working edge (<see cref="EdgeFrom"/>, <see cref="EdgeTo"/>), which is what a swing of it
/// touches: no reach of its own, so a new grip, angle or picture moves the reach with it.
/// </summary>
public sealed record HeldLook(string Picture, (double X, double Y) Grip, double Hold, (double X, double Y) EdgeFrom, (double X, double Y) EdgeTo)
{
    /// <summary>
    /// The fire axe, <c>art/hero_axe.png</c>: gripped a third up its haft, two pixels of it showing below
    /// the fist; held square to the forearm, as a hammer is, the blade facing along it; the bright
    /// steel's outer line the edge.
    /// </summary>
    public static readonly HeldLook FireAxe = new("res://art/hero_axe.png", (5.5, 7), System.Math.PI / 2, (13, 13), (22, 13));
}
