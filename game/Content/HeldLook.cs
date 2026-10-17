namespace Stillwake.Game;

/// <summary>
/// How an item held in the front hand looks (0040), in pixels of its picture (<see cref="Picture"/>, +x
/// along the handle toward the head, +y down): the point the fist closes on (<see cref="Grip"/>), the
/// radians the handle leaves the fist at, turned back from the forearm (<see cref="Hold"/>), and the two
/// ends of its working edge (<see cref="EdgeFrom"/>, <see cref="EdgeTo"/>), which is what a swing of it
/// touches: no reach of its own, so a new grip, angle or picture moves the reach with it. A gun's edge is
/// its barrel, the rod seated in it, and the shot leaves <see cref="EdgeTo"/>, the muzzle, along it; its
/// <see cref="Loaded"/> picture, the same size up and left, shows the rod in the barrel while it is ready.
/// </summary>
public sealed record HeldLook(
    string Picture, (double X, double Y) Grip, double Hold, (double X, double Y) EdgeFrom, (double X, double Y) EdgeTo, string? Loaded = null)
{
    /// <summary>
    /// The fire axe, <c>art/hero_axe.png</c>: gripped on the dark wrap a third up its haft; held square to
    /// the forearm, as a hammer is, the blade facing along it; the bright steel's outer line the edge.
    /// </summary>
    public static readonly HeldLook FireAxe = new("res://art/hero_axe.png", (7.5, 7), System.Math.PI / 2, (18, 13), (27, 13));

    /// <summary>
    /// The spring harpoon, <c>art/hero_harpoon.png</c> (0044): gripped by its plank stock, held as a pistol
    /// is, the barrel along the forearm; the edge the rod seated in the tube, from its eye to the muzzle.
    /// </summary>
    public static readonly HeldLook Harpoon = new(
        "res://art/hero_harpoon.png", (4.5, 8), 0, (9, 3.5), (25, 3.5), Loaded: "res://art/hero_harpoon_loaded.png");

    /// <summary>How <paramref name="item"/> looks in the hand; none for an item held as no picture.</summary>
    public static HeldLook? Of(string? item) =>
        item == Content.Axe.Item ? FireAxe : item == Content.Harpoon.Item ? Harpoon : null;
}

/// <summary>
/// The harpoon's rod as it flies, <c>art/hero_harpoon_rod.png</c>, its point along +x: where the line ties
/// on (<see cref="Eye"/>) and its point (<see cref="Tip"/>), in pixels of its picture.
/// </summary>
public static class RodLook
{
    public const string Picture = "res://art/hero_harpoon_rod.png";
    public static readonly (double X, double Y) Eye = (3, 2.5), Tip = (23, 2.5);
}
